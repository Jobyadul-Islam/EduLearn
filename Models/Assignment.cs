using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace EduLearn.Models
{
    public class Assignment
    {
        public int Id { get; set; }

        // Explicit [Required] on both — MVC's implicit non-nullable-reference-type
        // inference only rejects a MISSING field, and a submitted form always posts the
        // field, so an empty string would otherwise still pass ModelState.IsValid.
        [Required(ErrorMessage = "Title is required.")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Description is required.")]
        public string Description { get; set; }

        public DateTime DueDate { get; set; }

        public int LessonId { get; set; }

        [ValidateNever]
        public Lesson Lesson { get; set; }
    }
}