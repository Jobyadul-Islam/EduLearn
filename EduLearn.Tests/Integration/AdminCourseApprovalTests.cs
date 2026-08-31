using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EduLearn.Areas.Admin.Controllers;
using EduLearn.Models;
using EduLearn.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
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
            mockUserManager.Setup(m => m.UpdateAsync(applicant)).ReturnsAsync(IdentityResult.Success);
            mockUserManager.Setup(m => m.GeneratePasswordResetTokenAsync(applicant)).ReturnsAsync("fake-reset-token");

            var mockNotificationService = new Mock<INotificationService>();
            var admin = new AdminController(context, mockUserManager.Object, Mock.Of<IEmailService>(), mockNotificationService.Object);
            TestHelpers.AttachControllerContext(admin, "some-admin-id");

            var actionContext = new ActionContext(
                admin.ControllerContext.HttpContext,
                new Microsoft.AspNetCore.Routing.RouteData(),
                new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor { RouteValues = new System.Collections.Generic.Dictionary<string, string?>() });

            var mockUrlHelper = new Mock<IUrlHelper>();
            mockUrlHelper.Setup(u => u.ActionContext).Returns(actionContext);
            mockUrlHelper.Setup(u => u.RouteUrl(It.IsAny<UrlRouteContext>())).Returns("https://localhost/Identity/Account/ResetPassword?fake=1");
            admin.Url = mockUrlHelper.Object;

            await admin.Approve(applicant.Id);

            Assert.True(applicant.IsApproved);

            mockNotificationService.Verify(m => m.NotifyAsync(
                applicant.Id,
                It.Is<string>(msg => msg.Contains("approved")),
                "/Instructor"), Times.Once);
        }

        [Fact]
        public async Task Reject_InstructorAccount_ArchivesTheApplication_DeletesTheAccount_AndEmailsThemPolitely()
        {
            using var context = TestHelpers.CreateInMemoryContext();

            var appliedAt = new System.DateTime(2026, 1, 10);
            var applicant = new ApplicationUser
            {
                Id = "admin-test-applicant-2",
                FullName = "Rejected Applicant",
                Email = "rejected@example.com",
                UserName = "rejected@example.com",
                IsApproved = false,
                PhoneNumber = "0170000000",
                Qualification = "MSc",
                Institution = "Test University",
                Skills = "Testing",
                YearsOfExperience = 3,
                Bio = "A bio",
                ResumePath = "/uploads/resumes/fake.pdf",
                CreatedAt = appliedAt
            };
            context.Users.Add(applicant);
            context.SaveChanges();

            var mockUserManager = TestHelpers.CreateMockUserManager(applicant);
            mockUserManager.Setup(m => m.FindByIdAsync(applicant.Id)).ReturnsAsync(applicant);
            mockUserManager.Setup(m => m.UpdateAsync(applicant)).ReturnsAsync(IdentityResult.Success);
            mockUserManager.Setup(m => m.DeleteAsync(applicant)).ReturnsAsync(IdentityResult.Success);

            var mockEmailService = new Mock<IEmailService>();
            mockEmailService.Setup(m => m.SendEmailAsync(applicant.Email, It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);

            var mockNotificationService = new Mock<INotificationService>();
            var admin = new AdminController(context, mockUserManager.Object, mockEmailService.Object, mockNotificationService.Object);
            TestHelpers.AttachControllerContext(admin, "some-admin-id");

            await admin.Reject(applicant.Id);

            // The account itself is what gets deleted (via UserManager, verified below) — this
            // just confirms the safety-net flags were set on it first, in case delete had failed.
            Assert.False(applicant.IsApproved);
            Assert.True(applicant.IsRejected);
            Assert.False(applicant.IsActive);

            mockUserManager.Verify(m => m.DeleteAsync(applicant), Times.Once);

            var archived = Assert.Single(context.RejectedApplicationArchives);
            Assert.Equal("Rejected Applicant", archived.FullName);
            Assert.Equal("rejected@example.com", archived.Email);
            Assert.Equal("MSc", archived.Qualification);
            Assert.Equal("Test University", archived.Institution);
            Assert.Equal("/uploads/resumes/fake.pdf", archived.ResumePath);
            Assert.Equal(appliedAt, archived.AppliedAt);

            mockEmailService.Verify(m => m.SendEmailAsync(
                "rejected@example.com",
                It.IsAny<string>(),
                It.Is<string>(body => !body.Contains("Password"))), Times.Once);

            // No in-app notification — the account is deleted on rejection, so the applicant
            // could never log in to see one; email is the only channel that reaches them.
            mockNotificationService.Verify(m => m.NotifyAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Index_ExcludesRejectedInstructorsFromAllCounts()
        {
            using var context = TestHelpers.CreateInMemoryContext();

            var admin = new ApplicationUser { Id = "dash-admin", FullName = "Admin", Email = "dashadmin@example.com", UserName = "dashadmin@example.com" };
            var activeInstructor = new ApplicationUser { Id = "dash-instr-active", FullName = "Active Instructor", Email = "activeinstr@example.com", UserName = "activeinstr@example.com" };
            var rejectedInstructor = new ApplicationUser { Id = "dash-instr-rejected", FullName = "Rejected Instructor", Email = "rejectedinstr@example.com", UserName = "rejectedinstr@example.com", IsRejected = true, IsApproved = false, IsActive = false };
            var student = new ApplicationUser { Id = "dash-student", FullName = "Student", Email = "dashstudent@example.com", UserName = "dashstudent@example.com" };

            context.Users.AddRange(admin, activeInstructor, rejectedInstructor, student);
            context.SaveChanges();

            var mockUserManager = TestHelpers.CreateMockUserManager(admin);
            mockUserManager.Setup(m => m.GetUsersInRoleAsync("Student")).ReturnsAsync(new List<ApplicationUser> { student });
            mockUserManager.Setup(m => m.GetUsersInRoleAsync("Instructor")).ReturnsAsync(new List<ApplicationUser> { activeInstructor, rejectedInstructor });

            var admin_ = new AdminController(context, mockUserManager.Object, Mock.Of<IEmailService>(), Mock.Of<INotificationService>());

            var result = await admin_.Index() as ViewResult;

            Assert.NotNull(result);
            // 4 seeded users total, minus the 1 rejected one = 3 — the same exclusion the
            // Manage Users list applies, so the dashboard and the list never disagree.
            Assert.Equal(3, (int)result!.ViewData["TotalUsers"]!);
            Assert.Equal(1, (int)result.ViewData["TotalStudents"]!);
            Assert.Equal(1, (int)result.ViewData["TotalInstructors"]!);
        }
    }
}
