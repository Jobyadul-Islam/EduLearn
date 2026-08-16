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

namespace EduLearn.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
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

        public async Task<IActionResult> Users()
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
    }
}
