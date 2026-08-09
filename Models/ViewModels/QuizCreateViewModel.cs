using System.Collections.Generic;

namespace EduLearn.Models.ViewModels
{
    public class QuizCreateViewModel
    {
        public int LessonId { get; set; }
        public string Title { get; set; }
        public List<QuestionViewModel> Questions { get; set; } = new List<QuestionViewModel>();
    }

    public class QuestionViewModel
    {
        public string QuestionText { get; set; }
        public List<OptionViewModel> Options { get; set; } = new List<OptionViewModel>();
    }

    public class OptionViewModel
    {
        public string OptionText { get; set; }
        public bool IsCorrect { get; set; }
    }
}