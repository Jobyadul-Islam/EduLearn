using System.Linq;
using EduLearn.Areas.Admin.Controllers;
using EduLearn.Models;
using Microsoft.AspNetCore.Identity;
using Moq;
using Xunit;

namespace EduLearn.Tests.Integration
{
    public class AdminCourseApprovalTests
    {
        [Fact]
        public void ApproveCourse_ChangesStatusToApproved_AndClearsAnyPriorRejectionReason()
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
            var admin = new AdminController(context, mockUserManager.Object, Mock.Of<EduLearn.Services.IEmailService>());

            admin.ApproveCourse(course.Id);

            var updated = context.Courses.Single(c => c.Id == course.Id);
            Assert.Equal(CourseStatus.Approved, updated.Status);
            Assert.Null(updated.RejectionReason);
        }

        [Fact]
        public void RejectCourse_ChangesStatusToRejected_AndStoresTrimmedReason()
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
            var admin = new AdminController(context, mockUserManager.Object, Mock.Of<EduLearn.Services.IEmailService>());

            admin.RejectCourse(course.Id, "  Please add a thumbnail  ");

            var updated = context.Courses.Single(c => c.Id == course.Id);
            Assert.Equal(CourseStatus.Rejected, updated.Status);
            Assert.Equal("Please add a thumbnail", updated.RejectionReason);
        }

        [Fact]
        public void RejectCourse_WithBlankReason_StoresNullNotEmptyString()
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
            var admin = new AdminController(context, mockUserManager.Object, Mock.Of<EduLearn.Services.IEmailService>());

            admin.RejectCourse(course.Id, "   ");

            var updated = context.Courses.Single(c => c.Id == course.Id);
            Assert.Equal(CourseStatus.Rejected, updated.Status);
            Assert.Null(updated.RejectionReason);
        }
    }
}
