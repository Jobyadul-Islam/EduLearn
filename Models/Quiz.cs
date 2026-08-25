using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace EduLearn.Models
{
    public class Quiz
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public int PassMarkPercentage { get; set; } = 60;
        public int TimeLimitMinutes { get; set; } = 10;
        public int LessonId { get; set; }

        [ValidateNever]
        public Lesson Lesson { get; set; }

        [ValidateNever]
        public ICollection<QuizQuestion> Questions { get; set; }
    }
}