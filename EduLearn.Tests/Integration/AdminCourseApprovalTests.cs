using System.Linq;
using System.Threading.Tasks;
using EduLearn.Areas.Admin.Controllers;
using EduLearn.Models;
using EduLearn.Services;
using Microsoft.AspNetCore.Identity;
using Moq;
using Xunit;

namespace EduLearn.Tests.Integration
{
    public class AdminCourseApprovalTests
    {
        [Fact]
        public async Task ApproveCourse_ChangesStatusToApproved_AndClearsAnyPriorRejectionReason()
        {
            using var context = TestHelpers.CreateInMemoryContext();

            var instructor = new ApplicationUser { Id = "admin-test-instructor-1", FullName = "Instructor", Email = "i1@example.com", UserName = "i1@example.com" };
            context.Users.Add(instructor);

            var category = new Category { Id = 20, Name = "Cat", Description = "Cat" };
            context.Categories.Add(category);

            var course = new Course
            {
                Id = 20,
                Title = "Resubmitted Course",
                Description = "desc",
                CategoryId = category.Id,
                InstructorId = instructor.Id,
                Status = CourseStatus.Rejected,
                RejectionReason = "Needs more content"
            };
            context.Courses.Add(course);
            context.SaveChanges();

            var mockUserManager = TestHelpers.CreateMockUserManager(instructor);
            var mockNotificationService = new Mock<INotificationService>();
            var admin = new AdminController(context, mockUserManager.Object, Mock.Of<IEmailService>(), mockNotificationService.Object);

            await admin.ApproveCourse(course.Id);

            var updated = context.Courses.Single(c => c.Id == course.Id);
            Assert.Equal(CourseStatus.Approved, updated.Status);
            Assert.Null(updated.RejectionReason);

            mockNotificationService.Verify(m => m.NotifyAsync(
                instructor.Id,
                It.Is<string>(msg => msg.Contains(course.Title) && msg.Contains("approved")),
                $"/Instructor/CourseDetails/{course.Id}"), Times.Once);
        }

        [Fact]
        public async Task RejectCourse_ChangesStatusToRejected_AndStoresTrimmedReason()
        {
            using var context = TestHelpers.CreateInMemoryContext();

            var instructor = new ApplicationUser { Id = "admin-test-instructor-2", FullName = "Instructor 2", Email = "i2@example.com", UserName = "i2@example.com" };
            context.Users.Add(instructor);

            var category = new Category { Id = 21, Name = "Cat2", Description = "Cat2" };
            context.Categories.Add(category);

            var course = new Course
            {
                Id = 21,
                Title = "New Course",
                Description = "desc",
                CategoryId = category.Id,
                InstructorId = instructor.Id,
                Status = CourseStatus.Pending
            };
            context.Courses.Add(course);
            context.SaveChanges();

            var mockUserManager = TestHelpers.CreateMockUserManager(instructor);
            var mockNotificationService = new Mock<INotificationService>();
            var admin = new AdminController(context, mockUserManager.Object, Mock.Of<IEmailService>(), mockNotificationService.Object);

            await admin.RejectCourse(course.Id, "  Please add a thumbnail  ");

            var updated = context.Courses.Single(c => c.Id == course.Id);
            Assert.Equal(CourseStatus.Rejected, updated.Status);
            Assert.Equal("Please add a thumbnail", updated.RejectionReason);

            mockNotificationService.Verify(m => m.NotifyAsync(
                instructor.Id,
                It.Is<string>(msg => msg.Contains("Please add a thumbnail")),
                $"/Instructor/EditCourse/{course.Id}"), Times.Once);
        }

        [Fact]
        public async Task RejectCourse_WithBlankReason_StoresNullNotEmptyString()
        {
            using var context = TestHelpers.CreateInMemoryContext();

            var instructor = new ApplicationUser { Id = "admin-test-instructor-3", FullName = "Instructor 3", Email = "i3@example.com", UserName = "i3@example.com" };
            context.Users.Add(instructor);

            var category = new Category { Id = 22, Name = "Cat3", Description = "Cat3" };
            context.Categories.Add(category);

            var course = new Course
            {
                Id = 22,
                Title = "Another Course",
                Description = "desc",
                CategoryId = category.Id,
                InstructorId = instructor.Id,
                Status = CourseStatus.Pending
            };
            context.Courses.Add(course);
            context.SaveChanges();

            var mockUserManager = TestHelpers.CreateMockUserManager(instructor);
            var mockNotificationService = new Mock<INotificationService>();
            var admin = new AdminController(context, mockUserManager.Object, Mock.Of<IEmailService>(), mockNotificationService.Object);

            await admin.RejectCourse(course.Id, "   ");

            var updated = context.Courses.Single(c => c.Id == course.Id);
            Assert.Equal(CourseStatus.Rejected, updated.Status);
            Assert.Null(updated.RejectionReason);

            // No reason was given, so the notification message must not claim one exists.
            mockNotificationService.Verify(m => m.NotifyAsync(
                instructor.Id,
                It.Is<string>(msg => !msg.Contains("Reason:")),
                $"/Instructor/EditCourse/{course.Id}"), Times.Once);
        }

        [Fact]
        public async Task Approve_InstructorAccount_NotifiesThatInstructor()
        {
            using var context = TestHelpers.CreateInMemoryContext();

            var applicant = new ApplicationUser { Id = "admin-test-applicant-1", FullName = "New Instructor", Email = "newinstr@example.com", UserName = "newinstr@example.com", IsApproved = false };
            context.Users.Add(applicant);
            context.SaveChanges();

            var mockUserManager = TestHelpers.CreateMockUserManager(applicant);
            mockUserManager.Setup(m => m.FindByIdAsync(applicant.Id)).ReturnsAsync(applicant);
            mockUserManager.Setup(m => m.RemovePasswordAsync(applicant)).ReturnsAsync(IdentityResult.Success);
            mockUserManager.Setup(m => m.AddPasswordAsync(applicant, It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);
            mockUserManager.Setup(m => m.UpdateAsync(applicant)).ReturnsAsync(IdentityResult.Success);

            var mockNotificationService = new Mock<INotificationService>();
            var admin = new AdminController(context, mockUserManager.Object, Mock.Of<IEmailService>(), mockNotificationService.Object);
            TestHelpers.AttachControllerContext(admin, "some-admin-id");

            await admin.Approve(applicant.Id, "NewPassword123!");

            mockNotificationService.Verify(m => m.NotifyAsync(
                applicant.Id,
                It.Is<string>(msg => msg.Contains("approved")),
                "/Instructor"), Times.Once);
        }
    }
}
