using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EduLearn.Data;

namespace EduLearn.Controllers
{
    public class CourseController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CourseController(ApplicationDbContext context)
        {
            _context = context;
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

            return View(course);
        }
    }
}