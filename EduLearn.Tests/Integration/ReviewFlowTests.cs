using System.Linq;
using EduLearn.Controllers;
using EduLearn.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace EduLearn.Tests.Integration
{
    public class ReviewFlowTests
    {
        private static (Course course, Lesson lesson) SeedCourseWithOneLesson(EduLearn.Data.ApplicationDbContext context, string instructorId)
        {
            var category = new Category { Id = 30, Name = "Cat", Description = "Cat" };
            context.Categories.Add(category);

            var course = new Course
            {
                Id = 30,
                Title = "Reviewable Course",
                Description = "desc",
                CategoryId = category.Id,
                InstructorId = instructorId,
                Status = CourseStatus.Approved
            };
            context.Courses.Add(course);

            var module = new Module { Id = 30, Title = "Module", CourseId = course.Id };
            context.Modules.Add(module);

            var lesson = new Lesson { Id = 30, Title = "Lesson", Content = "Content", ModuleId = module.Id };
            context.Lessons.Add(lesson);
            context.SaveChanges();

            return (course, lesson);
        }

        [Fact]
        public void WriteReview_CourseNotCompleted_BlocksSubmissionAndCreatesNoReview()
        {
            using var context = TestHelpers.CreateInMemoryContext();

            var student = new ApplicationUser { Id = "review-student-1", FullName = "R Student", Email = "rs1@example.com", UserName = "rs1@example.com" };
            var instructor = new ApplicationUser { Id = "review-instructor-1", FullName = "R Instructor", Email = "ri1@example.com", UserName = "ri1@example.com" };
            context.Users.AddRange(student, instructor);

            var (course, _) = SeedCourseWithOneLesson(context, instructor.Id);
            context.Enrollments.Add(new Enrollment { CourseId = course.Id, StudentId = student.Id, Status = EnrollmentStatus.Active, EnrollDate = System.DateTime.Now });
            context.SaveChanges();

            var mockUserManager = TestHelpers.CreateMockUserManager(student);
            var controller = new CourseController(context, mockUserManager.Object, Mock.Of<IWebHostEnvironment>(), Mock.Of<EduLearn.Services.IEmailService>());
            TestHelpers.AttachControllerContext(controller, student.Id);

            var result = controller.WriteReview(course.Id, 5, "Great course!");

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MyEnrollments", redirect.ActionName);
            Assert.Equal("Complete every lesson in this course to leave a review.", controller.TempData["ReviewError"]);
            Assert.Empty(context.Reviews);
        }

        [Fact]
        public void WriteReview_AfterCompletingCourse_CreatesReview_ButBlocksASecondOne()
        {
            using var context = TestHelpers.CreateInMemoryContext();

            var student = new ApplicationUser { Id = "review-student-2", FullName = "R Student 2", Email = "rs2@example.com", UserName = "rs2@example.com" };
            var instructor = new ApplicationUser { Id = "review-instructor-2", FullName = "R Instructor 2", Email = "ri2@example.com", UserName = "ri2@example.com" };
            context.Users.AddRange(student, instructor);

            var (course, lesson) = SeedCourseWithOneLesson(context, instructor.Id);
            context.Enrollments.Add(new Enrollment { CourseId = course.Id, StudentId = student.Id, Status = EnrollmentStatus.Active, EnrollDate = System.DateTime.Now });
            context.LessonProgresses.Add(new LessonProgress { LessonId = lesson.Id, StudentId = student.Id, IsCompleted = true, CompletedAt = System.DateTime.Now });
            context.SaveChanges();

            var mockUserManager = TestHelpers.CreateMockUserManager(student);
            var controller = new CourseController(context, mockUserManager.Object, Mock.Of<IWebHostEnvironment>(), Mock.Of<EduLearn.Services.IEmailService>());
            TestHelpers.AttachControllerContext(controller, student.Id);

            // First review should go through.
            var firstResult = controller.WriteReview(course.Id, 4, "Pretty good");
            var firstRedirect = Assert.IsType<RedirectToActionResult>(firstResult);
            Assert.Equal("Details", firstRedirect.ActionName);
            Assert.Single(context.Reviews);

            // A second attempt by the same student on the same course must be blocked.
            var secondResult = controller.WriteReview(course.Id, 1, "Changed my mind");
            var secondRedirect = Assert.IsType<RedirectToActionResult>(secondResult);
            Assert.Equal("MyEnrollments", secondRedirect.ActionName);
            Assert.Equal("You've already reviewed this course.", controller.TempData["ReviewError"]);
            Assert.Single(context.Reviews); // still just the one
        }

        [Fact]
        public void WriteReview_RatingOutOfRange_IsRejected()
        {
            using var context = TestHelpers.CreateInMemoryContext();

            var student = new ApplicationUser { Id = "review-student-3", FullName = "R Student 3", Email = "rs3@example.com", UserName = "rs3@example.com" };
            var instructor = new ApplicationUser { Id = "review-instructor-3", FullName = "R Instructor 3", Email = "ri3@example.com", UserName = "ri3@example.com" };
            context.Users.AddRange(student, instructor);

            var (course, lesson) = SeedCourseWithOneLesson(context, instructor.Id);
            context.Enrollments.Add(new Enrollment { CourseId = course.Id, StudentId = student.Id, Status = EnrollmentStatus.Active, EnrollDate = System.DateTime.Now });
            context.LessonProgresses.Add(new LessonProgress { LessonId = lesson.Id, StudentId = student.Id, IsCompleted = true, CompletedAt = System.DateTime.Now });
            context.SaveChanges();

            var mockUserManager = TestHelpers.CreateMockUserManager(student);
            var controller = new CourseController(context, mockUserManager.Object, Mock.Of<IWebHostEnvironment>(), Mock.Of<EduLearn.Services.IEmailService>());
            TestHelpers.AttachControllerContext(controller, student.Id);

            var result = controller.WriteReview(course.Id, 0, "invalid rating");

            Assert.IsType<RedirectToActionResult>(result);
            Assert.Empty(context.Reviews);
        }
    }
}
