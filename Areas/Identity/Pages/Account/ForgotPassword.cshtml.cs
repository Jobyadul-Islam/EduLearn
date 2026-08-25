using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using EduLearn.Models;
using EduLearn.Services;

namespace EduLearn.Areas.Identity.Pages.Account
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;

        public ForgotPasswordModel(UserManager<ApplicationUser> userManager, IEmailService emailService)
        {
            _userManager = userManager;
            _emailService = emailService;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required, EmailAddress]
            public string Email { get; set; }
        }

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            var user = await _userManager.FindByEmailAsync(Input.Email);
            if (user != null && user.IsActive)
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                // Url.Page already URL-encodes every route value it's given, so encoding the
                // token here too would double-encode it and break validation on the other end.
                var resetLink = Url.Page(
                    "/Account/ResetPassword",
                    pageHandler: null,
                    values: new { area = "Identity", email = user.Email, code = token },
                    protocol: Request.Scheme);

                var body = $@"
                    <p>Hi {WebUtility.HtmlEncode(user.FullName)},</p>
                    <p>We received a request to reset your EduLearn password. Click the link below to choose a new one:</p>
                    <p><a href=""{resetLink}"">Reset your password</a></p>
                    <p>If you didn't request this, you can safely ignore this email.</p>
                    <p>— EduLearn</p>";

                await _emailService.SendEmailAsync(user.Email, "Reset your EduLearn password", body);
            }

            // Always redirect to the same confirmation regardless of whether the account exists,
            // so we never leak which emails are registered.
            return RedirectToPage("./ForgotPasswordConfirmation");
        }
    }
}
