using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.VisualBasic.FileIO;
using System.Collections.Generic;

namespace EduLearn.Models
{
    public class QuizQuestion
    {
        public int Id { get; set; }
        public string QuestionText { get; set; }
        public int QuizId { get; set; }

        [ValidateNever]
        public Quiz Quiz { get; set; }

        [ValidateNever]
        public ICollection<QuizOption> Options { get; set; }
    }
}