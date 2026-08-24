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
    public class RegisterModel : PageModel
    {
        public const string SessionKey = "PendingRegistration";

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;

        public RegisterModel(UserManager<ApplicationUser> userManager, IEmailService emailService)
        {
            _userManager = userManager;
            _emailService = emailService;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required]
            public string FullName { get; set; }

            [Required, EmailAddress]
            public string Email { get; set; }

            [Required, DataType(DataType.Password)]
            public string Password { get; set; }

            [DataType(DataType.Password), Compare("Password")]
            public string ConfirmPassword { get; set; }
        }

        // Held in session (not the database) until the OTP is verified, so an
        // unverified registration never leaves a half-created account behind.
        public class PendingRegistration
        {
            public string FullName { get; set; }
            public string Email { get; set; }
            public string Password { get; set; }
            public string Otp { get; set; }
            public DateTime ExpiresAt { get; set; }
        }

        public void OnGet() { }

        // Public self-registration only ever creates Student accounts.
        // Instructors go through the PIN-gated application flow at /Apply instead.
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            if (await _userManager.FindByEmailAsync(Input.Email) != null)
            {
                ModelState.AddModelError("Input.Email", "An account with this email already exists.");
                return Page();
            }

            var otp = new Random().Next(0, 1000000).ToString("D6");

            var pending = new PendingRegistration
            {
                FullName = Input.FullName,
                Email = Input.Email,
                Password = Input.Password,
                Otp = otp,
                ExpiresAt = DateTime.Now.AddMinutes(10)
            };
            HttpContext.Session.SetString(SessionKey, JsonSerializer.Serialize(pending));

            var body = $@"
                <p>Hi {System.Net.WebUtility.HtmlEncode(Input.FullName)},</p>
                <p>Your EduLearn verification code is:</p>
                <p style=""font-size:28px; font-weight:bold; letter-spacing:4px;"">{otp}</p>
                <p>This code expires in 10 minutes. If you didn't request this, you can ignore this email.</p>
                <p>— EduLearn</p>";
            await _emailService.SendEmailAsync(Input.Email, "Your EduLearn verification code", body);

            return RedirectToPage("./VerifyOtp");
        }
    }
}
