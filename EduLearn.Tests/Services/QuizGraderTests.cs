using System.Collections.Generic;
using EduLearn.Models;
using EduLearn.Services;
using Xunit;

namespace EduLearn.Tests.Services
{
    public class QuizGraderTests
    {
        private static Quiz BuildTwoQuestionQuiz(int passMarkPercentage = 60)
        {
            return new Quiz
            {
                Id = 1,
                Title = "Sample Quiz",
                PassMarkPercentage = passMarkPercentage,
                Questions = new List<QuizQuestion>
                {
                    new QuizQuestion
                    {
                        Id = 1,
                        QuestionText = "Single-answer question",
                        Options = new List<QuizOption>
                        {
                            new QuizOption { Id = 1, OptionText = "Right", IsCorrect = true },
                            new QuizOption { Id = 2, OptionText = "Wrong", IsCorrect = false }
                        }
                    },
                    new QuizQuestion
                    {
                        Id = 2,
                        QuestionText = "Multi-answer question",
                        Options = new List<QuizOption>
                        {
                            new QuizOption { Id = 3, OptionText = "Right A", IsCorrect = true },
                            new QuizOption { Id = 4, OptionText = "Right B", IsCorrect = true },
                            new QuizOption { Id = 5, OptionText = "Wrong", IsCorrect = false }
                        }
                    }
                }
            };
        }

        [Fact]
        public void Grade_AllAnswersFullyCorrect_ScoresFullMarks()
        {
            var quiz = BuildTwoQuestionQuiz();

            var result = QuizGrader.Grade(quiz, new List<int> { 1, 3, 4 });

            Assert.Equal(2, result.Score);
            Assert.Equal(2, result.TotalQuestions);
            Assert.True(result.Passed);
        }

        [Fact]
        public void Grade_NothingSelected_ScoresZero()
        {
            var quiz = BuildTwoQuestionQuiz();

            var result = QuizGrader.Grade(quiz, new List<int>());

            Assert.Equal(0, result.Score);
            Assert.False(result.Passed);
        }

        [Fact]
        public void Grade_NullSelection_DoesNotThrowAndScoresZero()
        {
            var quiz = BuildTwoQuestionQuiz();

            var result = QuizGrader.Grade(quiz, null);

            Assert.Equal(0, result.Score);
            Assert.False(result.Passed);
        }

        [Fact]
        public void Grade_PartialMultiSelectAnswer_CountsQuestionAsWrong()
        {
            // Multi-answer question requires BOTH option 3 and 4; only selecting 3 must not earn credit.
            var quiz = BuildTwoQuestionQuiz();

            var result = QuizGrader.Grade(quiz, new List<int> { 1, 3 });

            Assert.Equal(1, result.Score);
            Assert.Equal(2, result.TotalQuestions);
        }

        [Fact]
        public void Grade_ExtraWrongOptionAlongsideCorrectOnes_CountsQuestionAsWrong()
        {
            // Selecting every correct option PLUS an incorrect one should not earn credit either.
            var quiz = BuildTwoQuestionQuiz();

            var result = QuizGrader.Grade(quiz, new List<int> { 1, 3, 4, 5 });

            Assert.Equal(1, result.Score);
        }

        [Fact]
        public void Grade_UnknownOptionIds_AreIgnoredWithoutCrashing()
        {
            var quiz = BuildTwoQuestionQuiz();

            var result = QuizGrader.Grade(quiz, new List<int> { 1, 3, 4, 9999 });

            Assert.Equal(2, result.Score);
        }

        [Fact]
        public void Grade_ScoreExactlyAtPassMark_CountsAsPassed()
        {
            // 1/2 = 50%, pass mark set to exactly 50 — boundary should be inclusive.
            var quiz = BuildTwoQuestionQuiz(passMarkPercentage: 50);

            var result = QuizGrader.Grade(quiz, new List<int> { 1 });

            Assert.Equal(1, result.Score);
            Assert.True(result.Passed);
        }

        [Fact]
        public void Grade_ScoreJustBelowPassMark_CountsAsFailed()
        {
            // 1/2 = 50%, pass mark set to 51 — should fail by a hair.
            var quiz = BuildTwoQuestionQuiz(passMarkPercentage: 51);

            var result = QuizGrader.Grade(quiz, new List<int> { 1 });

            Assert.False(result.Passed);
        }

        [Fact]
        public void Grade_QuizWithNoQuestions_NeverPasses()
        {
            var quiz = new Quiz { Id = 2, Title = "Empty", PassMarkPercentage = 0, Questions = new List<QuizQuestion>() };

            var result = QuizGrader.Grade(quiz, new List<int>());

            Assert.Equal(0, result.TotalQuestions);
            Assert.False(result.Passed);
        }
    }
}
