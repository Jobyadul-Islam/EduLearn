using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

using System.Collections.Generic;

namespace EduLearn.Models
{
    public class Category
    {
        public int Id { get; set; }

        // Explicit [Required], not relying on MVC's implicit non-nullable-reference-type
        // inference — that inference only rejects a MISSING field, and a submitted form
        // always posts the field, so Name="" would otherwise still pass ModelState.IsValid.
        [Required(ErrorMessage = "Name is required.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Description is required.")]
        public string Description { get; set; }

        [ValidateNever]
        public ICollection<Course> Courses { get; set; }
    }
}