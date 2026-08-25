using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace EduLearn.Models
{
    public class Course
    {
        public int Id { get; set; }
        public string Title { get; set; }
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