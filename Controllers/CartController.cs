using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EduLearn.Data;
using EduLearn.Models;
using EduLearn.Services;

namespace EduLearn.Controllers
{
    // A student's "cart" isn't a separate list — it's simply their Pending, paid
    // enrollments (created the moment they click Enroll on a paid course; see
    // CourseController.Enroll). This page is the one place to pay for several of them,
    // optionally discounted by a coupon, in a single combined bKash charge.
    [Authorize(Roles = "Student")]
    public class CartController : Controller
    {
        public const string SessionCouponKey = "CartCouponCode";

        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CartController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            var userId = _userManager.GetUserId(User);
            var items = _context.Enrollments
                .Include(e => e.Course)
                .Where(e => e.StudentId == userId && e.Status == EnrollmentStatus.Pending && e.Course.Price > 0)
                .OrderByDescending(e => e.Id)
                .ToList();

            var subtotal = items.Sum(i => i.Course.Price);

            Coupon? coupon = null;
            var appliedCode = HttpContext.Session.GetString(SessionCouponKey);
            if (!string.IsNullOrWhiteSpace(appliedCode))
            {
                var (validCoupon, error) = CouponService.Validate(_context, appliedCode, userId!);
                if (validCoupon != null)
                {
                    coupon = validCoupon;
                }
                else
                {
                    // The coupon we had on file stopped being valid (expired, disabled,
                    // redeemed elsewhere) since it was applied — drop it rather than silently
                    // keep charging the discounted price at checkout.
                    HttpContext.Session.Remove(SessionCouponKey);
                    TempData["CouponError"] = error;
                }
            }

            var discount = coupon != null ? Math.Round(subtotal * coupon.DiscountPercent / 100m, 2) : 0;

            ViewBag.Subtotal = subtotal;
            ViewBag.Coupon = coupon;
            ViewBag.Discount = discount;
            ViewBag.Total = subtotal - discount;

            return View(items);
        }

        [HttpPost]
        public IActionResult RemoveFromCart(int courseId)
        {
            var userId = _userManager.GetUserId(User);
            var enrollment = _context.Enrollments
                .FirstOrDefault(e => e.CourseId == courseId && e.StudentId == userId && e.Status == EnrollmentStatus.Pending);

            if (enrollment != null)
            {
                _context.Enrollments.Remove(enrollment);
                _context.SaveChanges();
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult ApplyCoupon(string code)
        {
            var userId = _userManager.GetUserId(User);
            var (coupon, error) = CouponService.Validate(_context, code, userId!);

            if (coupon == null)
            {
                TempData["CouponError"] = error ?? "Enter a coupon code.";
            }
            else
            {
                HttpContext.Session.SetString(SessionCouponKey, coupon.Code);
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult RemoveCoupon()
        {
            HttpContext.Session.Remove(SessionCouponKey);
            return RedirectToAction("Index");
        }
    }
}
