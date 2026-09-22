using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace EduLearn.Models
{
    public class ProfileViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? ProfilePicture { get; set; }
        public string? Bio { get; set; }
        public string Role { get; set; } = string.Empty;
        public bool IsViewingOwnProfile { get; set; }
    }

    public class ProfileEditViewModel : IValidatableObject
    {
        // Display-only — rendered readonly and never written back to the account. Changing
        // an email properly needs its own confirmation flow (Identity ties it to the login
        // username), which is out of scope here; it's shown so the edit page still shows
        // every piece of contact info at a glance, not just the ones you can change.
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Name is required.")]
        [StringLength(100, ErrorMessage = "Name can't be longer than 100 characters.")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        // No [Phone] here on purpose: PhoneAttribute treats an empty string as an invalid
        // phone number rather than "none given" (confirmed — it only skips a null value),
        // which would make it impossible to ever clear a saved phone number. The format
        // check instead runs in Validate() below, only when a value is actually present.
        [StringLength(20, ErrorMessage = "Phone number can't be longer than 20 characters.")]
        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }

        [MaxLength(500)]
        public string? Bio { get; set; }

        public string? CurrentProfilePicture { get; set; }

        public IFormFile? NewProfilePicture { get; set; } // optional on edit

        // Populated client-side by the drag/zoom cropper (wwwroot/js/site.js) as a
        // data:image/jpeg;base64,... string when a new picture was chosen — this, not
        // NewProfilePicture's raw bytes, is what actually gets saved.
        public string? CroppedPictureData { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!string.IsNullOrWhiteSpace(PhoneNumber) && !new PhoneAttribute().IsValid(PhoneNumber))
            {
                yield return new ValidationResult("Enter a valid phone number.", new[] { nameof(PhoneNumber) });
            }
        }
    }
}