using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using EduLearn.Models;
using EduLearn.Services;

namespace EduLearn.Areas.Identity.Pages.Account
{
    public class VerifyOtpModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailService _emailService;
        private readonly IFileUploadService _fileUploadService; // NEW

        public VerifyOtpModel(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IEmailService emailService, IFileUploadService fileUploadService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailService = emailService;
            _fileUploadService = fileUploadService; // NEW
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string? MaskedEmail { get; set; }

        public class InputModel
        {
            [Required, StringLength(6, MinimumLength = 6)]
            public string Code { get; set; }
        }

        public IActionResult OnGet()
        {
            var pending = LoadPending();
            if (pending == null) return RedirectToPage("./Register");

            MaskedEmail = pending.Email;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var pending = LoadPending();
            if (pending == null) return RedirectToPage("./Register");

            MaskedEmail = pending.Email;

            if (!ModelState.IsValid) return Page();

            if (DateTime.Now > pending.ExpiresAt)
            {
                HttpContext.Session.Remove(RegisterModel.SessionKey);
                ModelState.AddModelError(string.Empty, "This code has expired. Please register again.");
                return Page();
            }

            if (Input.Code != pending.Otp)
            {
                ModelState.AddModelError(string.Empty, "Incorrect code. Please try again.");
                return Page();
            }

            // OTP verified — only now does the picture ever touch disk.
            var pictureBytes = Convert.FromBase64String(pending.ProfilePictureBase64);
            var profilePicturePath = await _fileUploadService.SaveImageAsync(pictureBytes, pending.ProfilePictureExtension, "profiles");

            var user = new ApplicationUser
            {
                UserName = pending.Email,
                Email = pending.Email,
                FullName = pending.FullName,
                EmailConfirmed = true,
                ProfilePicture = profilePicturePath // NEW
            };

            var result = await _userManager.CreateAsync(user, pending.Password);
            if (!result.Succeeded)
            {
                _fileUploadService.DeleteImage(profilePicturePath); // NEW — don't leave an orphaned file behind
                HttpContext.Session.Remove(RegisterModel.SessionKey);
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
                ModelState.AddModelError(string.Empty, "Please register again with a different password.");
                return Page();
            }

            await _userManager.AddToRoleAsync(user, "Student");
            HttpContext.Session.Remove(RegisterModel.SessionKey);

            await _signInManager.SignInAsync(user, isPersistent: false);
            return RedirectToPage("/Index", new { area = "" });
        }

        public async Task<IActionResult> OnPostResendAsync()
        {
            var pending = LoadPending();
            if (pending == null) return RedirectToPage("./Register");

            pending.Otp = new Random().Next(0, 1000000).ToString("D6");
            pending.ExpiresAt = DateTime.Now.AddMinutes(10);
            HttpContext.Session.SetString(RegisterModel.SessionKey, JsonSerializer.Serialize(pending));

            var body = $@"
                <p>Hi {System.Net.WebUtility.HtmlEncode(pending.FullName)},</p>
                <p>Your new EduLearn verification code is:</p>
                <p style=""font-size:28px; font-weight:bold; letter-spacing:4px;"">{pending.Otp}</p>
                <p>This code expires in 10 minutes.</p>
                <p>— EduLearn</p>";
            await _emailService.SendEmailAsync(pending.Email, "Your EduLearn verification code", body);

            MaskedEmail = pending.Email;
            ViewData["ResendMessage"] = "A new code has been sent.";
            return Page();
        }

        private RegisterModel.PendingRegistration? LoadPending()
        {
            var json = HttpContext.Session.GetString(RegisterModel.SessionKey);
            if (string.IsNullOrEmpty(json)) return null;
            return JsonSerializer.Deserialize<RegisterModel.PendingRegistration>(json);
        }
    }
}