using System.Linq;
using System.Threading.Tasks;
using EduLearn.Controllers;
using EduLearn.Data;
using EduLearn.Models;
using EduLearn.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace EduLearn.Tests.Integration
{
    // MarkComplete and SubmitAssignment previously required only [Authorize] — a student
    // enrolled but never paid (Status stays Pending on a paid course) could POST either one
    // directly for ANY lesson in the course, including ones ViewLesson would never let them
    // open, mark the whole course "complete," and download a certificate for a course they
    // never paid for. Both now share CourseController.TryLoadAccessibleLesson, the same
    // enrolled+paid-or-free-preview rule ViewLesson/LessonFile already enforced.
    public class LessonAccessControlTests
    {
        private static (ApplicationDbContext context, ApplicationUser student, Course course, Lesson freeLesson, Lesson paywalledLesson, Assignment paywalledAssignment) SeedPaidCourse(EnrollmentStatus enrollmentStatus)
        {
            var context = TestHelpers.CreateInMemoryContext();

            var student = new ApplicationUser { Id = "lac-student", FullName = "Student", Email = "lac.student@example.com", UserName = "lac.student@example.com" };
            var instructor = new ApplicationUser { Id = "lac-instructor", FullName = "Instructor", Email = "lac.instr@example.com", UserName = "lac.instr@example.com" };
            context.Users.AddRange(student, instructor);
            context.Categories.Add(new Category { Id = 70, Name = "Cat", Description = "Cat" });

            var course = new Course { Id = 70, Title = "Paid Course", Description = "d", Price = 500, CategoryId = 70, InstructorId = instructor.Id, Status = CourseStatus.Approved };
            context.Courses.Add(course);

            var module = new Module { Id = 70, Title = "Module 1", CourseId = course.Id };
            context.Modules.Add(module);

            // First 2 lessons by Id are always free-preview (GetFreePreviewLessonIds) — so
            // lesson 70/71 are free, lesson 72 is the one behind the paywall.
            var free1 = new Lesson { Id = 70, Title = "Free Lesson 1", Content = "c", ModuleId = module.Id };
            var free2 = new Lesson { Id = 71, Title = "Free Lesson 2", Content = "c", ModuleId = module.Id };
            var paywalled = new Lesson { Id = 72, Title = "Paywalled Lesson", Content = "c", ModuleId = module.Id };
            context.Lessons.AddRange(free1, free2, paywalled);

            var assignment = new Assignment { Id = 70, Title = "Paywalled Assignment", Description = "d", LessonId = paywalled.Id, DueDate = System.DateTime.Now.AddDays(7) };
            context.Assignments.Add(assignment);

            context.Enrollments.Add(new Enrollment { Id = 70, CourseId = course.Id, StudentId = student.Id, EnrollDate = System.DateTime.Now, Status = enrollmentStatus });

            context.SaveChanges();
            return (context, student, course, free1, paywalled, assignment);
        }

        private static CourseController CreateController(ApplicationDbContext context, ApplicationUser actingAs)
        {
            var userManager = TestHelpers.CreateMockUserManager(actingAs);
            var controller = new CourseController(context, userManager.Object, Mock.Of<IWebHostEnvironment>(),
                TestHelpers.CreateFakeEmailService().Object, Mock.Of<INotificationService>(), TestHelpers.CreateFakeFileUploadService().Object);
            TestHelpers.AttachControllerContext(controller, actingAs.Id);
            return controller;
        }

        // ---------------- MarkComplete ----------------

        [Fact]
        public void MarkComplete_UnpaidStudent_OnAPaywalledLesson_IsForbidden_AndNoProgressIsSaved()
        {
            var (context, student, _, _, paywalledLesson, _) = SeedPaidCourse(EnrollmentStatus.Pending);
            using var _c = context;
            var controller = CreateController(context, student);

            var result = controller.MarkComplete(paywalledLesson.Id);

            Assert.IsType<ForbidResult>(result);
            Assert.Empty(context.LessonProgresses);
        }

        [Fact]
        public void MarkComplete_UnpaidStudent_OnAFreePreviewLesson_StillWorks()
        {
            var (context, student, _, freeLesson, _, _) = SeedPaidCourse(EnrollmentStatus.Pending);
            using var _c = context;
            var controller = CreateController(context, student);

            var result = controller.MarkComplete(freeLesson.Id);

            Assert.IsType<RedirectToActionResult>(result);
            Assert.True(context.LessonProgresses.Single(p => p.LessonId == freeLesson.Id).IsCompleted);
        }

        [Fact]
        public void MarkComplete_PaidStudent_OnAPaywalledLesson_Works()
        {
            var (context, student, _, _, paywalledLesson, _) = SeedPaidCourse(EnrollmentStatus.Active);
            using var _c = context;
            var controller = CreateController(context, student);

            var result = controller.MarkComplete(paywalledLesson.Id);

            Assert.IsType<RedirectToActionResult>(result);
            Assert.True(context.LessonProgresses.Single(p => p.LessonId == paywalledLesson.Id).IsCompleted);
        }

        [Fact]
        public void MarkComplete_ForANonExistentLesson_ReturnsNotFound()
        {
            var (context, student, _, _, _, _) = SeedPaidCourse(EnrollmentStatus.Active);
            using var _c = context;
            var controller = CreateController(context, student);

            var result = controller.MarkComplete(999999);

            Assert.IsType<NotFoundResult>(result);
        }

        // ---------------- SubmitAssignment ----------------

        [Fact]
        public void SubmitAssignment_Get_UnpaidStudent_OnAPaywalledAssignment_IsForbidden()
        {
            var (context, student, _, _, _, paywalledAssignment) = SeedPaidCourse(EnrollmentStatus.Pending);
            using var _c = context;
            var controller = CreateController(context, student);

            var result = controller.SubmitAssignment(paywalledAssignment.Id);

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task SubmitAssignment_Post_UnpaidStudent_OnAPaywalledAssignment_IsForbidden_AndNoSubmissionIsSaved()
        {
            var (context, student, _, _, _, paywalledAssignment) = SeedPaidCourse(EnrollmentStatus.Pending);
            using var _c = context;
            var controller = CreateController(context, student);
            var file = new Mock<IFormFile>();
            file.Setup(f => f.Length).Returns(10);

            var result = await controller.SubmitAssignment(paywalledAssignment.Id, file.Object);

            Assert.IsType<ForbidResult>(result);
            Assert.Empty(context.AssignmentSubmissions);
        }

        [Fact]
        public async Task SubmitAssignment_Post_PaidStudent_OnAPaywalledAssignment_Works()
        {
            var (context, student, _, _, _, paywalledAssignment) = SeedPaidCourse(EnrollmentStatus.Active);
            using var _c = context;
            var controller = CreateController(context, student);
            var file = new Mock<IFormFile>();
            file.Setup(f => f.Length).Returns(10);

            var result = await controller.SubmitAssignment(paywalledAssignment.Id, file.Object);

            Assert.IsType<RedirectToActionResult>(result);
            Assert.Single(context.AssignmentSubmissions);
        }

        // ---------------- End-to-end: the original reported exploit path is now closed ----------------

        [Fact]
        public void UnpaidStudent_CannotMarkEveryLessonComplete_ToUnlockACertificateForAPaidCourse()
        {
            var (context, student, course, freeLesson, paywalledLesson, _) = SeedPaidCourse(EnrollmentStatus.Pending);
            using var _c = context;
            var controller = CreateController(context, student);
            TestHelpers.AttachControllerContext(controller, student.Id); // TempData needed for Certificate's redirect branch

            controller.MarkComplete(freeLesson.Id); // allowed — free preview
            var blocked = controller.MarkComplete(paywalledLesson.Id); // must be refused
            Assert.IsType<ForbidResult>(blocked);

            // Only 2 of the course's 3 lessons are marked complete, so the certificate
            // action must still refuse (via its own separate, pre-existing IsComplete
            // check) — this proves the paywalled lesson genuinely never got marked.
            var certResult = controller.Certificate(course.Id);
            Assert.IsType<RedirectToActionResult>(certResult);
        }
    }
}
