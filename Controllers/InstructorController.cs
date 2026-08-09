using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EduLearn.Data;
using EduLearn.Models;
using EduLearn.Models.ViewModels;

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
        public IActionResult Index()
        {
            var userId = _userManager.GetUserId(User);
            var courses = _context.Courses.Where(c => c.InstructorId == userId).ToList();
            return View(courses);
        }

        // ---------------- Course ----------------

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

        // View a course's modules (instructor's own course only)
        public IActionResult CourseDetails(int id)
        {
            var course = _context.Courses
                .Include(c => c.Modules)
                .ThenInclude(m => m.Lessons)
                .FirstOrDefault(c => c.Id == id);

            if (course == null) return NotFound();

            return View(course);
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