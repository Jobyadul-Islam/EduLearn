using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace EduLearn.Models
{
    public class ProfileViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? ProfilePicture { get; set; }
        public string? Bio { get; set; }
        public string Role { get; set; } = string.Empty;
        public bool IsViewingOwnProfile { get; set; }
    }

    public class ProfileEditViewModel
    {
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