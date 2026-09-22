using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EduLearn.Areas.Admin.Controllers;
using EduLearn.Data;
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
            var admin = new AdminController(context, mockUserManager.Object, Mock.Of<IEmailService>(), mockNotificationService.Object, Mock.Of<EduLearn.Services.IFileUploadService>());

            await admin.ApproveCourse(course.Id, isFree: false, price: 500);

            var updated = context.Courses.Single(c => c.Id == course.Id);
            Assert.Equal(CourseStatus.Approved, updated.Status);
            Assert.Null(updated.RejectionReason);
            Assert.Equal(500, updated.Price);

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
            var admin = new AdminController(context, mockUserManager.Object, Mock.Of<IEmailService>(), mockNotificationService.Object, Mock.Of<EduLearn.Services.IFileUploadService>());

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
            var admin = new AdminController(context, mockUserManager.Object, Mock.Of<IEmailService>(), mockNotificationService.Object, Mock.Of<EduLearn.Services.IFileUploadService>());

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
            var admin = new AdminController(context, mockUserManager.Object, Mock.Of<IEmailService>(), mockNotificationService.Object, Mock.Of<EduLearn.Services.IFileUploadService>());
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
            var admin = new AdminController(context, mockUserManager.Object, mockEmailService.Object, mockNotificationService.Object, Mock.Of<EduLearn.Services.IFileUploadService>());
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

            var admin_ = new AdminController(context, mockUserManager.Object, Mock.Of<IEmailService>(), Mock.Of<INotificationService>(), Mock.Of<EduLearn.Services.IFileUploadService>());

            var result = await admin_.Index() as ViewResult;

            Assert.NotNull(result);
            // 4 seeded users total, minus the 1 rejected one = 3 — the same exclusion the
            // Manage Users list applies, so the dashboard and the list never disagree.
            Assert.Equal(3, (int)result!.ViewData["TotalUsers"]!);
            Assert.Equal(1, (int)result.ViewData["TotalStudents"]!);
            Assert.Equal(1, (int)result.ViewData["TotalInstructors"]!);
        }

        // ---------------- Refunds ----------------

        private static (ApplicationDbContext context, ApplicationUser student, Course course, Enrollment enrollment, Payment payment) SeedRefundScenario(PaymentStatus paymentStatus)
        {
            var context = TestHelpers.CreateInMemoryContext();

            var student = new ApplicationUser { Id = "refund-student", FullName = "Refund <Student>", Email = "refund.student@example.com", UserName = "refund.student@example.com" };
            var instructor = new ApplicationUser { Id = "refund-instructor", FullName = "Instructor", Email = "refund.instr@example.com", UserName = "refund.instr@example.com" };
            context.Users.AddRange(student, instructor);
            context.Categories.Add(new Category { Id = 60, Name = "Cat", Description = "Cat" });

            var course = new Course { Id = 60, Title = "Refundable Course", Description = "desc", Price = 500, CategoryId = 60, InstructorId = instructor.Id, Status = CourseStatus.Approved };
            context.Courses.Add(course);

            var enrollment = new Enrollment { Id = 60, CourseId = course.Id, StudentId = student.Id, EnrollDate = System.DateTime.Now, Status = EnrollmentStatus.Active, PaymentDate = System.DateTime.Now };
            context.Enrollments.Add(enrollment);

            var payment = new Payment { Id = 60, CourseId = course.Id, StudentId = student.Id, Amount = 500, TransactionId = "TRX-REFUND-1", Status = paymentStatus, CreatedAt = System.DateTime.Now };
            context.Payments.Add(payment);
            context.SaveChanges();

            return (context, student, course, enrollment, payment);
        }

        private static AdminController CreateAdminForRefund(ApplicationDbContext context, ApplicationUser student, Mock<IEmailService> email, Mock<INotificationService> notifications)
        {
            var mockUserManager = TestHelpers.CreateMockUserManager(student);
            mockUserManager.Setup(m => m.FindByIdAsync(student.Id)).ReturnsAsync(student);

            var admin = new AdminController(context, mockUserManager.Object, email.Object, notifications.Object, Mock.Of<EduLearn.Services.IFileUploadService>());
            TestHelpers.AttachControllerContext(admin, "some-admin-id");
            return admin;
        }

        [Fact]
        public async Task RefundPayment_MarksRefunded_RevokesAccess_AndNotifiesAndEmailsTheStudent()
        {
            var (context, student, course, enrollment, payment) = SeedRefundScenario(PaymentStatus.Success);
            using var _ = context;

            var email = new Mock<IEmailService>();
            email.Setup(m => m.SendEmailAsync(student.Email, It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);
            var notifications = new Mock<INotificationService>();
            var admin = CreateAdminForRefund(context, student, email, notifications);

            await admin.RefundPayment(payment.Id);

            Assert.Equal(PaymentStatus.Refunded, context.Payments.Single(p => p.Id == payment.Id).Status);
            Assert.Equal(EnrollmentStatus.Pending, context.Enrollments.Single(e => e.Id == enrollment.Id).Status);

            notifications.Verify(m => m.NotifyAsync(student.Id, It.Is<string>(msg => msg.Contains(course.Title) && msg.Contains("refunded")), $"/Course/Details/{course.Id}"), Times.Once);

            // The student is emailed too: the subject names the course, the body carries the
            // amount and transaction reference, and the student's name is HTML-encoded.
            email.Verify(m => m.SendEmailAsync(
                "refund.student@example.com",
                It.Is<string>(subject => subject.Contains(course.Title)),
                It.Is<string>(body => body.Contains("500.00") && body.Contains("TRX-REFUND-1") && body.Contains(course.Title)
                                      && body.Contains("Refund &lt;Student&gt;") && !body.Contains("<Student>"))), Times.Once);
        }

        [Fact]
        public async Task RefundPayment_WhenTheEmailCannotBeSent_StillRefundsAndSaysSo()
        {
            var (context, student, _, enrollment, payment) = SeedRefundScenario(PaymentStatus.Success);
            using var _ = context;

            var email = new Mock<IEmailService>();
            email.Setup(m => m.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(false);
            var notifications = new Mock<INotificationService>();
            var admin = CreateAdminForRefund(context, student, email, notifications);

            await admin.RefundPayment(payment.Id);

            // An undeliverable email must never undo or block the refund itself.
            Assert.Equal(PaymentStatus.Refunded, context.Payments.Single(p => p.Id == payment.Id).Status);
            Assert.Equal(EnrollmentStatus.Pending, context.Enrollments.Single(e => e.Id == enrollment.Id).Status);
            notifications.Verify(m => m.NotifyAsync(student.Id, It.IsAny<string>(), It.IsAny<string>()), Times.Once);

            var message = (string)admin.TempData["EmailResult"]!;
            Assert.Contains("refunded", message);
            Assert.Contains("failed to send", message);
        }

        [Fact]
        public async Task RefundPayment_OnAnAlreadyRefundedPayment_DoesNothingAndSendsNoSecondEmail()
        {
            var (context, student, _, enrollment, payment) = SeedRefundScenario(PaymentStatus.Refunded);
            using var _ = context;
            // The student has since re-enrolled and paid nothing new; the enrollment is Active again.
            enrollment.Status = EnrollmentStatus.Active;
            context.SaveChanges();

            var email = new Mock<IEmailService>();
            var notifications = new Mock<INotificationService>();
            var admin = CreateAdminForRefund(context, student, email, notifications);

            await admin.RefundPayment(payment.Id);

            email.Verify(m => m.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            notifications.Verify(m => m.NotifyAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            Assert.Equal(EnrollmentStatus.Active, context.Enrollments.Single(e => e.Id == enrollment.Id).Status);
        }
    }
}
