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

        public IActionResult Index()
        {
            ViewBag.TotalUsers = _context.Users.Count();
            ViewBag.TotalCourses = _context.Courses.Count();
            ViewBag.TotalEnrollments = _context.Enrollments.Count();
            ViewBag.PendingCoursesCount = _context.Courses.Count(c => c.Status == CourseStatus.Pending);

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

            return View(rows);
        }

        [HttpPost]
        public async Task<IActionResult> Approve(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                user.IsApproved = true;
                await _userManager.UpdateAsync(user);
            }
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

        [HttpPost]
        public async Task<IActionResult> SendLoginEmail(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["EmailResult"] = "User not found.";
                return RedirectToAction("Users");
            }

            var tempPassword = GenerateTempPassword();

            await _userManager.RemovePasswordAsync(user);
            var addResult = await _userManager.AddPasswordAsync(user, tempPassword);

            if (!addResult.Succeeded)
            {
                TempData["EmailResult"] = "Could not reset the password — try again.";
                return RedirectToAction("Users");
            }

            var body = $@"
                <p>Hi {System.Net.WebUtility.HtmlEncode(user.FullName)},</p>
                <p>Your EduLearn instructor account is ready. Use the credentials below to log in:</p>
                <p><strong>Email:</strong> {System.Net.WebUtility.HtmlEncode(user.Email)}<br/>
                <strong>Password:</strong> {System.Net.WebUtility.HtmlEncode(tempPassword)}</p>
                <p>We recommend changing your password after logging in.</p>
                <p>— EduLearn</p>";

            var sent = await _emailService.SendEmailAsync(user.Email, "Your EduLearn Instructor Login", body);

            TempData["EmailResult"] = sent
                ? $"Login email sent to {user.Email}."
                : "Password was reset, but the email failed to send. Check the email configuration.";

            return RedirectToAction("Users");
        }

        private static string GenerateTempPassword()
        {
            const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            const string lower = "abcdefghijkmnpqrstuvwxyz";
            const string digits = "23456789";
            const string special = "!@#$%";
            var random = new System.Random();

            var chars = new List<char>
            {
                upper[random.Next(upper.Length)],
                lower[random.Next(lower.Length)],
                digits[random.Next(digits.Length)],
                special[random.Next(special.Length)]
            };

            const string all = upper + lower + digits + special;
            for (int i = 0; i < 6; i++)
                chars.Add(all[random.Next(all.Length)]);

            return new string(chars.OrderBy(_ => random.Next()).ToArray());
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
