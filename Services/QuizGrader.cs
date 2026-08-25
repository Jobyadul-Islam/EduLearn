using System.Collections.Generic;
using System.Linq;
using EduLearn.Models;

namespace EduLearn.Services
{
    public class QuizGradeResult
    {
        public int Score { get; set; }
        public int TotalQuestions { get; set; }
        public bool Passed { get; set; }
    }

    public static class QuizGrader
    {
        public static QuizGradeResult Grade(Quiz quiz, IEnumerable<int>? selectedOptionIds)
        {
            var selectedSet = new HashSet<int>(selectedOptionIds ?? Enumerable.Empty<int>());

            int score = 0;
            foreach (var question in quiz.Questions)
            {
                var correctSet = question.Options.Where(o => o.IsCorrect).Select(o => o.Id).ToHashSet();
                var selectedForQuestion = question.Options.Select(o => o.Id).Where(selectedSet.Contains).ToHashSet();

                if (correctSet.SetEquals(selectedForQuestion))
                {
                    score++;
                }
            }

            int totalQuestions = quiz.Questions.Count;
            bool passed = totalQuestions > 0 && (100.0 * score / totalQuestions) >= quiz.PassMarkPercentage;

            return new QuizGradeResult
            {
                Score = score,
                TotalQuestions = totalQuestions,
                Passed = passed
            };
        }
    }
}
