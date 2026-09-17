using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using EduLearn.Data;
using EduLearn.Models;
using EduLearn.Models.ViewModels;
using EduLearn.Services;

namespace EduLearn.Controllers
{
    public class ApplyController : Controller
    {
        private const string SessionKey = "VerifiedAccessRequestId";

        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly INotificationService _notificationService;
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;
        private readonly IFileUploadService _fileUploadService;

        public ApplyController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, INotificationService notificationService, IWebHostEnvironment environment, IConfiguration configuration, IFileUploadService fileUploadService)
        {
            _context = context;
            _userManager = userManager;
            _notificationService = notificationService;
            _environment = environment;
            _configuration = configuration;
            _fileUploadService = fileUploadService;
        }

        // Step 1: choose Google or email to request access to the application form
        public IActionResult Index()
        {
            ViewBag.GoogleEnabled = IsGoogleConfigured();
            return View();
        }

        public IActionResult GoogleChallenge()
        {
            if (!IsGoogleConfigured())
            {
                TempData["ApplyError"] = "Continuing with Google isn't available right now — please use email instead.";
                return RedirectToAction("Index");
            }

            var properties = new AuthenticationProperties
            {
                RedirectUri = Url.Action("GoogleCallback")
            };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        public async Task<IActionResult> GoogleCallback()
        {
            var authResult = await HttpContext.AuthenticateAsync(IdentityConstants.ExternalScheme);

            // Google is used only to read a verified email for this one request — the
            // transient external cookie is discarded immediately either way, so nothing
            // here ever becomes a persistent site login.
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            var email = authResult.Succeeded ? authResult.Principal?.FindFirstValue(ClaimTypes.Email) : null;
            if (string.IsNullOrWhiteSpace(email))
            {
                TempData["ApplyError"] = "We couldn't get your email from Google. Please try again or use email instead.";
                return RedirectToAction("Index");
            }

            await CreateOrReuseRequestAsync(email, AccessRequestMethod.Google);
            return RedirectToAction("RequestReceived");
        }

        [HttpPost]
        public async Task<IActionResult> SubmitEmailRequest(string email)
        {
            if (string.IsNullOrWhiteSpace(email) || !new EmailAddressAttribute().IsValid(email))
            {
                ModelState.AddModelError("", "Please enter a valid email address.");
                ViewBag.GoogleEnabled = IsGoogleConfigured();
                return View("Index");
            }

            await CreateOrReuseRequestAsync(email, AccessRequestMethod.Email);
            return RedirectToAction("RequestReceived");
        }

        public IActionResult RequestReceived()
        {
            return View();
        }

        // Step 2: the applicant clicks the link an Admin's approval emailed them
        public async Task<IActionResult> VerifyAccessCode(string token)
        {
            var request = await _context.InstructorAccessRequests
                .FirstOrDefaultAsync(r => r.AccessToken == token && r.Status == AccessRequestStatus.Approved);

            if (request == null)
            {
                return View("InvalidAccessLink");
            }

            ViewBag.Token = token;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> VerifyAccessCode(string token, string code)
        {
            var request = await _context.InstructorAccessRequests
                .FirstOrDefaultAsync(r => r.AccessToken == token && r.Status == AccessRequestStatus.Approved);

            if (request == null)
            {
                return View("InvalidAccessLink");
            }

            ViewBag.Token = token;

            if (request.IsConsumed)
            {
                ModelState.AddModelError("", "This application has already been submitted.");
                return View();
            }

            if (request.OtpExpiresAt == null || DateTime.Now > request.OtpExpiresAt)
            {
                ModelState.AddModelError("", "This code has expired. Contact the admin for a new invite.");
                return View();
            }

            if (request.OtpAttempts >= InstructorAccessRequest.MaxOtpAttempts)
            {
                ModelState.AddModelError("", "Too many incorrect attempts. Contact the admin for a new invite.");
                return View();
            }

            if (string.IsNullOrWhiteSpace(code) || code != request.OtpCode)
            {
                request.OtpAttempts++;
                await _context.SaveChangesAsync();

                var remaining = InstructorAccessRequest.MaxOtpAttempts - request.OtpAttempts;
                ModelState.AddModelError("", remaining > 0
                    ? $"That code is incorrect. Please check your email and try again ({remaining} attempt{(remaining == 1 ? "" : "s")} left)."
                    : "Too many incorrect attempts. Contact the admin for a new invite.");
                return View();
            }

            HttpContext.Session.SetInt32(SessionKey, request.Id);
            return RedirectToAction("Form");
        }

        // Step 3: the application form — only reachable after a valid access code was verified this session
        public IActionResult Form()
        {
            var requestId = HttpContext.Session.GetInt32(SessionKey);
            if (requestId == null)
            {
                return RedirectToAction("Index");
            }

            return View(new InstructorApplicationViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Form(InstructorApplicationViewModel model, IFormFile? Resume)
        {
            var requestId = HttpContext.Session.GetInt32(SessionKey);
            if (requestId == null)
            {
                return RedirectToAction("Index");
            }

            var request = await _context.InstructorAccessRequests.FindAsync(requestId.Value);
            if (request == null || request.Status != AccessRequestStatus.Approved || request.IsConsumed)
            {
                HttpContext.Session.Remove(SessionKey);
                ModelState.AddModelError("", "This access link is no longer valid. Please request a new one.");
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

            string resumePath;
            try
            {
                resumePath = await _fileUploadService.SavePrivateFileAsync(
                    Resume!, "resumes", UploadPolicy.ResumeExtensions, UploadPolicy.ResumeMaxSizeBytes);
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
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
                ResumePath = resumePath,
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

            request.IsConsumed = true;
            request.ConsumedAt = System.DateTime.Now;
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

        private async Task CreateOrReuseRequestAsync(string email, AccessRequestMethod method)
        {
            var existingPending = await _context.InstructorAccessRequests
                .FirstOrDefaultAsync(r => r.Email == email && r.Status == AccessRequestStatus.Pending);
            if (existingPending != null) return;

            var request = new InstructorAccessRequest { Email = email, Method = method };
            _context.InstructorAccessRequests.Add(request);
            await _context.SaveChangesAsync();

            foreach (var admin in await _userManager.GetUsersInRoleAsync("Admin"))
            {
                await _notificationService.NotifyAsync(
                    admin.Id,
                    $"New instructor access request from {email}",
                    "/Admin/Admin/AccessRequests");
            }
        }

        private bool IsGoogleConfigured() =>
            !string.IsNullOrWhiteSpace(_configuration["Authentication:Google:ClientId"]) &&
            !string.IsNullOrWhiteSpace(_configuration["Authentication:Google:ClientSecret"]);

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
