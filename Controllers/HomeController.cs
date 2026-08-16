using EduLearn.Data;
using EduLearn.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Linq;

namespace EduLearn.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            ViewBag.TotalCourses = _context.Courses.Count(c => c.Status == CourseStatus.Approved);
            ViewBag.TotalCategories = _context.Categories.Count();
            ViewBag.TotalStudents = _context.Enrollments.Select(e => e.StudentId).Distinct().Count();

            var featuredCourses = _context.Courses
                .Include(c => c.Category)
                .Where(c => c.Status == CourseStatus.Approved)
                .OrderByDescending(c => c.Id)
                .Take(6)
                .ToList();

            return View(featuredCourses);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult AccessDenied()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
