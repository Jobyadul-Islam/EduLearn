using System;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace EduLearn.Models
{
    public class Review
    {
        public int Id { get; set; }
        public string StudentId { get; set; }
        public int CourseId { get; set; }

        [ValidateNever]
        public Course Course { get; set; }

        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
