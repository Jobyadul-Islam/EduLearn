using System.Linq;
using EduLearn.Controllers;
using EduLearn.Data;
using EduLearn.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace EduLearn.Tests.Integration
{
    public class EditLessonTests
    {
        private static (ApplicationDbContext context, ApplicationUser owner, Lesson ownedLesson, Lesson otherLesson, int ownedCourseId) Seed()
        {
            var context = TestHelpers.CreateInMemoryContext();

            var owner = new ApplicationUser { Id = "edit-lesson-owner", FullName = "Owner", Email = "owner2@example.com", UserName = "owner2@example.com", IsApproved = true };
            var other = new ApplicationUser { Id = "edit-lesson-other", FullName = "Other", Email = "other2@example.com", UserName = "other2@example.com", IsApproved = true };
            context.Users.AddRange(owner, other);
            context.Categories.Add(new Category { Id = 95, Name = "Cat", Description = "Cat" });

            var ownedCourse = new Course { Id = 95, Title = "Owned Course", Description = "d", CategoryId = 95, InstructorId = owner.Id, Status = CourseStatus.Approved };
            var otherCourse = new Course { Id = 96, Title = "Other's Course", Description = "d", CategoryId = 95, InstructorId = other.Id, Status = CourseStatus.Approved };
            context.Courses.AddRange(ownedCourse, otherCourse);

            var ownedModule = new Module { Id = 95, Title = "Owned Module", CourseId = ownedCourse.Id };
            var otherModule = new Module { Id = 96, Title = "Other's Module", CourseId = otherCourse.Id };
            context.Modules.AddRange(ownedModule, otherModule);

            var ownedLesson = new Lesson { Id = 95, Title = "Original Title", Content = "Original content", VideoUrl = null, FilePath = "/uploads/lessons/original.pdf", ModuleId = ownedModule.Id };
            var otherLesson = new Lesson { Id = 96, Title = "Other's Lesson", Content = "c", ModuleId = otherModule.Id };
            context.Lessons.AddRange(ownedLesson, otherLesson);

            context.SaveChanges();
            return (context, owner, ownedLesson, otherLesson, ownedCourse.Id);
        }

        private static InstructorController CreateController(ApplicationDbContext context, ApplicationUser actingAs)
        {
            var userManager = TestHelpers.CreateMockUserManager(actingAs);
            var controller = new InstructorController(context, userManager.Object, Mock.Of<IWebHostEnvironment>(), TestHelpers.CreateFakeFileUploadService().Object);
            TestHelpers.AttachControllerContext(controller, actingAs.Id);
            TestHelpers.AttachValidation(controller);
            return controller;
        }

        [Fact]
        public void Get_ForOwnLesson_ReturnsTheLessonWithCourseIdInViewBag()
        {
            var (context, owner, ownedLesson, _, ownedCourseId) = Seed();
            using var _c = context;
            var controller = CreateController(context, owner);

            var result = controller.EditLesson(ownedLesson.Id);

            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<Lesson>(view.Model);
            Assert.Equal("Original Title", model.Title);
            Assert.Equal(ownedCourseId, (int)controller.ViewBag.CourseId);
        }

        [Fact]
        public void Get_ForAnotherInstructorsLesson_ReturnsNotFound()
        {
            var (context, owner, _, otherLesson, _) = Seed();
            using var _c = context;
            var controller = CreateController(context, owner);

            var result = controller.EditLesson(otherLesson.Id);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async System.Threading.Tasks.Task Post_WithValidChanges_UpdatesTitleContentAndVideoUrl_AndRedirectsToCourseDetails()
        {
            var (context, owner, ownedLesson, _, ownedCourseId) = Seed();
            using var _c = context;
            var controller = CreateController(context, owner);
            var posted = new Lesson { Title = "Updated Title", Content = "Updated content", VideoUrl = "https://example.com/video" };
            controller.TryValidateModel(posted);

            var result = await controller.EditLesson(ownedLesson.Id, posted, null);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("CourseDetails", redirect.ActionName);
            Assert.Equal(ownedCourseId, redirect.RouteValues["id"]);

            var saved = context.Lessons.Single(l => l.Id == ownedLesson.Id);
            Assert.Equal("Updated Title", saved.Title);
            Assert.Equal("Updated content", saved.Content);
            Assert.Equal("https://example.com/video", saved.VideoUrl);
            Assert.Equal("/uploads/lessons/original.pdf", saved.FilePath); // untouched — no new file posted
        }

        [Fact]
        public async System.Threading.Tasks.Task Post_ForAnotherInstructorsLesson_ReturnsNotFound_AndSavesNothing()
        {
            var (context, owner, _, otherLesson, _) = Seed();
            using var _c = context;
            var controller = CreateController(context, owner);
            var posted = new Lesson { Title = "Sneaky Edit", Content = "c" };

            var result = await controller.EditLesson(otherLesson.Id, posted, null);

            Assert.IsType<NotFoundResult>(result);
            Assert.Equal("Other's Lesson", context.Lessons.Single(l => l.Id == otherLesson.Id).Title);
        }

        [Fact]
        public async System.Threading.Tasks.Task Post_WithBlankTitle_IsRejected_AndTheLessonIsUnchanged()
        {
            var (context, owner, ownedLesson, _, _) = Seed();
            using var _c = context;
            var controller = CreateController(context, owner);
            var posted = new Lesson { Title = "   ", Content = "Updated content" };
            controller.TryValidateModel(posted);

            var result = await controller.EditLesson(ownedLesson.Id, posted, null);

            Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);
            var unchanged = context.Lessons.Single(l => l.Id == ownedLesson.Id);
            Assert.Equal("Original Title", unchanged.Title);
            Assert.Equal("Original content", unchanged.Content);
        }

        [Fact]
        public async System.Threading.Tasks.Task Post_OnValidationFailure_RedisplaysWithTheOriginalIdAndFilePath_SoTheFormStillWorks()
        {
            var (context, owner, ownedLesson, _, ownedCourseId) = Seed();
            using var _c = context;
            var controller = CreateController(context, owner);
            var posted = new Lesson { Title = "", Content = "x" };
            controller.TryValidateModel(posted);

            var result = await controller.EditLesson(ownedLesson.Id, posted, null);

            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<Lesson>(view.Model);
            Assert.Equal(ownedLesson.Id, model.Id);
            Assert.Equal("/uploads/lessons/original.pdf", model.FilePath);
            Assert.Equal(ownedCourseId, (int)controller.ViewBag.CourseId);
        }
    }
}
