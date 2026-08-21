using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using EduLearn.Data;
using EduLearn.Models;
using EduLearn.Models.ViewModels;
using EduLearn.Services;

namespace EduLearn.Controllers
{
    public class ApplyController : Controller
    {
        private const string SessionKey = "VerifiedInstructorPinId";

        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly INotificationService _notificationService;

        public ApplyController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, INotificationService notificationService)
        {
            _context = context;
            _userManager = userManager;
            _notificationService = notificationService;
        }

        // Step 1: enter the PIN emailed by the Admin
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult VerifyPin(string pin)
        {
            var match = _context.InstructorApplicationPins
                .FirstOrDefault(p => p.Code == pin && !p.IsUsed);

            if (match == null)
            {
                ModelState.AddModelError("", "That PIN is invalid or has already been used. Contact the admin for a new one.");
                return View("Index");
            }

            HttpContext.Session.SetInt32(SessionKey, match.Id);
            return RedirectToAction("Form");
        }

        // Step 2: the application form — only reachable after a valid PIN was verified this session
        public IActionResult Form()
        {
            var pinId = HttpContext.Session.GetInt32(SessionKey);
            if (pinId == null)
            {
                return RedirectToAction("Index");
            }

            return View(new InstructorApplicationViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Form(InstructorApplicationViewModel model)
        {
            var pinId = HttpContext.Session.GetInt32(SessionKey);
            if (pinId == null)
            {
                return RedirectToAction("Index");
            }

            var pin = await _context.InstructorApplicationPins.FindAsync(pinId.Value);
            if (pin == null || pin.IsUsed)
            {
                HttpContext.Session.Remove(SessionKey);
                ModelState.AddModelError("", "This PIN is no longer valid. Please request a new one.");
                return View("Index");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (await _userManager.FindByEmailAsync(model.Email) != null)
            {
                ModelState.AddModelError("Email", "An account with this email already exists.");
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                PhoneNumber = model.PhoneNumber,
                Qualification = model.Qualification,
                Institution = model.Institution,
                Skills = model.Skills,
                YearsOfExperience = model.YearsOfExperience,
                Bio = model.Bio,
                IsApproved = false,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);
                return View(model);
            }

            await _userManager.AddToRoleAsync(user, "Instructor");

            pin.IsUsed = true;
            pin.UsedAt = System.DateTime.Now;
            await _context.SaveChangesAsync();

            HttpContext.Session.Remove(SessionKey);

            var admins = await _userManager.GetUsersInRoleAsync("Admin");
            foreach (var admin in admins)
            {
                await _notificationService.NotifyAsync(
                    admin.Id,
                    $"New instructor application from {user.FullName}",
                    "/Admin/Admin/Users");
            }

            return RedirectToAction("Confirmation");
        }

        public IActionResult Confirmation()
        {
            return View();
        }
    }
}
