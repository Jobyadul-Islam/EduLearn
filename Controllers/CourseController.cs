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
using EduLearn.Services;

namespace EduLearn.Controllers
{
    public class CourseController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _environment;
        private readonly IEmailService _emailService;

        public CourseController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IWebHostEnvironment environment, IEmailService emailService)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
            _emailService = emailService;
        }

        private const int CoursesPerPage = 4;

        // Public course listing — no login required
        public IActionResult Index(string? search, int? categoryId, string? sort, int page = 1)
        {
            var query = _context.Courses
                .Include(c => c.Category)
                .Include(c => c.Instructor)
                .Where(c => c.Status == CourseStatus.Approved);

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(c => c.Title.Contains(search));
            }

            if (categoryId.HasValue)
            {
                query = query.Where(c => c.CategoryId == categoryId.Value);
            }

            // No CreatedAt column exists, so Id (assigned in insertion order) is the
            // same creation-order proxy already used elsewhere in this controller.
            query = sort switch
            {
                "popular" => query.OrderByDescending(c => c.Enrollments.Count),
                "rating" => query.OrderByDescending(c => c.Reviews.Any() ? c.Reviews.Average(r => r.Rating) : 0),
                _ => query.OrderByDescending(c => c.Id)
            };

            var totalCount = query.Count();
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)CoursesPerPage));
            page = Math.Clamp(page, 1, totalPages);

            var pagedCourses = query
                .Skip((page - 1) * CoursesPerPage)
                .Take(CoursesPerPage)
                .ToList();

            ViewBag.SearchTerm = search;
            ViewBag.CategoryId = categoryId;
            ViewBag.Sort = sort ?? "newest";
            ViewBag.Categories = _context.Categories.OrderBy(c => c.Name).ToList();
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.RatingsByCourseId = GetRatingStats(pagedCourses.Select(c => c.Id));

            return View(pagedCourses);
        }

        // Average rating + review count per course, keyed by CourseId
        private Dictionary<int, (double Average, int Count)> GetRatingStats(IEnumerable<int> courseIds)
        {
            var ids = courseIds.ToList();
            return _context.Reviews
                .Where(r => ids.Contains(r.CourseId))
                .GroupBy(r => r.CourseId)
                .Select(g => new { CourseId = g.Key, Average = g.Average(r => r.Rating), Count = g.Count() })
                .ToDictionary(x => x.CourseId, x => (x.Average, x.Count));
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
                ViewBag.HasFullAccess = course.Price == 0 || (enrollment?.Status == EnrollmentStatus.Active);
            }
            else
            {
                ViewBag.IsEnrolled = false;
                ViewBag.HasFullAccess = course.Price == 0;
            }

            var reviews = (from r in _context.Reviews
                            join u in _context.Users on r.StudentId equals u.Id
                            where r.CourseId == id
                            orderby r.CreatedAt descending
                            select new { r.Rating, r.Comment, r.CreatedAt, StudentName = u.FullName }).ToList();

            ViewBag.Reviews = reviews;
            ViewBag.AverageRating = reviews.Count > 0 ? reviews.Average(r => r.Rating) : 0;
            ViewBag.ReviewCount = reviews.Count;

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
        public async Task<IActionResult> Enroll(int courseId)
        {
            var userId = _userManager.GetUserId(User);

            var course = _context.Courses.Include(c => c.Instructor).FirstOrDefault(c => c.Id == courseId);
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
                    // Free courses need no payment step, so they start Active; paid courses
                    // stay Pending until ConfirmPayment transitions them.
                    Status = course.Price == 0 ? EnrollmentStatus.Active : EnrollmentStatus.Pending
                };
                _context.Enrollments.Add(enrollment);
                _context.SaveChanges();

                var student = await _userManager.GetUserAsync(User);
                var body = $@"
                    <p>Hi {System.Net.WebUtility.HtmlEncode(student.FullName)},</p>
                    <p>You're enrolled in <strong>{System.Net.WebUtility.HtmlEncode(course.Title)}</strong>.</p>
                    <p><strong>Instructor:</strong> {System.Net.WebUtility.HtmlEncode(course.Instructor?.FullName ?? "TBA")}</p>
                    <p>Log in to EduLearn any time to start learning.</p>
                    <p>— EduLearn</p>";
                // Best-effort: enrollment already succeeded, so a failed/undeliverable email
                // (expected for made-up test addresses) must not block or roll it back.
                await _emailService.SendEmailAsync(student.Email, $"You're enrolled in {course.Title}", body);
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

            if (course.Price == 0 || enrollment.Status == EnrollmentStatus.Active)
            {
                return RedirectToAction("Details", new { id = courseId });
            }

            return View(course);
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
            var nextLessonByCourseId = new Dictionary<int, int?>();
            foreach (var enrollment in enrollments)
            {
                var courseLessons = _context.Lessons
                    .Where(l => l.Module.CourseId == enrollment.CourseId)
                    .OrderBy(l => l.ModuleId).ThenBy(l => l.Id)
                    .Select(l => l.Id)
                    .ToList();

                var completedLessonIds = _context.LessonProgresses
                    .Where(p => p.StudentId == userId && p.IsCompleted && p.Lesson.Module.CourseId == enrollment.CourseId)
                    .Select(p => p.LessonId)
                    .ToList();

                progress[enrollment.CourseId] = courseLessons.Count == 0
                    ? 0
                    : (int)Math.Round(completedLessonIds.Count * 100.0 / courseLessons.Count);

                nextLessonByCourseId[enrollment.CourseId] = courseLessons
                    .Where(id => !completedLessonIds.Contains(id))
                    .Select(id => (int?)id)
                    .FirstOrDefault();
            }
            ViewBag.ProgressByCourseId = progress;
            ViewBag.NextLessonByCourseId = nextLessonByCourseId;

            var enrolledCourseIds = enrollments.Select(e => e.CourseId).ToList();
            var submittedAssignmentIds = _context.AssignmentSubmissions
                .Where(s => s.StudentId == userId)
                .Select(s => s.AssignmentId)
                .ToList();

            ViewBag.UpcomingDeadlines = _context.Assignments
                .Include(a => a.Lesson).ThenInclude(l => l.Module).ThenInclude(m => m.Course)
                .Where(a => enrolledCourseIds.Contains(a.Lesson.Module.CourseId)
                    && a.DueDate >= DateTime.Now
                    && !submittedAssignmentIds.Contains(a.Id))
                .OrderBy(a => a.DueDate)
                .Take(5)
                .ToList();

            ViewBag.ReviewedCourseIds = _context.Reviews
                .Where(r => r.StudentId == userId)
                .Select(r => r.CourseId)
                .ToList();

            return View(enrollments);
        }

        [Authorize(Roles = "Student")]
        public IActionResult OrderHistory()
        {
            var userId = _userManager.GetUserId(User);

            var payments = _context.Payments
                .Include(p => p.Course)
                .Where(p => p.StudentId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .ToList();

            return View(payments);
        }

        [Authorize(Roles = "Student")]
        public IActionResult Receipt(int paymentId)
        {
            var userId = _userManager.GetUserId(User);

            var payment = _context.Payments
                .Include(p => p.Course)
                .FirstOrDefault(p => p.Id == paymentId && p.StudentId == userId);

            if (payment == null || payment.Status != PaymentStatus.Success) return NotFound();

            var user = _context.Users.Find(userId);

            var pdfBytes = InvoiceService.Generate(user.FullName, payment.Course.Title, payment.TransactionId, payment.Amount, payment.CreatedAt);

            var fileName = $"Receipt-{payment.TransactionId}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }

        [Authorize]
        public IActionResult ViewLesson(int id)
        {
            var lesson = _context.Lessons
                .Include(l => l.Module)
                .ThenInclude(m => m.Course)
                .Include(l => l.Assignments)
                .Include(l => l.Quizzes)
                .ThenInclude(q => q.Questions)
                .FirstOrDefault(l => l.Id == id);

            if (lesson == null) return NotFound();

            var courseId = lesson.Module.Course.Id;
            var userId = _userManager.GetUserId(User);

            var enrollment = _context.Enrollments.FirstOrDefault(e => e.CourseId == courseId && e.StudentId == userId);

            if (enrollment == null)
            {
                return Forbid();
            }

            bool hasFullAccess = lesson.Module.Course.Price == 0 || enrollment.Status == EnrollmentStatus.Active;
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

            var quizIds = lesson.Quizzes.Select(q => q.Id).ToList();
            ViewBag.QuizResultsByQuizId = _context.QuizResults
                .Where(r => r.StudentId == userId && quizIds.Contains(r.QuizId))
                .ToDictionary(r => r.QuizId, r => r);

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
                    IsCompleted = true,
                    CompletedAt = DateTime.Now
                };
                _context.LessonProgresses.Add(progress);
            }
            else
            {
                progress.IsCompleted = true;
                progress.CompletedAt = DateTime.Now;
            }

            _context.SaveChanges();

            return RedirectToAction("ViewLesson", new { id = lessonId });
        }

        [Authorize]
        public IActionResult TakeQuiz(int quizId)
        {
            var quiz = _context.Quizzes
                .Include(q => q.Questions)
                .ThenInclude(qq => qq.Options)
                .FirstOrDefault(q => q.Id == quizId);

            if (quiz == null) return NotFound();

            return View(quiz);
        }

        [Authorize]
        [HttpPost]
        public IActionResult SubmitQuiz(int quizId, List<int> selectedOptionIds)
        {
            var quiz = _context.Quizzes
                .Include(q => q.Questions)
                .ThenInclude(qq => qq.Options)
                .FirstOrDefault(q => q.Id == quizId);

            if (quiz == null) return NotFound();

            selectedOptionIds ??= new List<int>();
            var selectedSet = new HashSet<int>(selectedOptionIds);

            int score = 0;
            foreach (var question in quiz.Questions)
            {
                var correctSet = question.Options.Where(o => o.IsCorrect).Select(o => o.Id).ToHashSet();
                var selectedForQuestion = question.Options.Select(o => o.Id).Where(selectedSet.Contains).ToHashSet();

                if (correctSet.SetEquals(selectedForQuestion))
                {
                    score++;
                }
            }

            var userId = _userManager.GetUserId(User);
            var existingResult = _context.QuizResults
                .FirstOrDefault(r => r.QuizId == quizId && r.StudentId == userId);

            if (existingResult != null)
            {
                existingResult.Score = score;
                existingResult.TotalQuestions = quiz.Questions.Count;
                existingResult.AttemptDate = DateTime.Now;
            }
            else
            {
                _context.QuizResults.Add(new QuizResult
                {
                    QuizId = quizId,
                    StudentId = userId,
                    Score = score,
                    TotalQuestions = quiz.Questions.Count,
                    AttemptDate = DateTime.Now
                });
            }

            _context.SaveChanges();

            return RedirectToAction("QuizResult", new { quizId });
        }

        [Authorize]
        public IActionResult QuizResult(int quizId)
        {
            var userId = _userManager.GetUserId(User);
            var result = _context.QuizResults
                .Include(r => r.Quiz)
                .FirstOrDefault(r => r.QuizId == quizId && r.StudentId == userId);

            if (result == null) return NotFound();

            return View(result);
        }

        [Authorize]
        public IActionResult Certificate(int courseId)
        {
            var userId = _userManager.GetUserId(User);

            var enrollment = _context.Enrollments
                .Include(e => e.Course)
                .FirstOrDefault(e => e.CourseId == courseId && e.StudentId == userId);

            if (enrollment == null) return NotFound();

            var totalLessons = _context.Lessons.Count(l => l.Module.CourseId == courseId);
            var completedProgress = _context.LessonProgresses
                .Where(p => p.StudentId == userId && p.IsCompleted && p.Lesson.Module.CourseId == courseId)
                .ToList();

            if (totalLessons == 0 || completedProgress.Count < totalLessons)
            {
                TempData["CertificateError"] = "Complete every lesson in this course to unlock your certificate.";
                return RedirectToAction("MyEnrollments");
            }

            var completionDate = completedProgress
                .Select(p => p.CompletedAt ?? DateTime.Now)
                .Max();

            var user = _context.Users.Find(userId);

            var pdfBytes = CertificateService.Generate(user.FullName, enrollment.Course.Title, completionDate);

            var fileName = $"Certificate-{enrollment.Course.CourseCode ?? enrollment.Course.Title}.pdf".Replace(" ", "-");
            return File(pdfBytes, "application/pdf", fileName);
        }

        [Authorize(Roles = "Student")]
        public IActionResult WriteReview(int courseId)
        {
            var userId = _userManager.GetUserId(User);

            var enrollment = _context.Enrollments.Include(e => e.Course).FirstOrDefault(e => e.CourseId == courseId && e.StudentId == userId);
            if (enrollment == null) return NotFound();

            if (!HasCompletedCourse(userId, courseId))
            {
                TempData["ReviewError"] = "Complete every lesson in this course to leave a review.";
                return RedirectToAction("MyEnrollments");
            }

            if (_context.Reviews.Any(r => r.CourseId == courseId && r.StudentId == userId))
            {
                TempData["ReviewError"] = "You've already reviewed this course.";
                return RedirectToAction("MyEnrollments");
            }

            return View(enrollment.Course);
        }

        [Authorize(Roles = "Student")]
        [HttpPost]
        public IActionResult WriteReview(int courseId, int rating, string? comment)
        {
            var userId = _userManager.GetUserId(User);

            var enrollment = _context.Enrollments.FirstOrDefault(e => e.CourseId == courseId && e.StudentId == userId);
            if (enrollment == null) return NotFound();

            if (!HasCompletedCourse(userId, courseId))
            {
                TempData["ReviewError"] = "Complete every lesson in this course to leave a review.";
                return RedirectToAction("MyEnrollments");
            }

            if (_context.Reviews.Any(r => r.CourseId == courseId && r.StudentId == userId))
            {
                TempData["ReviewError"] = "You've already reviewed this course.";
                return RedirectToAction("MyEnrollments");
            }

            if (rating < 1 || rating > 5)
            {
                TempData["ReviewError"] = "Choose a rating between 1 and 5 stars.";
                return RedirectToAction("WriteReview", new { courseId });
            }

            _context.Reviews.Add(new Review
            {
                StudentId = userId,
                CourseId = courseId,
                Rating = rating,
                Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim(),
                CreatedAt = DateTime.Now
            });
            _context.SaveChanges();

            TempData["ReviewSuccess"] = "Thanks for your review!";
            return RedirectToAction("Details", new { id = courseId });
        }

        private bool HasCompletedCourse(string userId, int courseId)
        {
            var totalLessons = _context.Lessons.Count(l => l.Module.CourseId == courseId);
            if (totalLessons == 0) return false;

            var completedLessons = _context.LessonProgresses.Count(p =>
                p.StudentId == userId && p.IsCompleted && p.Lesson.Module.CourseId == courseId);

            return completedLessons == totalLessons;
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