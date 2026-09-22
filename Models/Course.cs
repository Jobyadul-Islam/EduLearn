using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace EduLearn.Models
{
    public class Course
    {
        public int Id { get; set; }

        // Explicit [Required] on purpose: with <Nullable>enable</Nullable>, MVC infers an
        // implicit "required" for a non-nullable string, but that inference only rejects a
        // MISSING field — a submitted form always posts the field, so Title="" still binds
        // and passes ModelState.IsValid without this attribute (confirmed via the MVC
        // validation pipeline directly). RequiredAttribute rejects empty/whitespace too.
        [Required(ErrorMessage = "Title is required.")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Description is required.")]
        public string Description { get; set; }

        [ValidateNever]
        public string? CourseCode { get; set; }   // e.g. "EDU-0007"

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        public int CategoryId { get; set; }

        [ValidateNever]
        public Category Category { get; set; }

        [ValidateNever]
        public string InstructorId { get; set; }

        [ValidateNever]
        [ForeignKey("InstructorId")]
        public ApplicationUser Instructor { get; set; }

        public string? ThumbnailPath { get; set; }

        public CourseStatus Status { get; set; } = CourseStatus.Pending;
        public string? RejectionReason { get; set; }

        [ValidateNever]
        public ICollection<Module> Modules { get; set; }

        [ValidateNever]
        public ICollection<Enrollment> Enrollments { get; set; }

        [ValidateNever]
        public ICollection<Review> Reviews { get; set; }
    }
}