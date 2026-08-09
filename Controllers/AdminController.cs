using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EduLearn.Data;

namespace EduLearn.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            ViewBag.TotalUsers = _context.Users.Count();
            ViewBag.TotalCourses = _context.Courses.Count();
            ViewBag.TotalEnrollments = _context.Enrollments.Count();

            return View();
        }
    }
}