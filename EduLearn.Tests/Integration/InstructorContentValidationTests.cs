using System.Collections.Generic;
using System.Linq;
using EduLearn.Controllers;
using EduLearn.Data;
using EduLearn.Models;
using EduLearn.Models.ViewModels;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace EduLearn.Tests.Integration
{
    // Covers three gaps found and fixed together: (1) Course/Category/Module/Lesson/
    // Assignment accepted a blank title/description because MVC's implicit "required" for a
    // non-nullable string only rejects a MISSING field, not an empty one a submitted form
    // still posts; (2) CreateModule/CreateLesson/CreateAssignment/CreateQuiz trusted the
    // posted parent id with no check it belonged to the caller; (3) CreateQuiz had no
    // validation at all — not even ModelState.IsValid.
    public class InstructorContentValidationTests
    {
        private static (ApplicationDbContext context, ApplicationUser owner, ApplicationUser other, Course ownedCourse, Module ownedModule, Lesson ownedLesson, Course otherCourse, Module otherModule, Lesson otherLesson) Seed()
        {
            var context = TestHelpers.CreateInMemoryContext();

            var owner = new ApplicationUser { Id = "owner-instr", FullName = "Owner", Email = "owner@example.com", UserName = "owner@example.com", IsApproved = true };
            var other = new ApplicationUser { Id = "other-instr", FullName = "Other", Email = "other@example.com", UserName = "other@example.com", IsApproved = true };
            context.Users.AddRange(owner, other);
            context.Categories.Add(new Category { Id = 90, Name = "Cat", Description = "Cat" });

            var ownedCourse = new Course { Id = 90, Title = "Owned Course", Description = "d", CategoryId = 90, InstructorId = owner.Id, Status = CourseStatus.Approved };
            var otherCourse = new Course { Id = 91, Title = "Other's Course", Description = "d", CategoryId = 90, InstructorId = other.Id, Status = CourseStatus.Approved };
            context.Courses.AddRange(ownedCourse, otherCourse);

            var ownedModule = new Module { Id = 90, Title = "Owned Module", CourseId = ownedCourse.Id };
            var otherModule = new Module { Id = 91, Title = "Other's Module", CourseId = otherCourse.Id };
            context.Modules.AddRange(ownedModule, otherModule);

            var ownedLesson = new Lesson { Id = 90, Title = "Owned Lesson", Content = "c", ModuleId = ownedModule.Id };
            var otherLesson = new Lesson { Id = 91, Title = "Other's Lesson", Content = "c", ModuleId = otherModule.Id };
            context.Lessons.AddRange(ownedLesson, otherLesson);

            context.SaveChanges();
            return (context, owner, other, ownedCourse, ownedModule, ownedLesson, otherCourse, otherModule, otherLesson);
        }

        private static InstructorController CreateController(ApplicationDbContext context, ApplicationUser actingAs)
        {
            var userManager = TestHelpers.CreateMockUserManager(actingAs);
            var controller = new InstructorController(context, userManager.Object, Mock.Of<IWebHostEnvironment>(), TestHelpers.CreateFakeFileUploadService().Object);
            TestHelpers.AttachControllerContext(controller, actingAs.Id);
            TestHelpers.AttachValidation(controller);
            return controller;
        }

        // ---------------- Empty-string validation ----------------

        [Fact]
        public void CreateModule_WithBlankTitle_IsRejected_AndNoRowIsSaved()
        {
            var (context, owner, _, ownedCourse, _, _, _, _, _) = Seed();
            using var _c = context;
            var controller = CreateController(context, owner);
            var module = new Module { Title = "   ", CourseId = ownedCourse.Id };
            controller.TryValidateModel(module);

            var result = controller.CreateModule(module);

            Assert.IsType<ViewResult>(result);
            Assert.False(context.Modules.Any(m => m.CourseId == ownedCourse.Id && m.Title == "   "));
        }

        [Fact]
        public void CreateAssignment_WithBlankTitle_IsRejected_AndNoRowIsSaved()
        {
            var (context, owner, _, _, _, ownedLesson, _, _, _) = Seed();
            using var _c = context;
            var controller = CreateController(context, owner);
            var assignment = new Assignment { Title = "", Description = "", LessonId = ownedLesson.Id, DueDate = System.DateTime.Now.AddDays(7) };
            controller.TryValidateModel(assignment);

            var result = controller.CreateAssignment(assignment);

            Assert.IsType<ViewResult>(result);
            Assert.Empty(context.Assignments);
        }

        // ---------------- Ownership checks ----------------

        [Fact]
        public void CreateModule_Get_ForAnotherInstructorsCourse_ReturnsNotFound()
        {
            var (context, owner, _, _, _, _, otherCourse, _, _) = Seed();
            using var _c = context;
            var controller = CreateController(context, owner);

            var result = controller.CreateModule(otherCourse.Id);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public void CreateModule_Post_ForAnotherInstructorsCourse_ReturnsNotFound_AndSavesNothing()
        {
            var (context, owner, _, _, _, _, otherCourse, _, _) = Seed();
            using var _c = context;
            var controller = CreateController(context, owner);
            var module = new Module { Title = "Sneaky Module", CourseId = otherCourse.Id };

            var result = controller.CreateModule(module);

            Assert.IsType<NotFoundResult>(result);
            Assert.False(context.Modules.Any(m => m.Title == "Sneaky Module"));
        }

        [Fact]
        public void CreateLesson_Post_ForAnotherInstructorsModule_ReturnsNotFound_AndSavesNothing()
        {
            var (context, owner, _, _, _, _, _, otherModule, _) = Seed();
            using var _c = context;
            var controller = CreateController(context, owner);
            var lesson = new Lesson { Title = "Sneaky Lesson", Content = "c", ModuleId = otherModule.Id };

            var result = controller.CreateLesson(lesson, null).GetAwaiter().GetResult();

            Assert.IsType<NotFoundResult>(result);
            Assert.False(context.Lessons.Any(l => l.ModuleId == otherModule.Id && l.Title == "Sneaky Lesson"));
        }

        [Fact]
        public void CreateAssignment_Post_ForAnotherInstructorsLesson_ReturnsNotFound_AndSavesNothing()
        {
            var (context, owner, _, _, _, _, _, _, otherLesson) = Seed();
            using var _c = context;
            var controller = CreateController(context, owner);
            var assignment = new Assignment { Title = "Sneaky", Description = "d", LessonId = otherLesson.Id, DueDate = System.DateTime.Now.AddDays(7) };

            var result = controller.CreateAssignment(assignment);

            Assert.IsType<NotFoundResult>(result);
            Assert.False(context.Assignments.Any(a => a.LessonId == otherLesson.Id));
        }

        [Fact]
        public void CreateQuiz_Get_ForAnotherInstructorsLesson_ReturnsNotFound()
        {
            var (context, owner, _, _, _, _, _, _, otherLesson) = Seed();
            using var _c = context;
            var controller = CreateController(context, owner);

            var result = controller.CreateQuiz(otherLesson.Id);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public void CreateQuiz_Post_ForAnotherInstructorsLesson_ReturnsNotFound_AndSavesNothing()
        {
            var (context, owner, _, _, _, _, _, _, otherLesson) = Seed();
            using var _c = context;
            var controller = CreateController(context, owner);
            var model = ValidQuizModel(otherLesson.Id);

            var result = controller.CreateQuiz(model);

            Assert.IsType<NotFoundResult>(result);
            Assert.Empty(context.Quizzes);
        }

        // ---------------- CreateQuiz validation (previously entirely absent) ----------------

        [Fact]
        public void CreateQuiz_WithNoQuestions_IsRejected_AndSavesNothing()
        {
            var (context, owner, _, _, _, ownedLesson, _, _, _) = Seed();
            using var _c = context;
            var controller = CreateController(context, owner);
            var model = new QuizCreateViewModel { LessonId = ownedLesson.Id, Title = "Empty Quiz" };
            controller.TryValidateModel(model);

            var result = controller.CreateQuiz(model);

            Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);
            Assert.Empty(context.Quizzes);
        }

        [Fact]
        public void CreateQuiz_WhereNoOptionIsMarkedCorrect_IsRejected_AndSavesNothing()
        {
            var (context, owner, _, _, _, ownedLesson, _, _, _) = Seed();
            using var _c = context;
            var controller = CreateController(context, owner);
            var model = ValidQuizModel(ownedLesson.Id);
            model.Questions[0].Options.ForEach(o => o.IsCorrect = false); // no correct answer at all
            controller.TryValidateModel(model);

            var result = controller.CreateQuiz(model);

            Assert.IsType<ViewResult>(result);
            Assert.Empty(context.Quizzes);
        }

        [Fact]
        public void CreateQuiz_WithBlankTitle_IsRejected_AndSavesNothing()
        {
            var (context, owner, _, _, _, ownedLesson, _, _, _) = Seed();
            using var _c = context;
            var controller = CreateController(context, owner);
            var model = ValidQuizModel(ownedLesson.Id);
            model.Title = "  ";
            controller.TryValidateModel(model);

            var result = controller.CreateQuiz(model);

            Assert.IsType<ViewResult>(result);
            Assert.Empty(context.Quizzes);
        }

        [Fact]
        public void CreateQuiz_WellFormed_IsAcceptedAndSaved()
        {
            var (context, owner, _, _, _, ownedLesson, _, _, _) = Seed();
            using var _c = context;
            var controller = CreateController(context, owner);
            var model = ValidQuizModel(ownedLesson.Id);
            controller.TryValidateModel(model);
            Assert.True(controller.ModelState.IsValid); // sanity: the model itself is well-formed

            var result = controller.CreateQuiz(model);

            Assert.IsType<RedirectToActionResult>(result);
            var saved = Assert.Single(context.Quizzes);
            Assert.Equal("Chapter 1 Quiz", saved.Title);
        }

        private static QuizCreateViewModel ValidQuizModel(int lessonId) => new QuizCreateViewModel
        {
            LessonId = lessonId,
            Title = "Chapter 1 Quiz",
            Questions = new List<QuestionViewModel>
            {
                new QuestionViewModel
                {
                    QuestionText = "2 + 2 = ?",
                    Options = new List<OptionViewModel>
                    {
                        new OptionViewModel { OptionText = "4", IsCorrect = true },
                        new OptionViewModel { OptionText = "5", IsCorrect = false }
                    }
                }
            }
        };
    }
}
