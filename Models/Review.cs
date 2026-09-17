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
        public DateTime? EditedAt { get; set; }

        // Public reply from the course's instructor — visible under the review on the
        // course details page, same as a normal marketplace review thread.
        public string? InstructorReply { get; set; }
        public DateTime? InstructorReplyAt { get; set; }
    }
}
