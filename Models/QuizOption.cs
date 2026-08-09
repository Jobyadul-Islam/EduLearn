using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace EduLearn.Models
{
    public class QuizOption
    {
        public int Id { get; set; }
        public string OptionText { get; set; }
        public bool IsCorrect { get; set; }
        public int QuizQuestionId { get; set; }

        [ValidateNever]
        public QuizQuestion QuizQuestion { get; set; }
    }
}