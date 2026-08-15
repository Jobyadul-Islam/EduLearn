using Microsoft.AspNetCore.Identity;

namespace EduLearn.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; }
        public string? ProfilePicture { get; set; }
        public bool IsApproved { get; set; } = true;
        public bool IsActive { get; set; } = true;
    }
}