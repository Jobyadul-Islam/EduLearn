using System;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace EduLearn.Models
{
    public class LessonProgress
    {
        public int Id { get; set; }
        public string StudentId { get; set; }
        public int LessonId { get; set; }

        [ValidateNever]
        public Lesson Lesson { get; set; }

        public bool IsCompleted { get; set; }
        public DateTime? CompletedAt { get; set; }
    }
}