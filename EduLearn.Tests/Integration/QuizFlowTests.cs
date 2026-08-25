using System.Collections.Generic;
using System.Linq;
using EduLearn.Controllers;
using EduLearn.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace EduLearn.Tests.Integration
{
    public class QuizFlowTests
    {
        private static (Category category, Course course, Module module, Lesson lesson, Quiz quiz) SeedCourseWithQuiz(
            EduLearn.Data.ApplicationDbContext context, string instructorId, int passMarkPercentage = 60)
        {
            var category = new Category { Id = 10, Name = "Programming", Description = "Programming courses" };
            context.Categories.Add(category);

            var course = new Course
            {
                Id = 10,
                Title = ".NET Fundamentals",
                Description = "Learn .NET",
                Price = 0,
                CategoryId = category.Id,
                InstructorId = instructorId,
                Status = CourseStatus.Approved
            };
            context.Courses.Add(course);

            var module = new Module { Id = 10, Title = "Module 1", CourseId = course.Id };
            context.Modules.Add(module);

            var lesson = new Lesson { Id = 10, Title = "Lesson 1", Content = "Content", ModuleId = module.Id };
            context.Lessons.Add(lesson);

            var quiz = new Quiz
            {
                Id = 10,
                Title = "Basics Quiz",
                LessonId = lesson.Id,
                PassMarkPercentage = passMarkPercentage,
                TimeLimitMinutes = 10,
                Questions = new List<QuizQuestion>
                {
                    new QuizQuestion
                    {
                        Id = 10,
                        QuestionText = "Q1",
                        Options = new List<QuizOption>
                        {
                            new QuizOption { Id = 10, OptionText = "Right", IsCorrect = true },
                            new QuizOption { Id = 11, OptionText = "Wrong", IsCorrect = false }
                        }
                    },
                    new QuizQuestion
                    {
                        Id = 11,
                        QuestionText = "Q2",
                        Options = new List<QuizOption>
                        {
                            new QuizOption { Id = 12, OptionText = "Right", IsCorrect = true },
                            new QuizOption { Id = 13, OptionText = "Wrong", IsCorrect = false }
                        }
                    }
                }
            };
            context.Quizzes.Add(quiz);
            context.SaveChanges();

            return (category, course, module, lesson, quiz);
        }

        [Fact]
        public void SubmitQuiz_FullyCorrect_PersistsPassingResult()
        {
            using var context = TestHelpers.CreateInMemoryContext();

            var student = new ApplicationUser { Id = "quiz-student-1", FullName = "Q Student", Email = "qs1@example.com", UserName = "qs1@example.com" };
            var instructor = new ApplicationUser { Id = "quiz-instructor-1", FullName = "Q Instructor", Email = "qi1@example.com", UserName = "qi1@example.com" };
            context.Users.AddRange(student, instructor);

            var (_, _, _, _, quiz) = SeedCourseWithQuiz(context, instructor.Id);

            var mockUserManager = TestHelpers.CreateMockUserManager(student);
            var controller = new CourseController(context, mockUserManager.Object, Mock.Of<IWebHostEnvironment>(), Mock.Of<EduLearn.Services.IEmailService>());
            TestHelpers.AttachControllerContext(controller, student.Id);

            var result = controller.SubmitQuiz(quiz.Id, new List<int> { 10, 12 });

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("QuizResult", redirect.ActionName);

            var stored = context.QuizResults.Single(r => r.QuizId == quiz.Id && r.StudentId == student.Id);
            Assert.Equal(2, stored.Score);
            Assert.Equal(2, stored.TotalQuestions);
            Assert.True(stored.Passed);
        }

        [Fact]
        public void SubmitQuiz_RetakeWithWorseScore_UpdatesExistingRowInPlace()
        {
            using var context = TestHelpers.CreateInMemoryContext();

            var student = new ApplicationUser { Id = "quiz-student-2", FullName = "Q Student 2", Email = "qs2@example.com", UserName = "qs2@example.com" };
            var instructor = new ApplicationUser { Id = "quiz-instructor-2", FullName = "Q Instructor 2", Email = "qi2@example.com", UserName = "qi2@example.com" };
            context.Users.AddRange(student, instructor);

            var (_, _, _, _, quiz) = SeedCourseWithQuiz(context, instructor.Id);

            var mockUserManager = TestHelpers.CreateMockUserManager(student);
            var controller = new CourseController(context, mockUserManager.Object, Mock.Of<IWebHostEnvironment>(), Mock.Of<EduLearn.Services.IEmailService>());
            TestHelpers.AttachControllerContext(controller, student.Id);

            controller.SubmitQuiz(quiz.Id, new List<int> { 10, 12 }); // both correct
            controller.SubmitQuiz(quiz.Id, new List<int> { 11, 13 }); // retake, both wrong

            var results = context.QuizResults.Where(r => r.QuizId == quiz.Id && r.StudentId == student.Id).ToList();
            Assert.Single(results); // must update in place, not accumulate attempt history
            Assert.Equal(0, results[0].Score);
            Assert.False(results[0].Passed);
        }

        [Fact]
        public void InstructorQuizResults_ShowsStudentScoreForOwnedCourse_ButNotForOtherInstructorsCourses()
        {
            using var context = TestHelpers.CreateInMemoryContext();

            var student = new ApplicationUser { Id = "quiz-student-3", FullName = "Q Student 3", Email = "qs3@example.com", UserName = "qs3@example.com" };
            var owningInstructor = new ApplicationUser { Id = "quiz-instructor-owner", FullName = "Owner Instructor", Email = "owner@example.com", UserName = "owner@example.com" };
            var otherInstructor = new ApplicationUser { Id = "quiz-instructor-other", FullName = "Other Instructor", Email = "other@example.com", UserName = "other@example.com" };
            context.Users.AddRange(student, owningInstructor, otherInstructor);

            var (_, course, _, _, quiz) = SeedCourseWithQuiz(context, owningInstructor.Id);

            var studentUserManager = TestHelpers.CreateMockUserManager(student);
            var courseController = new CourseController(context, studentUserManager.Object, Mock.Of<IWebHostEnvironment>(), Mock.Of<EduLearn.Services.IEmailService>());
            TestHelpers.AttachControllerContext(courseController, student.Id);
            courseController.SubmitQuiz(quiz.Id, new List<int> { 10, 12 });

            // The owning instructor should see this result...
            var owningInstructorUserManager = TestHelpers.CreateMockUserManager(owningInstructor);
            var ownerController = new InstructorController(context, owningInstructorUserManager.Object, Mock.Of<IWebHostEnvironment>());
            TestHelpers.AttachControllerContext(ownerController, owningInstructor.Id);

            var ownerViewResult = Assert.IsType<ViewResult>(ownerController.QuizResults(null));
            var ownerRows = Assert.IsAssignableFrom<IEnumerable<dynamic>>(ownerViewResult.Model).Cast<dynamic>().ToList();
            Assert.Single(ownerRows);
            Assert.Equal(student.FullName, (string)ownerRows[0].StudentName);
            Assert.Equal(course.Title, (string)ownerRows[0].CourseTitle);
            Assert.True((bool)ownerRows[0].Passed);

            // ...but a DIFFERENT instructor must see nothing, even with no course filter applied.
            var otherInstructorUserManager = TestHelpers.CreateMockUserManager(otherInstructor);
            var otherController = new InstructorController(context, otherInstructorUserManager.Object, Mock.Of<IWebHostEnvironment>());
            TestHelpers.AttachControllerContext(otherController, otherInstructor.Id);

            var otherViewResult = Assert.IsType<ViewResult>(otherController.QuizResults(null));
            var otherRows = Assert.IsAssignableFrom<IEnumerable<dynamic>>(otherViewResult.Model).Cast<dynamic>().ToList();
            Assert.Empty(otherRows);
        }
    }
}
