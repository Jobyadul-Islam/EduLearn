using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EduLearn.Data;
using EduLearn.Models;
using EduLearn.Models.ViewModels;
using EduLearn.Services;

namespace EduLearn.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly INotificationService _notificationService;
        private readonly IFileUploadService _fileUploadService;

        public AdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IEmailService emailService, INotificationService notificationService, IFileUploadService fileUploadService)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
            _notificationService = notificationService;
            _fileUploadService = fileUploadService;
        }

        public async Task<IActionResult> Index()
        {
            // Rejected instructor applicants are excluded everywhere here, matching the Manage
            // Users list — a rejected account isn't really part of the platform's population.
            ViewBag.TotalUsers = _context.Users.Count(u => !u.IsRejected);
            ViewBag.TotalCourses = _context.Courses.Count();
            ViewBag.TotalEnrollments = _context.Enrollments.Count();
            ViewBag.PendingCoursesCount = _context.Courses.Count(c => c.Status == CourseStatus.Pending);

            ViewBag.TotalStudents = (await _userManager.GetUsersInRoleAsync("Student")).Count(u => !u.IsRejected);
            ViewBag.TotalInstructors = (await _userManager.GetUsersInRoleAsync("Instructor")).Count(u => !u.IsRejected);

            var sixMonthsAgo = new System.DateTime(System.DateTime.Now.Year, System.DateTime.Now.Month, 1).AddMonths(-5);
            var registrationsByMonth = _context.Users
                .Where(u => u.CreatedAt >= sixMonthsAgo)
                .AsEnumerable()
                .GroupBy(u => new System.DateTime(u.CreatedAt.Year, u.CreatedAt.Month, 1))
                .ToDictionary(g => g.Key, g => g.Count());

            // Always show all 6 months, even ones with zero registrations, so the chart has no gaps
            ViewBag.MonthlyRegistrations = Enumerable.Range(0, 6)
                .Select(i => sixMonthsAgo.AddMonths(i))
                .Select(month => (Month: month, Count: registrationsByMonth.GetValueOrDefault(month, 0)))
                .ToList();

            return View();
        }

        public IActionResult PendingCourses()
        {
            var courses = _context.Courses
                .Include(c => c.Category)
                .Include(c => c.Instructor)
                .Where(c => c.Status == CourseStatus.Pending)
                .OrderBy(c => c.Id)
                .ToList();

            return View(courses);
        }

        public IActionResult AllCourses()
        {
            var courses = _context.Courses
                .Include(c => c.Category)
                .Include(c => c.Instructor)
                .OrderByDescending(c => c.Id)
                .ToList();

            return View(courses);
        }

        public IActionResult CourseOverview(int id)
        {
            var course = _context.Courses
                .Include(c => c.Category)
                .Include(c => c.Instructor)
                .FirstOrDefault(c => c.Id == id);
            if (course == null) return NotFound();

            ViewBag.Roster = CourseRosterService.GetRoster(_context, id);
            ViewBag.Timeline = CourseRosterService.GetActivityTimeline(_context, id);
            return View(course);
        }

        public IActionResult CourseReport(int courseId, string period = "monthly")
        {
            var course = _context.Courses.Find(courseId);
            if (course == null) return NotFound();

            var normalizedPeriod = period == "weekly" ? "weekly" : "monthly";
            var data = normalizedPeriod == "weekly"
                ? CourseReportService.GetWeeklyReport(_context, courseId)
                : CourseReportService.GetMonthlyReport(_context, courseId);

            ViewBag.Course = course;
            ViewBag.Period = normalizedPeriod;
            return View(data);
        }

        public IActionResult ExportCourseReportPdf(int courseId, string period = "monthly")
        {
            var course = _context.Courses.Find(courseId);
            if (course == null) return NotFound();

            var normalizedPeriod = period == "weekly" ? "weekly" : "monthly";
            var data = normalizedPeriod == "weekly"
                ? CourseReportService.GetWeeklyReport(_context, courseId)
                : CourseReportService.GetMonthlyReport(_context, courseId);

            var pdf = ReportPdfService.GenerateCourseActivityReport(course.Title, normalizedPeriod, data);
            var fileName = $"{course.Title}-{normalizedPeriod}-Report-{System.DateTime.Now:yyyyMMdd}.pdf".Replace(" ", "-");
            return File(pdf, "application/pdf", fileName);
        }

        public IActionResult AllEnrollments()
        {
            var enrollments = (from e in _context.Enrollments
                                join u in _context.Users on e.StudentId equals u.Id
                                join c in _context.Courses on e.CourseId equals c.Id
                                orderby e.Id descending
                                select new EnrollmentListItemViewModel
                                {
                                    Id = e.Id,
                                    StudentName = u.FullName,
                                    StudentEmail = u.Email,
                                    CourseId = c.Id,
                                    CourseTitle = c.Title,
                                    Status = e.Status,
                                    EnrollDate = e.EnrollDate,
                                    PaymentDate = e.PaymentDate
                                }).ToList();

            return View(enrollments);
        }

        public IActionResult Revenue()
        {
            var (total, monthly) = GetRevenueData();
            ViewBag.TotalRevenue = total;
            ViewBag.MonthlyRevenue = monthly;
            return View();
        }

        public IActionResult ExportRevenuePdf()
        {
            var (total, monthly) = GetRevenueData();
            var pdf = ReportPdfService.GenerateRevenueReport(total, monthly);
            return File(pdf, "application/pdf", $"Revenue-Report-{System.DateTime.Now:yyyyMMdd}.pdf");
        }

        public IActionResult Analytics()
        {
            var (mostPopular, topRated) = GetAnalyticsData();
            ViewBag.MostPopular = mostPopular;
            ViewBag.TopRated = topRated;
            return View();
        }

        public IActionResult ExportAnalyticsPdf()
        {
            var (mostPopular, topRated) = GetAnalyticsData();
            var pdf = ReportPdfService.GenerateAnalyticsReport(mostPopular, topRated);
            return File(pdf, "application/pdf", $"Course-Analytics-{System.DateTime.Now:yyyyMMdd}.pdf");
        }

        private (decimal Total, List<(System.DateTime Month, decimal Revenue)> Monthly) GetRevenueData()
        {
            var successfulPayments = _context.Payments.Where(p => p.Status == PaymentStatus.Success);
            var total = successfulPayments.Sum(p => (decimal?)p.Amount) ?? 0;

            var sixMonthsAgo = new System.DateTime(System.DateTime.Now.Year, System.DateTime.Now.Month, 1).AddMonths(-5);
            var revenueByMonth = successfulPayments
                .Where(p => p.CreatedAt >= sixMonthsAgo)
                .AsEnumerable()
                .GroupBy(p => new System.DateTime(p.CreatedAt.Year, p.CreatedAt.Month, 1))
                .ToDictionary(g => g.Key, g => g.Sum(p => p.Amount));

            // Always show all 6 months, even ones with zero revenue, so the chart/report has no gaps
            var monthly = Enumerable.Range(0, 6)
                .Select(i => sixMonthsAgo.AddMonths(i))
                .Select(month => (Month: month, Revenue: revenueByMonth.GetValueOrDefault(month, 0)))
                .ToList();

            return (total, monthly);
        }

        private (List<(int Id, string Title, int EnrollmentCount)> MostPopular, List<(int Id, string Title, int ReviewCount, double? AverageRating)> TopRated) GetAnalyticsData()
        {
            // EF Core can't put a tuple literal inside a SQL expression tree, so the
            // ranking/limiting happens in SQL via an anonymous type, then the small
            // materialized result is converted to named tuples for the view/PDF.
            var mostPopular = _context.Courses
                .Select(c => new { c.Id, c.Title, EnrollmentCount = c.Enrollments.Count })
                .OrderByDescending(c => c.EnrollmentCount)
                .Take(10)
                .ToList()
                .Select(c => (c.Id, c.Title, c.EnrollmentCount))
                .ToList();

            var topRated = _context.Courses
                .Select(c => new { c.Id, c.Title, ReviewCount = c.Reviews.Count, AverageRating = c.Reviews.Average(r => (double?)r.Rating) })
                .Where(c => c.ReviewCount > 0)
                .OrderByDescending(c => c.AverageRating)
                .Take(10)
                .ToList()
                .Select(c => (c.Id, c.Title, c.ReviewCount, c.AverageRating))
                .ToList();

            return (mostPopular, topRated);
        }

        [HttpPost]
        public async Task<IActionResult> ApproveCourse(int id, bool isFree, decimal? price)
        {
            var course = _context.Courses.Find(id);
            if (course != null)
            {
                // Instructors never set a price themselves (see InstructorController) — the
                // Admin decides it here, at approval time, as either Free or a fixed amount.
                course.Price = isFree ? 0 : Math.Max(0, price ?? 0);
                course.Status = CourseStatus.Approved;
                course.RejectionReason = null;
                _context.SaveChanges();

                await _notificationService.NotifyAsync(
                    course.InstructorId,
                    $"Your course \"{course.Title}\" has been approved and is now live.",
                    $"/Instructor/CourseDetails/{course.Id}");
            }
            return RedirectToAction("PendingCourses");
        }

        // Admin can change a course's price at any time, not just at approval — the
        // instructor-facing Edit Course form never exposes Price at all.
        [HttpPost]
        public IActionResult EditCoursePrice(int id, decimal price)
        {
            var course = _context.Courses.Find(id);
            if (course != null)
            {
                course.Price = Math.Max(0, price);
                _context.SaveChanges();
            }
            return RedirectToAction("AllCourses");
        }

        [HttpPost]
        public IActionResult DeleteCourse(int id)
        {
            var course = _context.Courses.Find(id);
            if (course != null)
            {
                // Cascades to the course's modules/lessons/assignments/quizzes,
                // enrollments, payments, and reviews (see ApplicationDbContext) — this is
                // a full, permanent removal, not a soft delete.
                _context.Courses.Remove(course);
                _context.SaveChanges();
            }
            return RedirectToAction("AllCourses");
        }

        [HttpPost]
        public async Task<IActionResult> RejectCourse(int id, string? reason)
        {
            var course = _context.Courses.Find(id);
            if (course != null)
            {
                course.Status = CourseStatus.Rejected;
                course.RejectionReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
                _context.SaveChanges();

                var message = course.RejectionReason != null
                    ? $"Your course \"{course.Title}\" was rejected. Reason: {course.RejectionReason}"
                    : $"Your course \"{course.Title}\" was rejected.";

                await _notificationService.NotifyAsync(
                    course.InstructorId,
                    message,
                    $"/Instructor/EditCourse/{course.Id}");
            }
            return RedirectToAction("PendingCourses");
        }

        public async Task<IActionResult> Users(string? search, string? role, string? status)
        {
            // Rejected applications are done — once handled, they drop off the working list
            // instead of sitting alongside accounts that still need a decision.
            var users = _context.Users.Where(u => !u.IsRejected).OrderBy(u => u.FullName).ToList();
            var rows = new List<UserListItemViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                rows.Add(new UserListItemViewModel
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email,
                    Role = roles.FirstOrDefault() ?? "(none)",
                    IsApproved = user.IsApproved,
                    IsRejected = user.IsRejected,
                    IsActive = user.IsActive
                });
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                rows = rows
                    .Where(r => r.Email != null && r.Email.Contains(search, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(role) && role != "All")
            {
                rows = rows.Where(r => r.Role == role).ToList();
            }

            if (!string.IsNullOrWhiteSpace(status) && status != "All")
            {
                bool wantActive = status == "Active";
                rows = rows.Where(r => r.IsActive == wantActive).ToList();
            }

            ViewBag.SearchTerm = search;
            ViewBag.RoleFilter = role ?? "All";
            ViewBag.StatusFilter = status ?? "All";
            ViewBag.CurrentUserId = _userManager.GetUserId(User);

            return View(rows);
        }

        [HttpPost]
        public async Task<IActionResult> Approve(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["EmailResult"] = "User not found.";
                return RedirectToAction("Users");
            }

            user.IsApproved = true;
            user.IsRejected = false;
            await _userManager.UpdateAsync(user);

            // The account already has an unknown, randomly-generated password from when the
            // application was submitted (see ApplyController.GenerateRandomPassword) — nobody
            // has ever known it, including the applicant. Rather than the admin choosing a real
            // password and emailing it in plain text, we hand the instructor a one-time,
            // time-limited link to the same password-reset flow "Forgot your password?" uses, so
            // they set their own password themselves and it's never visible to anyone else.
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var setPasswordLink = Url.Page(
                "/Account/ResetPassword",
                pageHandler: null,
                values: new { area = "Identity", email = user.Email, code = token },
                protocol: Request.Scheme);

            var body = $@"
                <p>Hi {System.Net.WebUtility.HtmlEncode(user.FullName)},</p>
                <p>Congratulations — your EduLearn instructor application has been approved! We're excited to have you join our community of instructors.</p>
                <p><a href=""{setPasswordLink}"">Click here to set your password and log in</a></p>
                <p>Once you're in, you can start building your first course.</p>
                <p>This link is single-use and expires for your security. If it stops working, use ""Forgot your password?"" on the login page instead.</p>
                <p>Welcome aboard!<br>— EduLearn</p>";

            var sent = await _emailService.SendEmailAsync(user.Email, "Your EduLearn Instructor Account is Approved", body);

            await _notificationService.NotifyAsync(
                user.Id,
                "Your instructor application has been approved! Check your email to set your password.",
                "/Instructor");

            TempData["EmailResult"] = sent
                ? $"Instructor approved and a password-setup email sent to {user.Email}."
                : "Instructor approved, but the email failed to send. Check the email configuration.";

            return RedirectToAction("Users");
        }

        [HttpPost]
        public async Task<IActionResult> Reject(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["EmailResult"] = "User not found.";
                return RedirectToAction("Users");
            }

            // Flip these first as a safety net — if account deletion below fails for any
            // reason, the account still correctly disappears from the Manage Users list and
            // stays locked out, exactly like before this account-deletion behavior existed.
            user.IsApproved = false;
            user.IsRejected = true;
            user.IsActive = false;
            await _userManager.UpdateAsync(user);

            // Keep a permanent record of who applied and was turned down — this table has no
            // effect on anything live (nothing else in the app ever queries it) and isn't
            // tied back to the account by a foreign key, since that account is about to be
            // deleted entirely. The resume file itself is deliberately left on disk (not
            // deleted) so ResumePath here still points to something real if it's ever needed.
            _context.RejectedApplicationArchives.Add(new RejectedApplicationArchive
            {
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Qualification = user.Qualification,
                Institution = user.Institution,
                Skills = user.Skills,
                YearsOfExperience = user.YearsOfExperience,
                Bio = user.Bio,
                ResumePath = user.ResumePath,
                AppliedAt = user.CreatedAt,
                RejectedAt = System.DateTime.Now
            });
            await _context.SaveChangesAsync();

            // No in-app notification here on purpose — the account is about to be deleted (and
            // was already deactivated above), so the applicant could never log in to see one.
            // Email is the only channel that actually reaches them.
            var body = $@"
                <p>Hi {System.Net.WebUtility.HtmlEncode(user.FullName)},</p>
                <p>Thank you for your interest in teaching on EduLearn and for taking the time to apply.</p>
                <p>After reviewing your application, we won't be moving forward with it at this time.</p>
                <p>We appreciate your interest and wish you the best.</p>
                <p>— EduLearn</p>";

            var sent = await _emailService.SendEmailAsync(user.Email, "Update on Your EduLearn Instructor Application", body);
            var userEmail = user.Email;

            // Deleting the account (rather than just leaving it deactivated) frees the email
            // address up immediately, so the same person can apply again later if they choose.
            var deleteResult = await _userManager.DeleteAsync(user);

            TempData["EmailResult"] = deleteResult.Succeeded
                ? (sent
                    ? $"Application rejected, account removed, and an email sent to {userEmail}."
                    : $"Application rejected and the account removed, but the email to {userEmail} failed to send.")
                : "Application rejected, but the account couldn't be fully removed: " + string.Join(" ", deleteResult.Errors.Select(e => e.Description));

            return RedirectToAction("Users");
        }

        [HttpPost]
        public async Task<IActionResult> ToggleActive(string id)
        {
            var currentUserId = _userManager.GetUserId(User);
            if (id == currentUserId)
            {
                return RedirectToAction("Users");
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                user.IsActive = !user.IsActive;
                await _userManager.UpdateAsync(user);
            }
            return RedirectToAction("Users");
        }

        public IActionResult ViewApplication(string id)
        {
            var user = _context.Users.FirstOrDefault(u => u.Id == id);
            if (user == null) return NotFound();

            return View(user);
        }

        // Resumes live in private storage (see FileUploadService) — this is the only way to
        // reach one, and only an Admin (controller-level [Authorize(Roles = "Admin")]) can.
        public IActionResult DownloadResume(string id)
        {
            var user = _context.Users.FirstOrDefault(u => u.Id == id);
            if (user == null || string.IsNullOrEmpty(user.ResumePath)) return NotFound();

            var physicalPath = _fileUploadService.ResolvePrivateFilePath(user.ResumePath, "resumes");
            if (physicalPath == null) return NotFound();

            var provider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();
            var contentType = provider.TryGetContentType(physicalPath, out var ct) ? ct : "application/octet-stream";
            return PhysicalFile(physicalPath, contentType, System.IO.Path.GetFileName(physicalPath));
        }

        // Read-only record of turned-down instructor applications — kept for reference
        // (who applied, when, why) even though the account itself is deleted on rejection.
        public IActionResult RejectedApplications()
        {
            var archives = _context.RejectedApplicationArchives
                .OrderByDescending(a => a.RejectedAt)
                .ToList();

            return View(archives);
        }

        // Every payment ever recorded, most recent first — the working list for issuing refunds.
        public IActionResult Payments()
        {
            var payments = _context.Payments
                .Include(p => p.Course)
                .OrderByDescending(p => p.CreatedAt)
                .ToList();

            var studentIds = payments.Select(p => p.StudentId).Distinct().ToList();
            ViewBag.StudentNamesById = _context.Users
                .Where(u => studentIds.Contains(u.Id))
                .ToDictionary(u => u.Id, u => u.FullName);

            return View(payments);
        }

        // Marks a successful payment as refunded and revokes the access it paid for — the
        // actual money movement happens on bKash's side (or wherever the admin processed the
        // refund); this just keeps EduLearn's own records and access in sync with that.
        [HttpPost]
        public async Task<IActionResult> RefundPayment(int id)
        {
            var payment = _context.Payments.Include(p => p.Course).FirstOrDefault(p => p.Id == id);
            if (payment == null || payment.Status != PaymentStatus.Success)
            {
                TempData["EmailResult"] = "Payment not found or not eligible for a refund.";
                return RedirectToAction("Payments");
            }

            payment.Status = PaymentStatus.Refunded;

            var enrollment = _context.Enrollments
                .FirstOrDefault(e => e.CourseId == payment.CourseId && e.StudentId == payment.StudentId);
            if (enrollment != null)
            {
                enrollment.Status = EnrollmentStatus.Pending;
            }

            await _context.SaveChangesAsync();

            await _notificationService.NotifyAsync(
                payment.StudentId,
                $"Your payment for \"{payment.Course.Title}\" was refunded. Full access to the course has been revoked.",
                $"/Course/Details/{payment.CourseId}");

            TempData["EmailResult"] = $"Payment {payment.TransactionId} marked as refunded and course access revoked.";
            return RedirectToAction("Payments");
        }

        // ---------------- Coupons ----------------

        public IActionResult Coupons()
        {
            var coupons = _context.Coupons
                .OrderByDescending(c => c.CreatedAt)
                .ToList();

            return View(coupons);
        }

        public IActionResult CreateCoupon()
        {
            return View();
        }

        [HttpPost]
        public IActionResult CreateCoupon(string code, int discountPercent, DateTime? expiresAt, int? maxRedemptions)
        {
            code = (code ?? "").Trim().ToUpperInvariant();

            if (string.IsNullOrWhiteSpace(code))
            {
                ModelState.AddModelError("", "Enter a coupon code.");
            }
            else if (_context.Coupons.Any(c => c.Code == code))
            {
                ModelState.AddModelError("", "A coupon with this code already exists.");
            }

            if (discountPercent < 1 || discountPercent > 100)
            {
                ModelState.AddModelError("", "Discount must be between 1 and 100 percent.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Code = code;
                ViewBag.DiscountPercent = discountPercent;
                return View();
            }

            _context.Coupons.Add(new Coupon
            {
                Code = code,
                DiscountPercent = discountPercent,
                ExpiresAt = expiresAt,
                MaxRedemptions = maxRedemptions
            });
            _context.SaveChanges();

            return RedirectToAction("Coupons");
        }

        [HttpPost]
        public IActionResult ToggleCouponActive(int id)
        {
            var coupon = _context.Coupons.Find(id);
            if (coupon != null)
            {
                coupon.IsActive = !coupon.IsActive;
                _context.SaveChanges();
            }
            return RedirectToAction("Coupons");
        }

        [HttpPost]
        public IActionResult DeleteCoupon(int id)
        {
            var coupon = _context.Coupons.Find(id);
            if (coupon != null)
            {
                _context.Coupons.Remove(coupon);
                _context.SaveChanges();
            }
            return RedirectToAction("Coupons");
        }

        public IActionResult AccessRequests()
        {
            var requests = _context.InstructorAccessRequests
                .OrderBy(r => r.Status == AccessRequestStatus.Pending ? 0 : 1)
                .ThenByDescending(r => r.CreatedAt)
                .Take(50)
                .ToList();

            return View(requests);
        }

        [HttpPost]
        public async Task<IActionResult> ApproveAccessRequest(int id)
        {
            var request = await _context.InstructorAccessRequests.FindAsync(id);
            if (request == null || request.Status != AccessRequestStatus.Pending)
            {
                TempData["EmailResult"] = "Request not found or already decided.";
                return RedirectToAction("AccessRequests");
            }

            request.Status = AccessRequestStatus.Approved;
            request.DecidedAt = System.DateTime.Now;
            request.DecidedByAdminId = _userManager.GetUserId(User);
            request.OtpCode = System.Security.Cryptography.RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
            request.OtpExpiresAt = System.DateTime.Now.AddHours(24);
            request.AccessToken = System.Security.Cryptography.RandomNumberGenerator.GetHexString(32);
            await _context.SaveChangesAsync();

            var applyLink = Url.Action("VerifyAccessCode", "Apply", new { area = "", token = request.AccessToken }, Request.Scheme);
            var body = $@"
                <p>Hi,</p>
                <p>Your request to apply as an instructor on EduLearn has been approved.</p>
                <p><a href=""{applyLink}"">Click here to continue your application</a></p>
                <p>You'll be asked for this verification code:</p>
                <p style=""font-size:28px; font-weight:bold; letter-spacing:4px;"">{request.OtpCode}</p>
                <p>This link and code expire in 24 hours.</p>
                <p>— EduLearn</p>";

            var sent = await _emailService.SendEmailAsync(request.Email, "You're invited to apply as an EduLearn instructor", body);

            TempData["EmailResult"] = sent
                ? $"Request approved and an invite emailed to {request.Email}."
                : "Request approved, but the email failed to send. Check the email configuration.";

            return RedirectToAction("AccessRequests");
        }

        [HttpPost]
        public async Task<IActionResult> DenyAccessRequest(int id)
        {
            var request = await _context.InstructorAccessRequests.FindAsync(id);
            if (request == null || request.Status != AccessRequestStatus.Pending)
            {
                TempData["EmailResult"] = "Request not found or already decided.";
                return RedirectToAction("AccessRequests");
            }

            request.Status = AccessRequestStatus.Denied;
            request.DecidedAt = System.DateTime.Now;
            request.DecidedByAdminId = _userManager.GetUserId(User);
            await _context.SaveChangesAsync();

            var body = $@"
                <p>Hi,</p>
                <p>Thanks for your interest in applying as an instructor on EduLearn.</p>
                <p>We ran into a technical issue processing your request and weren't able to move it forward this time. This isn't a reflection on you — please feel free to try again in a little while.</p>
                <p>We're sorry for the inconvenience and appreciate your patience.</p>
                <p>— EduLearn</p>";

            var sent = await _emailService.SendEmailAsync(request.Email, "Update on Your EduLearn Instructor Access Request", body);

            TempData["EmailResult"] = sent
                ? $"Request from {request.Email} denied and an email sent."
                : $"Request from {request.Email} denied, but the email failed to send. Check the email configuration.";

            return RedirectToAction("AccessRequests");
        }
    }
}
