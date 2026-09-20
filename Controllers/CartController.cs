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
    // CourseController.Enroll). This page is the one place to pay for several of them
    // in a single combined bKash charge.
    [Authorize(Roles = "Student")]
    public class CartController : Controller
    {
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

            ViewBag.Subtotal = subtotal;
            ViewBag.Total = subtotal;

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
    }
}
