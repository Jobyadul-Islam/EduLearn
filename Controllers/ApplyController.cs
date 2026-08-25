using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
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
        private readonly IWebHostEnvironment _environment;

        public ApplyController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, INotificationService notificationService, IWebHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _notificationService = notificationService;
            _environment = environment;
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
        public async Task<IActionResult> Form(InstructorApplicationViewModel model, IFormFile? Resume)
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

            if (Resume == null || Resume.Length == 0)
            {
                ModelState.AddModelError("", "Please attach your CV.");
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

            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "resumes");
            Directory.CreateDirectory(uploadsFolder);
            var uniqueFileName = Guid.NewGuid().ToString() + "_" + Resume!.FileName;
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await Resume.CopyToAsync(stream);
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
                ResumePath = "/uploads/resumes/" + uniqueFileName,
                IsApproved = false,
                IsActive = true
            };

            // The applicant never chooses a password — the Admin sets the real one when
            // approving, so this random value (unknown to anyone) is just to satisfy
            // Identity's CreateAsync requirement and keep the account unusable until then.
            var result = await _userManager.CreateAsync(user, GenerateRandomPassword());
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

        private static string GenerateRandomPassword()
        {
            const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            const string lower = "abcdefghijkmnpqrstuvwxyz";
            const string digits = "23456789";
            const string special = "!@#$%";
            var random = new Random();

            var chars = new List<char>
            {
                upper[random.Next(upper.Length)],
                lower[random.Next(lower.Length)],
                digits[random.Next(digits.Length)],
                special[random.Next(special.Length)]
            };

            const string all = upper + lower + digits + special;
            for (int i = 0; i < 16; i++)
                chars.Add(all[random.Next(all.Length)]);

            return new string(chars.OrderBy(_ => random.Next()).ToArray());
        }

        public IActionResult Confirmation()
        {
            return View();
        }
    }
}
