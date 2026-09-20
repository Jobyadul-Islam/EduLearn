using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EduLearn.Data;
using EduLearn.Models;
using EduLearn.Services;

namespace EduLearn.Controllers
{
    [Authorize(Roles = "Student")]
    public class BkashController : Controller
    {
        // Survives the redirect out to bKash and back — courseIds is never re-trusted from
        // the client, only from what we stored here right before leaving for bKash.
        private const string SessionCartKey = "BkashCart";

        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IBkashPaymentService _bkash;

        public BkashController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IBkashPaymentService bkash)
        {
            _context = context;
            _userManager = userManager;
            _bkash = bkash;
        }

        private class CartSession
        {
            public List<int> CourseIds { get; set; } = new();
        }

        // Called both by the single-course "Unlock Full Course" button (courseIds has one
        // entry) and by the Cart page's "Checkout" button (courseIds has several) — either
        // way this only ever charges for the caller's own Pending, paid enrollments.
        [HttpPost]
        public async Task<IActionResult> Pay(List<int> courseIds)
        {
            var userId = _userManager.GetUserId(User);
            var ids = (courseIds ?? new List<int>()).Distinct().ToList();

            var enrollments = _context.Enrollments
                .Include(e => e.Course)
                .Where(e => e.StudentId == userId && e.Status == EnrollmentStatus.Pending && ids.Contains(e.CourseId))
                .ToList();

            if (enrollments.Count == 0)
            {
                return RedirectToAction("Index", "Cart");
            }

            var total = ComputeTotal(enrollments.Select(e => e.Course).ToList());

            if (!_bkash.IsConfigured)
            {
                TempData["PaymentError"] = "bKash isn't configured on this server yet.";
                return RedirectToAction("Index", "Cart");
            }

            var idToken = await _bkash.GrantTokenAsync();
            if (idToken == null)
            {
                TempData["PaymentError"] = "Could not reach bKash. Please try again.";
                return RedirectToAction("Index", "Cart");
            }

            var callbackUrl = Url.Action("AgreementCallback", "Bkash", null, Request.Scheme);
            var agreement = await _bkash.CreateAgreementAsync(idToken, userId!, callbackUrl!);

            if (!agreement.Success)
            {
                TempData["PaymentError"] = agreement.ErrorMessage ?? "Could not start the bKash agreement.";
                return RedirectToAction("Index", "Cart");
            }

            var cart = new CartSession { CourseIds = enrollments.Select(e => e.CourseId).ToList() };
            HttpContext.Session.SetString(SessionCartKey, JsonSerializer.Serialize(cart));

            return Redirect(agreement.BkashUrl!);
        }

        // The customer's browser lands here after authorizing (or cancelling) the agreement on bKash's page
        public async Task<IActionResult> AgreementCallback(string paymentID, string status)
        {
            var cart = LoadCart();
            if (cart == null) return RedirectToAction("Index", "Course");

            if (!string.Equals(status, "success", StringComparison.OrdinalIgnoreCase))
            {
                HttpContext.Session.Remove(SessionCartKey);
                TempData["PaymentError"] = $"bKash agreement was not completed ({status}).";
                return RedirectToAction("Index", "Cart");
            }

            var idToken = await _bkash.GrantTokenAsync();
            if (idToken == null)
            {
                TempData["PaymentError"] = "Could not reach bKash. Please try again.";
                return RedirectToAction("Index", "Cart");
            }

            var executed = await _bkash.ExecuteAgreementAsync(idToken, paymentID);
            if (!executed.Success)
            {
                TempData["PaymentError"] = executed.ErrorMessage ?? "Could not confirm the bKash agreement.";
                return RedirectToAction("Index", "Cart");
            }

            var userId = _userManager.GetUserId(User);
            var courses = _context.Courses.Where(c => cart.CourseIds.Contains(c.Id)).ToList();
            var total = ComputeTotal(courses);

            var callbackUrl = Url.Action("PaymentCallback", "Bkash", null, Request.Scheme);
            var invoiceNumber = $"ORD-{DateTime.Now:yyyyMMddHHmmssfff}";
            var payment = await _bkash.CreatePaymentAsync(idToken, userId!, executed.AgreementId!, total, invoiceNumber, callbackUrl!);

            if (!payment.Success)
            {
                TempData["PaymentError"] = payment.ErrorMessage ?? "Could not start the bKash payment.";
                return RedirectToAction("Index", "Cart");
            }

            return Redirect(payment.BkashUrl!);
        }

        // The customer's browser lands here after authorizing (or cancelling) the actual charge
        public async Task<IActionResult> PaymentCallback(string paymentID, string status)
        {
            var cart = LoadCart();
            HttpContext.Session.Remove(SessionCartKey);
            if (cart == null) return RedirectToAction("Index", "Course");

            var userId = _userManager.GetUserId(User);
            var enrollments = _context.Enrollments
                .Include(e => e.Course)
                .Where(e => e.StudentId == userId && cart.CourseIds.Contains(e.CourseId) && e.Status == EnrollmentStatus.Pending)
                .ToList();
            if (enrollments.Count == 0) return RedirectToAction("Index", "Course");

            var invoiceNumber = $"ORD-{DateTime.Now:yyyyMMddHHmmssfff}";

            if (!string.Equals(status, "success", StringComparison.OrdinalIgnoreCase))
            {
                RecordFailedPayments(enrollments, paymentID, invoiceNumber, userId!);
                TempData["PaymentError"] = $"bKash payment was not completed ({status}).";
                return RedirectToAction("Index", "Cart");
            }

            var idToken = await _bkash.GrantTokenAsync();
            var executed = idToken == null ? new BkashPaymentExecuteResult { Success = false } : await _bkash.ExecutePaymentAsync(idToken, paymentID);

            if (!executed.Success)
            {
                RecordFailedPayments(enrollments, paymentID, invoiceNumber, userId!);
                TempData["PaymentError"] = executed.ErrorMessage ?? "Could not confirm the bKash payment.";
                return RedirectToAction("Index", "Cart");
            }

            CompleteOrder(enrollments, userId!, executed.TrxId ?? paymentID, invoiceNumber);

            TempData["PaymentSuccess"] = $"Payment successful via bKash — order {invoiceNumber}.";
            return RedirectToAction("MyEnrollments", "Course");
        }

        private void RecordFailedPayments(List<Enrollment> enrollments, string transactionId, string orderReference, string userId)
        {
            foreach (var e in enrollments)
            {
                _context.Payments.Add(new Payment
                {
                    StudentId = userId,
                    CourseId = e.CourseId,
                    Amount = e.Course.Price,
                    TransactionId = transactionId,
                    Status = PaymentStatus.Failed,
                    CreatedAt = DateTime.Now,
                    OrderReference = orderReference
                });
            }
            _context.SaveChanges();
        }

        private void CompleteOrder(List<Enrollment> enrollments, string userId, string transactionId, string orderReference)
        {
            foreach (var e in enrollments)
            {
                _context.Payments.Add(new Payment
                {
                    StudentId = userId,
                    CourseId = e.CourseId,
                    Amount = e.Course.Price,
                    TransactionId = transactionId,
                    Status = PaymentStatus.Success,
                    CreatedAt = DateTime.Now,
                    OrderReference = orderReference
                });

                e.Status = EnrollmentStatus.Active;
                e.PaymentDate = DateTime.Now;
            }

            _context.SaveChanges();
        }

        // Re-prices every course from the database at the callback — never trusts a total
        // computed earlier in the flow, since a course's price could change between
        // initiating payment and the callback.
        private decimal ComputeTotal(List<Course> courses) => courses.Sum(c => c.Price);

        private CartSession? LoadCart()
        {
            var json = HttpContext.Session.GetString(SessionCartKey);
            if (string.IsNullOrEmpty(json)) return null;

            try
            {
                return JsonSerializer.Deserialize<CartSession>(json);
            }
            catch
            {
                return null;
            }
        }
    }
}
