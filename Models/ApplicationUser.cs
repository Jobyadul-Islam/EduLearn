using System;
using Microsoft.AspNetCore.Identity;

namespace EduLearn.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; }
        public string? ProfilePicture { get; set; }
        public bool IsApproved { get; set; } = true;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Instructor application details (populated only for instructor applicants)
        public string? Qualification { get; set; }
        public string? Institution { get; set; }
        public string? Skills { get; set; }
        public int? YearsOfExperience { get; set; }
        public string? Bio { get; set; }
        public string? ResumePath { get; set; }
    }
}