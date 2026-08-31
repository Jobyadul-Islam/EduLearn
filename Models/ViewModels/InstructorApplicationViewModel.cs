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

        // Nullable on purpose: [Required] on a non-nullable int is a no-op (0 is never
        // "null"), so a genuine 0-years answer and an accidentally-untouched field were
        // indistinguishable and both silently passed validation. Making this int? means
        // the field starts empty rather than pre-filled with a misleading "0", and
        // [Required] now actually forces the applicant to type something before submitting.
        [Required(ErrorMessage = "Please enter your years of experience (0 if you're just starting out)."), Range(0, 60)]
        public int? YearsOfExperience { get; set; }

        [Required]
        public string Bio { get; set; }
    }
}
