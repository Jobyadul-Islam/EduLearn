using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace EduLearn.Models
{
    public class Module
    {
        public int Id { get; set; }

        // Explicit [Required] — MVC's implicit non-nullable-reference-type inference only
        // rejects a MISSING field, and a submitted form always posts the field, so
        // Title="" would otherwise still pass ModelState.IsValid.
        [Required(ErrorMessage = "Title is required.")]
        public string Title { get; set; }

        public int CourseId { get; set; }

        [ValidateNever]
        public Course Course { get; set; }

        [ValidateNever]
        public ICollection<Lesson> Lessons { get; set; }
    }
}