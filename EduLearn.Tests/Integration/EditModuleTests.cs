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
    public class EditModuleTests
    {
        private static (ApplicationDbContext context, ApplicationUser owner, Module ownedModule, Module otherModule, int ownedCourseId) Seed()
        {
            var context = TestHelpers.CreateInMemoryContext();

            var owner = new ApplicationUser { Id = "edit-module-owner", FullName = "Owner", Email = "owner3@example.com", UserName = "owner3@example.com", IsApproved = true };
            var other = new ApplicationUser { Id = "edit-module-other", FullName = "Other", Email = "other3@example.com", UserName = "other3@example.com", IsApproved = true };
            context.Users.AddRange(owner, other);
            context.Categories.Add(new Category { Id = 97, Name = "Cat", Description = "Cat" });

            var ownedCourse = new Course { Id = 97, Title = "Owned Course", Description = "d", CategoryId = 97, InstructorId = owner.Id, Status = CourseStatus.Approved };
            var otherCourse = new Course { Id = 98, Title = "Other's Course", Description = "d", CategoryId = 97, InstructorId = other.Id, Status = CourseStatus.Approved };
            context.Courses.AddRange(ownedCourse, otherCourse);

            var ownedModule = new Module { Id = 97, Title = "Original Title", CourseId = ownedCourse.Id };
            var otherModule = new Module { Id = 98, Title = "Other's Module", CourseId = otherCourse.Id };
            context.Modules.AddRange(ownedModule, otherModule);

            context.SaveChanges();
            return (context, owner, ownedModule, otherModule, ownedCourse.Id);
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
        public void Get_ForOwnModule_ReturnsIt()
        {
            var (context, owner, ownedModule, _, _) = Seed();
            using var _c = context;
            var controller = CreateController(context, owner);

            var result = controller.EditModule(ownedModule.Id);

            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<Module>(view.Model);
            Assert.Equal("Original Title", model.Title);
        }

        [Fact]
        public void Get_ForAnotherInstructorsModule_ReturnsNotFound()
        {
            var (context, owner, _, otherModule, _) = Seed();
            using var _c = context;
            var controller = CreateController(context, owner);

            var result = controller.EditModule(otherModule.Id);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public void Post_WithAValidTitle_UpdatesIt_AndRedirectsToCourseDetails()
        {
            var (context, owner, ownedModule, _, ownedCourseId) = Seed();
            using var _c = context;
            var controller = CreateController(context, owner);
            var posted = new Module { Title = "Updated Title" };
            controller.TryValidateModel(posted);

            var result = controller.EditModule(ownedModule.Id, posted);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("CourseDetails", redirect.ActionName);
            Assert.Equal(ownedCourseId, redirect.RouteValues["id"]);
            Assert.Equal("Updated Title", context.Modules.Single(m => m.Id == ownedModule.Id).Title);
        }

        [Fact]
        public void Post_ForAnotherInstructorsModule_ReturnsNotFound_AndSavesNothing()
        {
            var (context, owner, _, otherModule, _) = Seed();
            using var _c = context;
            var controller = CreateController(context, owner);
            var posted = new Module { Title = "Sneaky Edit" };

            var result = controller.EditModule(otherModule.Id, posted);

            Assert.IsType<NotFoundResult>(result);
            Assert.Equal("Other's Module", context.Modules.Single(m => m.Id == otherModule.Id).Title);
        }

        [Fact]
        public void Post_WithBlankTitle_IsRejected_AndTheModuleIsUnchanged()
        {
            var (context, owner, ownedModule, _, ownedCourseId) = Seed();
            using var _c = context;
            var controller = CreateController(context, owner);
            var posted = new Module { Title = "   " };
            controller.TryValidateModel(posted);

            var result = controller.EditModule(ownedModule.Id, posted);

            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<Module>(view.Model);
            Assert.False(controller.ModelState.IsValid);
            Assert.Equal(ownedModule.Id, model.Id);
            Assert.Equal(ownedCourseId, model.CourseId); // so the Cancel link still works
            Assert.Equal("Original Title", context.Modules.Single(m => m.Id == ownedModule.Id).Title);
        }
    }
}
