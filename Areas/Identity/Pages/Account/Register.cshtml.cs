using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
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
        private readonly IFileUploadService _fileUploadService;

        public RegisterModel(UserManager<ApplicationUser> userManager, IEmailService emailService, IFileUploadService fileUploadService)
        {
            _userManager = userManager;
            _emailService = emailService;
            _fileUploadService = fileUploadService;
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

            [Required(ErrorMessage = "A profile picture is required.")]
            [Display(Name = "Profile Picture")]
            public IFormFile ProfilePicture { get; set; }

            // Populated client-side by the drag/zoom cropper (wwwroot/js/site.js) as a
            // data:image/jpeg;base64,... string — this, not ProfilePicture's raw bytes,
            // is what actually gets saved.
            [Required(ErrorMessage = "Please drag your photo into position before submitting.")]
            public string CroppedPictureData { get; set; }
        }

        // Held in session (not the database) until the OTP is verified, so an
        // unverified registration never leaves a half-created account behind.
        // The profile picture is likewise held only as in-memory bytes here —
        // it is never written to disk until OTP verification succeeds.
        public class PendingRegistration
        {
            public string FullName { get; set; }
            public string Email { get; set; }
            public string Password { get; set; }
            public string Otp { get; set; }
            public DateTime ExpiresAt { get; set; }
            public string ProfilePictureBase64 { get; set; }
            public string ProfilePictureExtension { get; set; }
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

            // The raw file the user picked is only used to trigger the client-side cropper —
            // what actually gets saved is the cropped square JPEG it produces, decoded here.
            const string extension = ".jpg";
            byte[] pictureBytes;
            try
            {
                pictureBytes = DecodeCroppedImage(Input.CroppedPictureData);
            }
            catch (FormatException)
            {
                ModelState.AddModelError("Input.ProfilePicture", "That image couldn't be processed. Please choose it again.");
                return Page();
            }

            try
            {
                _fileUploadService.ValidateImage(extension, pictureBytes.Length);
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("Input.ProfilePicture", ex.Message);
                return Page();
            }

            var otp = new Random().Next(0, 1000000).ToString("D6");

            var pending = new PendingRegistration
            {
                FullName = Input.FullName,
                Email = Input.Email,
                Password = Input.Password,
                Otp = otp,
                ExpiresAt = DateTime.Now.AddMinutes(10),
                ProfilePictureBase64 = Convert.ToBase64String(pictureBytes),
                ProfilePictureExtension = extension
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

        // The cropper hands back a data URL ("data:image/jpeg;base64,xxxx") — strip the
        // prefix before decoding, tolerating a bare base64 string too just in case.
        private static byte[] DecodeCroppedImage(string dataUrl)
        {
            var commaIndex = dataUrl.IndexOf(',');
            var base64 = commaIndex >= 0 ? dataUrl[(commaIndex + 1)..] : dataUrl;
            return Convert.FromBase64String(base64);
        }
    }
}