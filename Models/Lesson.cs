using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace EduLearn.Models
{
    public class Lesson
    {
        public int Id { get; set; }

        // Explicit [Required] on both — MVC's implicit non-nullable-reference-type
        // inference only rejects a MISSING field, and a submitted form always posts the
        // field, so an empty string would otherwise still pass ModelState.IsValid.
        [Required(ErrorMessage = "Title is required.")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Content is required.")]
        public string Content { get; set; }

        public string? VideoUrl { get; set; }
        public string? FilePath { get; set; }

        public int ModuleId { get; set; }

        [ValidateNever]
        public Module Module { get; set; }

        [ValidateNever]
        public ICollection<Assignment> Assignments { get; set; }

        [ValidateNever]
        public ICollection<Quiz> Quizzes { get; set; }
    }
}