using EduLearn.Data;
using EduLearn.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace EduLearn.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.TotalCourses = _context.Courses.Count(c => c.Status == CourseStatus.Approved);
            ViewBag.TotalCategories = _context.Categories.Count();
            ViewBag.TotalStudents = _context.Enrollments.Select(e => e.StudentId).Distinct().Count();
            ViewBag.TotalInstructors = _context.Courses.Select(c => c.InstructorId).Distinct().Count();

            ViewBag.TopCategories = _context.Categories
                .Select(cat => new
                {
                    cat.Id,
                    cat.Name,
                    CourseCount = cat.Courses.Count(c => c.Status == CourseStatus.Approved)
                })
                .OrderByDescending(c => c.CourseCount)
                .Take(6)
                .ToList();

            var admins = await _userManager.GetUsersInRoleAsync("Admin");
            ViewBag.ContactEmail = admins.FirstOrDefault()?.Email ?? "contact@edulearn.com";

            var featuredCourses = _context.Courses
                .Include(c => c.Category)
                .Where(c => c.Status == CourseStatus.Approved)
                .OrderByDescending(c => c.Id)
                .Take(6)
                .ToList();

            var featuredCourseIds = featuredCourses.Select(c => c.Id).ToList();
            ViewBag.RatingsByCourseId = _context.Reviews
                .Where(r => featuredCourseIds.Contains(r.CourseId))
                .GroupBy(r => r.CourseId)
                .Select(g => new { CourseId = g.Key, Average = g.Average(r => r.Rating), Count = g.Count() })
                .ToDictionary(x => x.CourseId, x => (x.Average, x.Count));

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
