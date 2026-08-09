using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EduLearn.Data;
using EduLearn.Models;

namespace EduLearn.Controllers
{
    public class CourseController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CourseController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Public course listing — no login required
        public IActionResult Index()
        {
            var courses = _context.Courses
                .Include(c => c.Category)
                .ToList();

            return View(courses);
        }

        // Public course details page
        public IActionResult Details(int id)
        {
            var course = _context.Courses
                .Include(c => c.Category)
                .Include(c => c.Modules)
                .ThenInclude(m => m.Lessons)
                .FirstOrDefault(c => c.Id == id);

            if (course == null) return NotFound();

            if (User.Identity.IsAuthenticated)
            {
                var userId = _userManager.GetUserId(User);
                ViewBag.IsEnrolled = _context.Enrollments.Any(e => e.CourseId == id && e.StudentId == userId);
            }
            else
            {
                ViewBag.IsEnrolled = false;
            }

            return View(course);
        }

        // Enroll the logged-in user into a course
        [Authorize]
        [HttpPost]
        public IActionResult Enroll(int courseId)
        {
            var userId = _userManager.GetUserId(User);

            // Prevent duplicate enrollment
            bool alreadyEnrolled = _context.Enrollments
                .Any(e => e.CourseId == courseId && e.StudentId == userId);

            if (!alreadyEnrolled)
            {
                var enrollment = new Enrollment
                {
                    CourseId = courseId,
                    StudentId = userId,
                    EnrollDate = DateTime.Now
                };
                _context.Enrollments.Add(enrollment);
                _context.SaveChanges();
            }

            return RedirectToAction("Details", new { id = courseId });
        }

        // Show the logged-in user's enrolled courses
        [Authorize]
        public IActionResult MyEnrollments()
        {
            var userId = _userManager.GetUserId(User);

            var enrollments = _context.Enrollments
                .Include(e => e.Course)
                .Where(e => e.StudentId == userId)
                .ToList();

            return View(enrollments);
        }
    }
}