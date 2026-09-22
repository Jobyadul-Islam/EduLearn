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

    public class ProfileEditViewModel
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

        // Optional field, so [RegularExpression] rather than [Phone]: confirmed live that
        // RegularExpressionAttribute treats both null AND an empty string as valid (skips
        // the pattern check), so clearing a saved number still works — PhoneAttribute
        // doesn't extend that same skip to an empty string, which would make it impossible
        // to ever clear one.
        [StringLength(11, ErrorMessage = "Phone number can't be longer than 11 digits.")]
        [RegularExpression(@"^\d{11}$", ErrorMessage = "Phone number must be exactly 11 digits.")]
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
    }
}