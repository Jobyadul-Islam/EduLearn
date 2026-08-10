using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
        private readonly IWebHostEnvironment _environment;

        public CourseController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IWebHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
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

        [Authorize]
        [HttpPost]
        public IActionResult Enroll(int courseId)
        {
            var userId = _userManager.GetUserId(User);

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

        [Authorize]
        public IActionResult ViewLesson(int id)
        {
            var lesson = _context.Lessons
                .Include(l => l.Module)
                .ThenInclude(m => m.Course)
                .Include(l => l.Assignments)
                .FirstOrDefault(l => l.Id == id);

            if (lesson == null) return NotFound();

            var courseId = lesson.Module.Course.Id;
            var userId = _userManager.GetUserId(User);

            bool isEnrolled = _context.Enrollments.Any(e => e.CourseId == courseId && e.StudentId == userId);

            if (!isEnrolled)
            {
                return Forbid();
            }

            return View(lesson);
        }

        [Authorize]
        public IActionResult SubmitAssignment(int assignmentId)
        {
            var assignment = _context.Assignments.Find(assignmentId);
            if (assignment == null) return NotFound();

            ViewBag.AssignmentId = assignmentId;
            ViewBag.AssignmentTitle = assignment.Title;
            return View();
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> SubmitAssignment(int assignmentId, IFormFile SubmissionFile)
        {
            if (SubmissionFile == null || SubmissionFile.Length == 0)
            {
                ModelState.AddModelError("", "Please select a file to submit.");
                var assignment = _context.Assignments.Find(assignmentId);
                ViewBag.AssignmentId = assignmentId;
                ViewBag.AssignmentTitle = assignment.Title;
                return View();
            }

            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "submissions");
            Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = Guid.NewGuid().ToString() + "_" + SubmissionFile.FileName;
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await SubmissionFile.CopyToAsync(stream);
            }

            var submission = new AssignmentSubmission
            {
                AssignmentId = assignmentId,
                StudentId = _userManager.GetUserId(User),
                FilePath = "/uploads/submissions/" + uniqueFileName,
                SubmittedDate = DateTime.Now
            };

            _context.AssignmentSubmissions.Add(submission);
            _context.SaveChanges();

            var assignmentForLesson = _context.Assignments.Find(assignmentId);
            return RedirectToAction("ViewLesson", new { id = assignmentForLesson.LessonId });
        }
    }
}