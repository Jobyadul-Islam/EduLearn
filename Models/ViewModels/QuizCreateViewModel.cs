using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace EduLearn.Models.ViewModels
{
    // IValidatableObject.Validate runs as part of ModelState.IsValid, alongside the plain
    // DataAnnotations below — this is where the cross-field quiz-authoring rules live (at
    // least one question, every question has text, at least two options with text, and at
    // least one option marked correct), since none of those can be expressed as a single
    // attribute on one property.
    public class QuizCreateViewModel : IValidatableObject
    {
        public int LessonId { get; set; }

        [Required(ErrorMessage = "Give the quiz a title.")]
        public string Title { get; set; }

        [Range(0, 100, ErrorMessage = "Pass mark must be between 0 and 100.")]
        public int PassMarkPercentage { get; set; } = 60;

        [Range(1, int.MaxValue, ErrorMessage = "Time limit must be at least 1 minute.")]
        public int TimeLimitMinutes { get; set; } = 10;

        public DateTime DueDate { get; set; } = DateTime.Now.AddDays(7);

        public List<QuestionViewModel> Questions { get; set; } = new List<QuestionViewModel>();

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Questions == null || Questions.Count == 0)
            {
                yield return new ValidationResult("Add at least one question.", new[] { nameof(Questions) });
                yield break;
            }

            for (int i = 0; i < Questions.Count; i++)
            {
                var q = Questions[i];
                var label = $"Question {i + 1}";

                if (string.IsNullOrWhiteSpace(q.QuestionText))
                    yield return new ValidationResult($"{label} needs question text.", new[] { nameof(Questions) });

                if (q.Options == null || q.Options.Count < 2)
                {
                    yield return new ValidationResult($"{label} needs at least two options.", new[] { nameof(Questions) });
                    continue;
                }

                if (q.Options.Any(o => string.IsNullOrWhiteSpace(o.OptionText)))
                    yield return new ValidationResult($"{label} has an option with no text.", new[] { nameof(Questions) });

                if (!q.Options.Any(o => o.IsCorrect))
                    yield return new ValidationResult($"{label} needs at least one correct option checked.", new[] { nameof(Questions) });
            }
        }
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