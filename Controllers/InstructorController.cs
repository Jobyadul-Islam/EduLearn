using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using EduLearn.Data;
using EduLearn.Models;

namespace EduLearn.Controllers
{
    [Authorize(Roles = "Instructor")]
    public class InstructorController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _environment;

        public InstructorController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IWebHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
        }

        // Instructor dashboard — shows only THIS instructor's courses
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var courses = _context.Courses.Where(c => c.InstructorId == userId).ToList();
            return View(courses);
        }

        public IActionResult CreateCourse()
        {
            ViewBag.Categories = _context.Categories.ToList();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateCourse(Course course, IFormFile? Thumbnail)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Categories = _context.Categories.ToList();
                return View(course);
            }

            course.InstructorId = _userManager.GetUserId(User);

            if (Thumbnail != null && Thumbnail.Length > 0)
            {
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "thumbnails");
                var uniqueFileName = Guid.NewGuid().ToString() + "_" + Thumbnail.FileName;
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await Thumbnail.CopyToAsync(stream);
                }

                course.ThumbnailPath = "/uploads/thumbnails/" + uniqueFileName;
            }

            _context.Courses.Add(course);
            _context.SaveChanges();

            return RedirectToAction("Index");
        }
    }
}