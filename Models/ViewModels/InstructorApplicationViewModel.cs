using System.ComponentModel.DataAnnotations;

namespace EduLearn.Models.ViewModels
{
    public class InstructorApplicationViewModel
    {
        [Required]
        public string FullName { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        [Required]
        public string PhoneNumber { get; set; }

        [Required]
        public string Qualification { get; set; }

        [Required]
        public string Institution { get; set; }

        [Required]
        public string Skills { get; set; }

        [Required, Range(0, 60)]
        public int YearsOfExperience { get; set; }

        [Required]
        public string Bio { get; set; }
    }
}
