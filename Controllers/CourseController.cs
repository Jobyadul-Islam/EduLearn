using System;
using System.Collections.Generic;
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
                .Include(c => c.Instructor)
                .Where(c => c.Status == CourseStatus.Approved)
                .ToList();

            return View(courses);
        }

        // Public course details page
        public IActionResult Details(int id)
        {
            var course = _context.Courses
                .Include(c => c.Category)
                .Include(c => c.Instructor)
                .Include(c => c.Modules)
                .ThenInclude(m => m.Lessons)
                .FirstOrDefault(c => c.Id == id);

            if (course == null) return NotFound();

            if (course.Status != CourseStatus.Approved)
            {
                // Only the owning instructor or an Admin may preview a course that isn't live yet
                var canPreview = User.Identity.IsAuthenticated &&
                    (User.IsInRole("Admin") || _userManager.GetUserId(User) == course.InstructorId);

                if (!canPreview) return NotFound();
            }

            ViewBag.FreeLessonIds = GetFreePreviewLessonIds(id);

            if (User.Identity.IsAuthenticated)
            {
                var userId = _userManager.GetUserId(User);
                var enrollment = _context.Enrollments.FirstOrDefault(e => e.CourseId == id && e.StudentId == userId);

                ViewBag.IsEnrolled = enrollment != null;
                ViewBag.HasFullAccess = course.Price == 0 || (enrollment?.IsPaid ?? false);
            }
            else
            {
                ViewBag.IsEnrolled = false;
                ViewBag.HasFullAccess = course.Price == 0;
            }

            return View(course);
        }

        // First 2 lessons of a course (by creation order) are free to preview even without payment
        private List<int> GetFreePreviewLessonIds(int courseId)
        {
            return _context.Lessons
                .Where(l => l.Module.CourseId == courseId)
                .OrderBy(l => l.Id)
                .Take(2)
                .Select(l => l.Id)
                .ToList();
        }

        [Authorize(Roles = "Student")]
        [HttpPost]
        public IActionResult Enroll(int courseId)
        {
            var userId = _userManager.GetUserId(User);

            var course = _context.Courses.Find(courseId);
            if (course == null || course.Status != CourseStatus.Approved)
            {
                return NotFound();
            }

            bool alreadyEnrolled = _context.Enrollments
                .Any(e => e.CourseId == courseId && e.StudentId == userId);

            if (!alreadyEnrolled)
            {
                var enrollment = new Enrollment
                {
                    CourseId = courseId,
                    StudentId = userId,
                    EnrollDate = DateTime.Now,
                    IsPaid = false
                };
                _context.Enrollments.Add(enrollment);
                _context.SaveChanges();
            }

            return RedirectToAction("Details", new { id = courseId });
        }

        [Authorize(Roles = "Student")]
        public IActionResult Checkout(int courseId)
        {
            var userId = _userManager.GetUserId(User);

            var course = _context.Courses.Include(c => c.Category).FirstOrDefault(c => c.Id == courseId);
            if (course == null) return NotFound();

            var enrollment = _context.Enrollments.FirstOrDefault(e => e.CourseId == courseId && e.StudentId == userId);
            if (enrollment == null)
            {
                // Must enroll (free) before paying to unlock the rest of the course
                return RedirectToAction("Details", new { id = courseId });
            }

            if (course.Price == 0 || enrollment.IsPaid)
            {
                return RedirectToAction("Details", new { id = courseId });
            }

            return View(course);
        }

        [Authorize(Roles = "Student")]
        [HttpPost]
        public IActionResult ConfirmPayment(int courseId)
        {
            var userId = _userManager.GetUserId(User);

            var enrollment = _context.Enrollments.FirstOrDefault(e => e.CourseId == courseId && e.StudentId == userId);
            if (enrollment == null) return NotFound();

            enrollment.IsPaid = true;
            enrollment.PaymentDate = DateTime.Now;
            _context.SaveChanges();

            return RedirectToAction("Details", new { id = courseId });
        }

        [Authorize]
        public IActionResult MyEnrollments()
        {
            // Instructors/Admins don't take courses — send them to the dashboard that matches their role
            if (User.IsInRole("Instructor"))
            {
                return RedirectToAction("Index", "Instructor");
            }
            if (User.IsInRole("Admin"))
            {
                return RedirectToAction("Index", "Admin", new { area = "Admin" });
            }

            var userId = _userManager.GetUserId(User);

            var enrollments = _context.Enrollments
                .Include(e => e.Course)
                .Where(e => e.StudentId == userId)
                .ToList();

            var progress = new Dictionary<int, int>();
            foreach (var enrollment in enrollments)
            {
                var totalLessons = _context.Lessons.Count(l => l.Module.CourseId == enrollment.CourseId);
                var completedLessons = _context.LessonProgresses.Count(p =>
                    p.StudentId == userId && p.IsCompleted && p.Lesson.Module.CourseId == enrollment.CourseId);

                progress[enrollment.CourseId] = totalLessons == 0 ? 0 : (int)Math.Round(completedLessons * 100.0 / totalLessons);
            }
            ViewBag.ProgressByCourseId = progress;

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

            var enrollment = _context.Enrollments.FirstOrDefault(e => e.CourseId == courseId && e.StudentId == userId);

            if (enrollment == null)
            {
                return Forbid();
            }

            bool hasFullAccess = lesson.Module.Course.Price == 0 || enrollment.IsPaid;
            bool isFreePreview = GetFreePreviewLessonIds(courseId).Contains(id);

            if (!hasFullAccess && !isFreePreview)
            {
                return RedirectToAction("Checkout", new { courseId });
            }

            ViewBag.IsFreePreview = isFreePreview;

            ViewBag.IsCompleted = _context.LessonProgresses
                .Any(p => p.LessonId == id && p.StudentId == userId && p.IsCompleted);

            ViewBag.SubmittedAssignmentIds = _context.AssignmentSubmissions
                .Where(s => s.StudentId == userId)
                .Select(s => s.AssignmentId)
                .ToList();

            return View(lesson);
        }

        [Authorize]
        [HttpPost]
        public IActionResult MarkComplete(int lessonId)
        {
            var userId = _userManager.GetUserId(User);

            var progress = _context.LessonProgresses
                .FirstOrDefault(p => p.LessonId == lessonId && p.StudentId == userId);

            if (progress == null)
            {
                progress = new LessonProgress
                {
                    LessonId = lessonId,
                    StudentId = userId,
                    IsCompleted = true
                };
                _context.LessonProgresses.Add(progress);
            }
            else
            {
                progress.IsCompleted = true;
            }

            _context.SaveChanges();

            return RedirectToAction("ViewLesson", new { id = lessonId });
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

            var userId = _userManager.GetUserId(User);

            // Check if this student already submitted this assignment
            var existingSubmission = _context.AssignmentSubmissions
                .FirstOrDefault(s => s.AssignmentId == assignmentId && s.StudentId == userId);

            if (existingSubmission != null)
            {
                // Update the existing submission instead of creating a new one
                existingSubmission.FilePath = "/uploads/submissions/" + uniqueFileName;
                existingSubmission.SubmittedDate = DateTime.Now;
            }
            else
            {
                var submission = new AssignmentSubmission
                {
                    AssignmentId = assignmentId,
                    StudentId = userId,
                    FilePath = "/uploads/submissions/" + uniqueFileName,
                    SubmittedDate = DateTime.Now
                };
                _context.AssignmentSubmissions.Add(submission);
            }

            _context.SaveChanges();

            var assignmentForLesson = _context.Assignments.Find(assignmentId);
            return RedirectToAction("ViewLesson", new { id = assignmentForLesson.LessonId });
        }
    }
}