using System;
using System.Linq;
using System.Threading.Tasks;
using EduLearn.Data;
using EduLearn.Models;
using Microsoft.EntityFrameworkCore;

namespace EduLearn.Services
{
    public class DeadlineReminderService : IDeadlineReminderService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;

        public DeadlineReminderService(ApplicationDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        public async Task<int> SendDueRemindersAsync()
        {
            var now = DateTime.Now;
            var window = now.AddHours(24);

            var dueSoonAssignments = _context.Assignments
                .Include(a => a.Lesson).ThenInclude(l => l.Module).ThenInclude(m => m.Course)
                .Where(a => a.DueDate >= now && a.DueDate <= window)
                .ToList();

            var sentCount = 0;

            foreach (var assignment in dueSoonAssignments)
            {
                var courseId = assignment.Lesson.Module.CourseId;

                var alreadySubmittedStudentIds = _context.AssignmentSubmissions
                    .Where(s => s.AssignmentId == assignment.Id)
                    .Select(s => s.StudentId)
                    .ToList();

                var alreadyRemindedStudentIds = _context.AssignmentReminders
                    .Where(r => r.AssignmentId == assignment.Id)
                    .Select(r => r.StudentId)
                    .ToList();

                var students = _context.Enrollments
                    .Where(e => e.CourseId == courseId && e.Status == EnrollmentStatus.Active)
                    .Where(e => !alreadySubmittedStudentIds.Contains(e.StudentId) && !alreadyRemindedStudentIds.Contains(e.StudentId))
                    .Select(e => e.StudentId)
                    .ToList();

                foreach (var studentId in students)
                {
                    var student = _context.Users.Find(studentId);
                    if (student == null) continue;

                    var body = $@"
                        <p>Hi {System.Net.WebUtility.HtmlEncode(student.FullName)},</p>
                        <p>Reminder: <strong>{System.Net.WebUtility.HtmlEncode(assignment.Title)}</strong> in
                        <strong>{System.Net.WebUtility.HtmlEncode(assignment.Lesson.Module.Course.Title)}</strong>
                        is due on {assignment.DueDate:MMMM d, yyyy 'at' h:mm tt}.</p>
                        <p>Log in to EduLearn to submit before the deadline.</p>
                        <p>— EduLearn</p>";

                    await _emailService.SendEmailAsync(student.Email, $"Reminder: {assignment.Title} is due soon", body);

                    _context.AssignmentReminders.Add(new AssignmentReminder
                    {
                        AssignmentId = assignment.Id,
                        StudentId = studentId,
                        SentAt = now
                    });
                    sentCount++;
                }
            }

            if (sentCount > 0)
            {
                await _context.SaveChangesAsync();
            }

            return sentCount;
        }
    }
}
