using System.Linq;
using System.Threading.Tasks;
using EduLearn.Controllers;
using EduLearn.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace EduLearn.Tests.Integration
{
    public class EnrollmentAndCertificateFlowTests
    {
        static EnrollmentAndCertificateFlowTests()
        {
            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
        }

        [Fact]
        public async Task FullFlow_EnrollCompleteLessons_ThenCertificateBecomesAvailable()
        {
            using var context = TestHelpers.CreateInMemoryContext();

            var student = new ApplicationUser { Id = "student-1", FullName = "Jane Student", Email = "jane@example.com", UserName = "jane@example.com" };
            var instructor = new ApplicationUser { Id = "instructor-1", FullName = "Ivy Instructor", Email = "ivy@example.com", UserName = "ivy@example.com" };
            context.Users.AddRange(student, instructor);

            var category = new Category { Id = 1, Name = "Programming", Description = "Programming courses" };
            context.Categories.Add(category);

            var course = new Course
            {
                Id = 1,
                Title = "Intro to C#",
                Description = "Learn C# basics",
                Price = 0,
                CategoryId = category.Id,
                InstructorId = instructor.Id,
                Status = CourseStatus.Approved
            };
            context.Courses.Add(course);

            var module = new Module { Id = 1, Title = "Module 1", CourseId = course.Id };
            context.Modules.Add(module);

            var lesson1 = new Lesson { Id = 1, Title = "Lesson 1", Content = "Content 1", ModuleId = module.Id };
            var lesson2 = new Lesson { Id = 2, Title = "Lesson 2", Content = "Content 2", ModuleId = module.Id };
            context.Lessons.AddRange(lesson1, lesson2);
            context.SaveChanges();

            var mockUserManager = TestHelpers.CreateMockUserManager(student);
            var mockEmailService = TestHelpers.CreateFakeEmailService();

            var controller = new CourseController(context, mockUserManager.Object, Mock.Of<IWebHostEnvironment>(), mockEmailService.Object);
            TestHelpers.AttachControllerContext(controller, student.Id);

            // 1. Enroll (free course -> should start Active with immediate full access)
            var enrollResult = await controller.Enroll(course.Id);
            Assert.IsType<RedirectToActionResult>(enrollResult);

            var enrollment = context.Enrollments.Single(e => e.CourseId == course.Id && e.StudentId == student.Id);
            Assert.Equal(EnrollmentStatus.Active, enrollment.Status);

            // 2. Certificate should NOT be available yet — no lessons completed
            var earlyCertResult = controller.Certificate(course.Id);
            var redirect = Assert.IsType<RedirectToActionResult>(earlyCertResult);
            Assert.Equal("MyEnrollments", redirect.ActionName);
            Assert.Equal("Complete every lesson in this course to unlock your certificate.", controller.TempData["CertificateError"]);

            // 3. Complete only ONE of the two lessons — still shouldn't unlock
            controller.MarkComplete(lesson1.Id);
            var partialCertResult = controller.Certificate(course.Id);
            Assert.IsType<RedirectToActionResult>(partialCertResult);

            // 4. Complete the second lesson — certificate should now be a real PDF file
            controller.MarkComplete(lesson2.Id);
            var finalCertResult = controller.Certificate(course.Id);

            var fileResult = Assert.IsType<FileContentResult>(finalCertResult);
            Assert.Equal("application/pdf", fileResult.ContentType);
            Assert.True(fileResult.FileContents.Length > 0);
        }

        [Fact]
        public async Task Enroll_CalledTwice_DoesNotCreateDuplicateEnrollment()
        {
            using var context = TestHelpers.CreateInMemoryContext();

            var student = new ApplicationUser { Id = "student-2", FullName = "Sam Student", Email = "sam@example.com", UserName = "sam@example.com" };
            var instructor = new ApplicationUser { Id = "instructor-2", FullName = "Ivy Instructor", Email = "ivy2@example.com", UserName = "ivy2@example.com" };
            context.Users.AddRange(student, instructor);

            var category = new Category { Id = 2, Name = "Data", Description = "Data courses" };
            context.Categories.Add(category);

            var course = new Course
            {
                Id = 2,
                Title = "Intro to Python",
                Description = "Learn Python basics",
                Price = 0,
                CategoryId = category.Id,
                InstructorId = instructor.Id,
                Status = CourseStatus.Approved
            };
            context.Courses.Add(course);
            context.SaveChanges();

            var mockUserManager = TestHelpers.CreateMockUserManager(student);
            var mockEmailService = TestHelpers.CreateFakeEmailService();

            var controller = new CourseController(context, mockUserManager.Object, Mock.Of<IWebHostEnvironment>(), mockEmailService.Object);
            TestHelpers.AttachControllerContext(controller, student.Id);

            await controller.Enroll(course.Id);
            await controller.Enroll(course.Id);

            var enrollmentCount = context.Enrollments.Count(e => e.CourseId == course.Id && e.StudentId == student.Id);
            Assert.Equal(1, enrollmentCount);
        }
    }
}
