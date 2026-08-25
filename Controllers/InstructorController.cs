using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EduLearn.Data;
using EduLearn.Models;
using EduLearn.Models.ViewModels;
using System.Linq;

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

        // ---------------- Dashboard ----------------

        // Instructor dashboard — shows only THIS instructor's courses
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var courses = _context.Courses.Where(c => c.InstructorId == userId).ToList();

            var user = await _userManager.FindByIdAsync(userId);
            ViewBag.IsApproved = user?.IsApproved ?? false;

            return View(courses);
        }

        // ---------------- Course ----------------

        public async Task<IActionResult> CreateCourse()
        {
            if (!await IsCurrentInstructorApproved())
            {
                return RedirectToAction("Index");
            }

            ViewBag.Categories = _context.Categories.ToList();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateCourse(Course course, IFormFile? Thumbnail)
        {
            if (!await IsCurrentInstructorApproved())
            {
                return RedirectToAction("Index");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Categories = _context.Categories.ToList();
                return View(course);
            }

            course.InstructorId = _userManager.GetUserId(User);
            course.CourseCode = GenerateCourseCode();
            course.Status = CourseStatus.Pending;
            course.RejectionReason = null;

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

        // View a course's modules (instructor's own course only)
        public IActionResult CourseDetails(int id)
        {
            var userId = _userManager.GetUserId(User);

            var course = _context.Courses
                .Include(c => c.Modules)
                .ThenInclude(m => m.Lessons)
                .ThenInclude(l => l.Assignments)
                .FirstOrDefault(c => c.Id == id && c.InstructorId == userId);

            if (course == null) return NotFound();

            return View(course);
        }

        public IActionResult EditCourse(int id)
        {
            var userId = _userManager.GetUserId(User);
            var course = _context.Courses.FirstOrDefault(c => c.Id == id && c.InstructorId == userId);
            if (course == null) return NotFound();

            ViewBag.Categories = _context.Categories.ToList();
            return View(course);
        }

        [HttpPost]
        public async Task<IActionResult> EditCourse(int id, Course course, IFormFile? Thumbnail)
        {
            var userId = _userManager.GetUserId(User);
            var existing = _context.Courses.FirstOrDefault(c => c.Id == id && c.InstructorId == userId);
            if (existing == null) return NotFound();

            if (!ModelState.IsValid)
            {
                ViewBag.Categories = _context.Categories.ToList();
                return View(course);
            }

            existing.Title = course.Title;
            existing.Description = course.Description;
            existing.Price = course.Price;
            existing.CategoryId = course.CategoryId;
            existing.Status = CourseStatus.Pending;
            existing.RejectionReason = null;

            if (Thumbnail != null && Thumbnail.Length > 0)
            {
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "thumbnails");
                var uniqueFileName = Guid.NewGuid().ToString() + "_" + Thumbnail.FileName;
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await Thumbnail.CopyToAsync(stream);
                }

                existing.ThumbnailPath = "/uploads/thumbnails/" + uniqueFileName;
            }

            _context.SaveChanges();

            return RedirectToAction("Index");
        }

        public IActionResult DeleteCourse(int id)
        {
            var userId = _userManager.GetUserId(User);
            var course = _context.Courses
                .Include(c => c.Category)
                .FirstOrDefault(c => c.Id == id && c.InstructorId == userId);
            if (course == null) return NotFound();

            return View(course);
        }

        [HttpPost, ActionName("DeleteCourse")]
        public IActionResult DeleteCourseConfirmed(int id)
        {
            var userId = _userManager.GetUserId(User);
            var course = _context.Courses.FirstOrDefault(c => c.Id == id && c.InstructorId == userId);
            if (course == null) return NotFound();

            _context.Courses.Remove(course);
            _context.SaveChanges();

            return RedirectToAction("Index");
        }

        // ---------------- Reviews ----------------

        public IActionResult Reviews(int? courseId)
        {
            var userId = _userManager.GetUserId(User);
            var myCourses = _context.Courses.Where(c => c.InstructorId == userId).ToList();
            var myCourseIds = myCourses.Select(c => c.Id).ToList();

            var reviewsQuery = from r in _context.Reviews
                                join u in _context.Users on r.StudentId equals u.Id
                                where myCourseIds.Contains(r.CourseId)
                                select new
                                {
                                    r.Rating,
                                    r.Comment,
                                    r.CreatedAt,
                                    StudentName = u.FullName,
                                    r.CourseId
                                };

            if (courseId.HasValue)
            {
                reviewsQuery = reviewsQuery.Where(r => r.CourseId == courseId.Value);
            }

            var reviews = reviewsQuery
                .OrderByDescending(r => r.CreatedAt)
                .ToList()
                .Select(r => new
                {
                    r.Rating,
                    r.Comment,
                    r.CreatedAt,
                    r.StudentName,
                    CourseTitle = myCourses.First(c => c.Id == r.CourseId).Title
                })
                .ToList();

            // Sorted lowest-rated first so underperforming courses surface immediately;
            // courses with no reviews yet sink to the bottom rather than being flagged as "worst".
            var courseStats = myCourses
                .Select(c =>
                {
                    var ratings = _context.Reviews.Where(r => r.CourseId == c.Id).Select(r => r.Rating).ToList();
                    return new
                    {
                        CourseId = c.Id,
                        CourseTitle = c.Title,
                        Average = ratings.Count > 0 ? ratings.Average() : (double?)null,
                        Count = ratings.Count
                    };
                })
                .OrderBy(x => x.Average ?? double.MaxValue)
                .ToList();

            ViewBag.MyCourses = myCourses;
            ViewBag.CourseStats = courseStats;
            ViewBag.SelectedCourseId = courseId;

            return View(reviews);
        }

        private async Task<bool> IsCurrentInstructorApproved()
        {
            var userId = _userManager.GetUserId(User);
            var user = await _userManager.FindByIdAsync(userId);
            return user != null && user.IsApproved;
        }

        // ---------------- Module ----------------

        // GET: Create a Module under a course
        public IActionResult CreateModule(int courseId)
        {
            ViewBag.CourseId = courseId;
            return View();
        }

        [HttpPost]
        public IActionResult CreateModule(Module module)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.CourseId = module.CourseId;
                return View(module);
            }

            _context.Modules.Add(module);
            _context.SaveChanges();

            return RedirectToAction("CourseDetails", new { id = module.CourseId });
        }

        // ---------------- Lesson ----------------

        // GET: Create a Lesson under a module
        public IActionResult CreateLesson(int moduleId)
        {
            ViewBag.ModuleId = moduleId;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateLesson(Lesson lesson, IFormFile? LessonFile)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.ModuleId = lesson.ModuleId;
                return View(lesson);
            }

            if (LessonFile != null && LessonFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "lessons");
                Directory.CreateDirectory(uploadsFolder); // ensures folder exists

                var uniqueFileName = Guid.NewGuid().ToString() + "_" + LessonFile.FileName;
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await LessonFile.CopyToAsync(stream);
                }

                lesson.FilePath = "/uploads/lessons/" + uniqueFileName;
            }

            _context.Lessons.Add(lesson);
            _context.SaveChanges();

            var module = _context.Modules.Find(lesson.ModuleId);
            return RedirectToAction("CourseDetails", new { id = module.CourseId });
        }

        // ---------------- Assignment ----------------

        // GET: Create an Assignment under a lesson
        public IActionResult CreateAssignment(int lessonId)
        {
            ViewBag.LessonId = lessonId;
            return View();
        }

        [HttpPost]
        public IActionResult CreateAssignment(Assignment assignment)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.LessonId = assignment.LessonId;
                return View(assignment);
            }

            _context.Assignments.Add(assignment);
            _context.SaveChanges();

            var lesson = _context.Lessons.Find(assignment.LessonId);
            var module = _context.Modules.Find(lesson.ModuleId);
            return RedirectToAction("CourseDetails", new { id = module.CourseId });
        }

        // ---------------- Quiz ----------------

        // GET: Create a Quiz under a lesson
        public IActionResult CreateQuiz(int lessonId)
        {
            var model = new QuizCreateViewModel { LessonId = lessonId };
            // start with 1 empty question, each with 2 empty options, so the form isn't blank
            model.Questions.Add(new QuestionViewModel
            {
                Options = new List<OptionViewModel> { new OptionViewModel(), new OptionViewModel() }
            });
            return View(model);
        }

        private string GenerateCourseCode()
        {
            var random = new Random();
            string letters = "ABCDEFGHJKLMNPQRSTUVWXYZ"; // no O/I to avoid confusion with 0/1

            for (int attempt = 0; attempt < 10; attempt++)
            {
                string code = new string(Enumerable.Range(0, 2).Select(_ => letters[random.Next(letters.Length)]).ToArray());
                string digits = random.Next(1000, 9999).ToString();
                string candidate = $"{code}-{digits}"; // e.g. "CS-4821"

                if (!_context.Courses.Any(c => c.CourseCode == candidate))
                    return candidate;
            }

            // Extremely unlikely fallback: guarantee uniqueness with a GUID fragment
            return $"EDU-{Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper()}";
        }

        [HttpPost]
        public IActionResult CreateQuiz(QuizCreateViewModel model)
        {
            var quiz = new Quiz
            {
                Title = model.Title,
                LessonId = model.LessonId,
                Questions = new List<QuizQuestion>()
            };

            foreach (var q in model.Questions)
            {
                var question = new QuizQuestion
                {
                    QuestionText = q.QuestionText,
                    Options = new List<QuizOption>()
                };

                foreach (var o in q.Options)
                {
                    question.Options.Add(new QuizOption
                    {
                        OptionText = o.OptionText,
                        IsCorrect = o.IsCorrect
                    });
                }

                quiz.Questions.Add(question);
            }

            _context.Quizzes.Add(quiz);
            _context.SaveChanges();

            var lesson = _context.Lessons.Find(model.LessonId);
            var module = _context.Modules.Find(lesson.ModuleId);
            return RedirectToAction("CourseDetails", new { id = module.CourseId });
        }
    }
}