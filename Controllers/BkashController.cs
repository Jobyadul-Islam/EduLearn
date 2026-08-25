using System;
using System.Linq;
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
        private const string SessionCourseIdKey = "BkashCourseId";

        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IBkashPaymentService _bkash;

        public BkashController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IBkashPaymentService bkash)
        {
            _context = context;
            _userManager = userManager;
            _bkash = bkash;
        }

        [HttpPost]
        public async Task<IActionResult> Pay(int courseId)
        {
            var userId = _userManager.GetUserId(User);

            var enrollment = _context.Enrollments.Include(e => e.Course).FirstOrDefault(e => e.CourseId == courseId && e.StudentId == userId);
            if (enrollment == null || enrollment.Status == EnrollmentStatus.Active)
            {
                return RedirectToAction("Details", "Course", new { id = courseId });
            }

            if (!_bkash.IsConfigured)
            {
                TempData["PaymentError"] = "bKash isn't configured on this server yet.";
                return RedirectToAction("Checkout", "Course", new { courseId });
            }

            var idToken = await _bkash.GrantTokenAsync();
            if (idToken == null)
            {
                TempData["PaymentError"] = "Could not reach bKash. Please try again.";
                return RedirectToAction("Checkout", "Course", new { courseId });
            }

            var callbackUrl = Url.Action("AgreementCallback", "Bkash", null, Request.Scheme);
            var agreement = await _bkash.CreateAgreementAsync(idToken, userId, callbackUrl!);

            if (!agreement.Success)
            {
                TempData["PaymentError"] = agreement.ErrorMessage ?? "Could not start the bKash agreement.";
                return RedirectToAction("Checkout", "Course", new { courseId });
            }

            HttpContext.Session.SetInt32(SessionCourseIdKey, courseId);
            return Redirect(agreement.BkashUrl!);
        }

        // The customer's browser lands here after authorizing (or cancelling) the agreement on bKash's page
        public async Task<IActionResult> AgreementCallback(string paymentID, string status)
        {
            var courseId = HttpContext.Session.GetInt32(SessionCourseIdKey);
            if (courseId == null) return RedirectToAction("Index", "Course");

            if (!string.Equals(status, "success", StringComparison.OrdinalIgnoreCase))
            {
                HttpContext.Session.Remove(SessionCourseIdKey);
                TempData["PaymentError"] = $"bKash agreement was not completed ({status}).";
                return RedirectToAction("Checkout", "Course", new { courseId = courseId.Value });
            }

            var idToken = await _bkash.GrantTokenAsync();
            if (idToken == null)
            {
                TempData["PaymentError"] = "Could not reach bKash. Please try again.";
                return RedirectToAction("Checkout", "Course", new { courseId = courseId.Value });
            }

            var executed = await _bkash.ExecuteAgreementAsync(idToken, paymentID);
            if (!executed.Success)
            {
                TempData["PaymentError"] = executed.ErrorMessage ?? "Could not confirm the bKash agreement.";
                return RedirectToAction("Checkout", "Course", new { courseId = courseId.Value });
            }

            var course = _context.Courses.Find(courseId.Value);
            if (course == null) return RedirectToAction("Index", "Course");

            var callbackUrl = Url.Action("PaymentCallback", "Bkash", null, Request.Scheme);
            var invoiceNumber = $"ENR-{courseId.Value}-{DateTime.Now:yyyyMMddHHmmssfff}";
            var payment = await _bkash.CreatePaymentAsync(idToken, executed.AgreementId!, course.Price, invoiceNumber, callbackUrl!);

            if (!payment.Success)
            {
                TempData["PaymentError"] = payment.ErrorMessage ?? "Could not start the bKash payment.";
                return RedirectToAction("Checkout", "Course", new { courseId = courseId.Value });
            }

            return Redirect(payment.BkashUrl!);
        }

        // The customer's browser lands here after authorizing (or cancelling) the actual charge
        public async Task<IActionResult> PaymentCallback(string paymentID, string status)
        {
            var courseId = HttpContext.Session.GetInt32(SessionCourseIdKey);
            HttpContext.Session.Remove(SessionCourseIdKey);
            if (courseId == null) return RedirectToAction("Index", "Course");

            var userId = _userManager.GetUserId(User);
            var enrollment = _context.Enrollments.Include(e => e.Course).FirstOrDefault(e => e.CourseId == courseId.Value && e.StudentId == userId);
            if (enrollment == null) return RedirectToAction("Index", "Course");

            if (!string.Equals(status, "success", StringComparison.OrdinalIgnoreCase))
            {
                _context.Payments.Add(new Payment
                {
                    StudentId = userId,
                    CourseId = courseId.Value,
                    Amount = enrollment.Course.Price,
                    TransactionId = paymentID,
                    Status = PaymentStatus.Failed,
                    CreatedAt = DateTime.Now
                });
                _context.SaveChanges();

                TempData["PaymentError"] = $"bKash payment was not completed ({status}).";
                return RedirectToAction("Checkout", "Course", new { courseId = courseId.Value });
            }

            var idToken = await _bkash.GrantTokenAsync();
            var executed = idToken == null ? new BkashPaymentExecuteResult { Success = false } : await _bkash.ExecutePaymentAsync(idToken, paymentID);

            var record = new Payment
            {
                StudentId = userId,
                CourseId = courseId.Value,
                Amount = enrollment.Course.Price,
                TransactionId = executed.TrxId ?? paymentID,
                Status = executed.Success ? PaymentStatus.Success : PaymentStatus.Failed,
                CreatedAt = DateTime.Now
            };
            _context.Payments.Add(record);

            if (!executed.Success)
            {
                _context.SaveChanges();
                TempData["PaymentError"] = executed.ErrorMessage ?? "Could not confirm the bKash payment.";
                return RedirectToAction("Checkout", "Course", new { courseId = courseId.Value });
            }

            enrollment.Status = EnrollmentStatus.Active;
            enrollment.PaymentDate = DateTime.Now;
            _context.SaveChanges();

            TempData["PaymentSuccess"] = $"Payment successful via bKash — transaction {record.TransactionId}.";
            return RedirectToAction("Details", "Course", new { id = courseId.Value });
        }
    }
}
