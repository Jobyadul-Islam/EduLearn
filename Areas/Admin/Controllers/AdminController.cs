using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EduLearn.Data;
using EduLearn.Models;
using EduLearn.Models.ViewModels;
using EduLearn.Services;

namespace EduLearn.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;

        public AdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IEmailService emailService)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.TotalUsers = _context.Users.Count();
            ViewBag.TotalCourses = _context.Courses.Count();
            ViewBag.TotalEnrollments = _context.Enrollments.Count();
            ViewBag.PendingCoursesCount = _context.Courses.Count(c => c.Status == CourseStatus.Pending);

            ViewBag.TotalStudents = (await _userManager.GetUsersInRoleAsync("Student")).Count;
            ViewBag.TotalInstructors = (await _userManager.GetUsersInRoleAsync("Instructor")).Count;

            var sixMonthsAgo = new System.DateTime(System.DateTime.Now.Year, System.DateTime.Now.Month, 1).AddMonths(-5);
            var registrationsByMonth = _context.Users
                .Where(u => u.CreatedAt >= sixMonthsAgo)
                .AsEnumerable()
                .GroupBy(u => new System.DateTime(u.CreatedAt.Year, u.CreatedAt.Month, 1))
                .ToDictionary(g => g.Key, g => g.Count());

            // Always show all 6 months, even ones with zero registrations, so the chart has no gaps
            ViewBag.MonthlyRegistrations = Enumerable.Range(0, 6)
                .Select(i => sixMonthsAgo.AddMonths(i))
                .Select(month => (Month: month, Count: registrationsByMonth.GetValueOrDefault(month, 0)))
                .ToList();

            return View();
        }

        public IActionResult PendingCourses()
        {
            var courses = _context.Courses
                .Include(c => c.Category)
                .Include(c => c.Instructor)
                .Where(c => c.Status == CourseStatus.Pending)
                .OrderBy(c => c.Id)
                .ToList();

            return View(courses);
        }

        public IActionResult AllCourses()
        {
            var courses = _context.Courses
                .Include(c => c.Category)
                .Include(c => c.Instructor)
                .OrderByDescending(c => c.Id)
                .ToList();

            return View(courses);
        }

        public IActionResult AllEnrollments()
        {
            var enrollments = (from e in _context.Enrollments
                                join u in _context.Users on e.StudentId equals u.Id
                                join c in _context.Courses on e.CourseId equals c.Id
                                orderby e.Id descending
                                select new EnrollmentListItemViewModel
                                {
                                    Id = e.Id,
                                    StudentName = u.FullName,
                                    StudentEmail = u.Email,
                                    CourseId = c.Id,
                                    CourseTitle = c.Title,
                                    Status = e.Status,
                                    EnrollDate = e.EnrollDate,
                                    PaymentDate = e.PaymentDate
                                }).ToList();

            return View(enrollments);
        }

        public IActionResult Revenue()
        {
            var (total, monthly) = GetRevenueData();
            ViewBag.TotalRevenue = total;
            ViewBag.MonthlyRevenue = monthly;
            return View();
        }

        public IActionResult ExportRevenuePdf()
        {
            var (total, monthly) = GetRevenueData();
            var pdf = ReportPdfService.GenerateRevenueReport(total, monthly);
            return File(pdf, "application/pdf", $"Revenue-Report-{System.DateTime.Now:yyyyMMdd}.pdf");
        }

        public IActionResult Analytics()
        {
            var (mostPopular, topRated) = GetAnalyticsData();
            ViewBag.MostPopular = mostPopular;
            ViewBag.TopRated = topRated;
            return View();
        }

        public IActionResult ExportAnalyticsPdf()
        {
            var (mostPopular, topRated) = GetAnalyticsData();
            var pdf = ReportPdfService.GenerateAnalyticsReport(mostPopular, topRated);
            return File(pdf, "application/pdf", $"Course-Analytics-{System.DateTime.Now:yyyyMMdd}.pdf");
        }

        private (decimal Total, List<(System.DateTime Month, decimal Revenue)> Monthly) GetRevenueData()
        {
            var successfulPayments = _context.Payments.Where(p => p.Status == PaymentStatus.Success);
            var total = successfulPayments.Sum(p => (decimal?)p.Amount) ?? 0;

            var sixMonthsAgo = new System.DateTime(System.DateTime.Now.Year, System.DateTime.Now.Month, 1).AddMonths(-5);
            var revenueByMonth = successfulPayments
                .Where(p => p.CreatedAt >= sixMonthsAgo)
                .AsEnumerable()
                .GroupBy(p => new System.DateTime(p.CreatedAt.Year, p.CreatedAt.Month, 1))
                .ToDictionary(g => g.Key, g => g.Sum(p => p.Amount));

            // Always show all 6 months, even ones with zero revenue, so the chart/report has no gaps
            var monthly = Enumerable.Range(0, 6)
                .Select(i => sixMonthsAgo.AddMonths(i))
                .Select(month => (Month: month, Revenue: revenueByMonth.GetValueOrDefault(month, 0)))
                .ToList();

            return (total, monthly);
        }

        private (List<(int Id, string Title, int EnrollmentCount)> MostPopular, List<(int Id, string Title, int ReviewCount, double? AverageRating)> TopRated) GetAnalyticsData()
        {
            // EF Core can't put a tuple literal inside a SQL expression tree, so the
            // ranking/limiting happens in SQL via an anonymous type, then the small
            // materialized result is converted to named tuples for the view/PDF.
            var mostPopular = _context.Courses
                .Select(c => new { c.Id, c.Title, EnrollmentCount = c.Enrollments.Count })
                .OrderByDescending(c => c.EnrollmentCount)
                .Take(10)
                .ToList()
                .Select(c => (c.Id, c.Title, c.EnrollmentCount))
                .ToList();

            var topRated = _context.Courses
                .Select(c => new { c.Id, c.Title, ReviewCount = c.Reviews.Count, AverageRating = c.Reviews.Average(r => (double?)r.Rating) })
                .Where(c => c.ReviewCount > 0)
                .OrderByDescending(c => c.AverageRating)
                .Take(10)
                .ToList()
                .Select(c => (c.Id, c.Title, c.ReviewCount, c.AverageRating))
                .ToList();

            return (mostPopular, topRated);
        }

        [HttpPost]
        public IActionResult ApproveCourse(int id)
        {
            var course = _context.Courses.Find(id);
            if (course != null)
            {
                course.Status = CourseStatus.Approved;
                course.RejectionReason = null;
                _context.SaveChanges();
            }
            return RedirectToAction("PendingCourses");
        }

        [HttpPost]
        public IActionResult RejectCourse(int id, string? reason)
        {
            var course = _context.Courses.Find(id);
            if (course != null)
            {
                course.Status = CourseStatus.Rejected;
                course.RejectionReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
                _context.SaveChanges();
            }
            return RedirectToAction("PendingCourses");
        }

        public async Task<IActionResult> Users(string? search, string? role, string? status)
        {
            var users = _context.Users.OrderBy(u => u.FullName).ToList();
            var rows = new List<UserListItemViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                rows.Add(new UserListItemViewModel
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email,
                    Role = roles.FirstOrDefault() ?? "(none)",
                    IsApproved = user.IsApproved,
                    IsActive = user.IsActive
                });
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                rows = rows
                    .Where(r => r.Email != null && r.Email.Contains(search, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(role) && role != "All")
            {
                rows = rows.Where(r => r.Role == role).ToList();
            }

            if (!string.IsNullOrWhiteSpace(status) && status != "All")
            {
                bool wantActive = status == "Active";
                rows = rows.Where(r => r.IsActive == wantActive).ToList();
            }

            ViewBag.SearchTerm = search;
            ViewBag.RoleFilter = role ?? "All";
            ViewBag.StatusFilter = status ?? "All";
            ViewBag.CurrentUserId = _userManager.GetUserId(User);

            return View(rows);
        }

        [HttpPost]
        public async Task<IActionResult> Approve(string id, string password)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["EmailResult"] = "User not found.";
                return RedirectToAction("Users");
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                TempData["EmailResult"] = "Enter a login password to approve this instructor.";
                return RedirectToAction("Users");
            }

            // The admin's chosen password becomes the instructor's real first-time login,
            // replacing whatever they set on the application form.
            await _userManager.RemovePasswordAsync(user);
            var addResult = await _userManager.AddPasswordAsync(user, password);
            if (!addResult.Succeeded)
            {
                TempData["EmailResult"] = string.Join(" ", addResult.Errors.Select(e => e.Description));
                return RedirectToAction("Users");
            }

            user.IsApproved = true;
            await _userManager.UpdateAsync(user);

            var body = $@"
                <p>Hi {System.Net.WebUtility.HtmlEncode(user.FullName)},</p>
                <p>Your EduLearn instructor application has been approved! Use the credentials below to log in:</p>
                <p><strong>Email:</strong> {System.Net.WebUtility.HtmlEncode(user.Email)}<br/>
                <strong>Password:</strong> {System.Net.WebUtility.HtmlEncode(password)}</p>
                <p>You can change your password any time using ""Forgot your password?"" on the login page.</p>
                <p>— EduLearn</p>";

            var sent = await _emailService.SendEmailAsync(user.Email, "Your EduLearn Instructor Account is Approved", body);

            TempData["EmailResult"] = sent
                ? $"Instructor approved and login email sent to {user.Email}."
                : "Instructor approved, but the login email failed to send. Check the email configuration.";

            return RedirectToAction("Users");
        }

        [HttpPost]
        public async Task<IActionResult> Reject(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                user.IsApproved = false;
                user.IsActive = false;
                await _userManager.UpdateAsync(user);
            }
            return RedirectToAction("Users");
        }

        [HttpPost]
        public async Task<IActionResult> ToggleActive(string id)
        {
            var currentUserId = _userManager.GetUserId(User);
            if (id == currentUserId)
            {
                return RedirectToAction("Users");
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                user.IsActive = !user.IsActive;
                await _userManager.UpdateAsync(user);
            }
            return RedirectToAction("Users");
        }

        public IActionResult ViewApplication(string id)
        {
            var user = _context.Users.FirstOrDefault(u => u.Id == id);
            if (user == null) return NotFound();

            return View(user);
        }

        public IActionResult InstructorPins()
        {
            var pins = _context.InstructorApplicationPins
                .OrderByDescending(p => p.CreatedAt)
                .Take(20)
                .ToList();

            return View(pins);
        }

        [HttpPost]
        public async Task<IActionResult> GeneratePin()
        {
            var random = new System.Random();
            string code;
            do
            {
                code = random.Next(0, 1000000).ToString("D6");
            }
            while (_context.InstructorApplicationPins.Any(p => p.Code == code && !p.IsUsed));

            var pin = new InstructorApplicationPin
            {
                Code = code,
                IsUsed = false,
                CreatedAt = System.DateTime.Now,
                GeneratedByAdminId = _userManager.GetUserId(User)
            };

            _context.InstructorApplicationPins.Add(pin);
            await _context.SaveChangesAsync();

            TempData["NewPinCode"] = code;
            return RedirectToAction("InstructorPins");
        }
    }
}
