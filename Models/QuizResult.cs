using System;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace EduLearn.Models
{
    public class QuizResult
    {
        public int Id { get; set; }
        public int QuizId { get; set; }

        [ValidateNever]
        public Quiz Quiz { get; set; }

        public string StudentId { get; set; }

        [ValidateNever]
        [ForeignKey("StudentId")]
        public ApplicationUser Student { get; set; }

        public int Score { get; set; }
        public int TotalQuestions { get; set; }
        public bool Passed { get; set; }
        public DateTime AttemptDate { get; set; }
    }
}
