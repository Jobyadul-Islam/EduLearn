using System;
using System.Collections.Generic;
using System.Linq;
using EduLearn.Data;
using EduLearn.Models;
using EduLearn.Models.ViewModels;

namespace EduLearn.Services
{
    // Shared by InstructorController (scoped to their own courses) and AdminController
    // (any course) — access control is entirely the caller's responsibility.
    public static class CourseRosterService
    {
        public static List<StudentRosterRowViewModel> GetRoster(ApplicationDbContext context, int courseId)
        {
            var enrollments = context.Enrollments
                .Where(e => e.CourseId == courseId)
                .ToList();

            var studentIds = enrollments.Select(e => e.StudentId).Distinct().ToList();

            var students = context.Users
                .Where(u => studentIds.Contains(u.Id))
                .ToDictionary(u => u.Id, u => u);

            var totalLessons = context.Lessons.Count(l => l.Module.CourseId == courseId);

            var completedRaw = context.LessonProgresses
                .Where(p => p.IsCompleted && p.Lesson.Module.CourseId == courseId && studentIds.Contains(p.StudentId))
                .Select(p => new { p.StudentId, p.CompletedAt })
                .ToList();
            var completedCountByStudent = completedRaw.GroupBy(x => x.StudentId).ToDictionary(g => g.Key, g => g.Count());
            var lastLessonActivity = completedRaw
                .Where(x => x.CompletedAt.HasValue)
                .GroupBy(x => x.StudentId)
                .ToDictionary(g => g.Key, g => (DateTime?)g.Max(x => x.CompletedAt!.Value));

            var quizRaw = context.QuizResults
                .Where(r => r.Quiz.Lesson.Module.CourseId == courseId && studentIds.Contains(r.StudentId))
                .Select(r => new { r.StudentId, r.AttemptDate })
                .ToList();
            var lastQuizActivity = quizRaw.GroupBy(x => x.StudentId).ToDictionary(g => g.Key, g => (DateTime?)g.Max(x => x.AttemptDate));

            var assignmentRaw = context.AssignmentSubmissions
                .Where(s => s.Assignment.Lesson.Module.CourseId == courseId && studentIds.Contains(s.StudentId))
                .Select(s => new { s.StudentId, s.SubmittedDate })
                .ToList();
            var lastAssignmentActivity = assignmentRaw.GroupBy(x => x.StudentId).ToDictionary(g => g.Key, g => (DateTime?)g.Max(x => x.SubmittedDate));

            var rows = new List<StudentRosterRowViewModel>();
            foreach (var enrollment in enrollments)
            {
                if (!students.TryGetValue(enrollment.StudentId, out var student)) continue;

                var completedCount = completedCountByStudent.GetValueOrDefault(enrollment.StudentId, 0);
                var progress = totalLessons == 0 ? 0 : (int)Math.Round(completedCount * 100.0 / totalLessons);

                DateTime? lastActivityAt = enrollment.EnrollDate;
                string? lastActivityType = "Enrolled";

                void ConsiderLatest(DateTime? candidate, string type)
                {
                    if (candidate.HasValue && (!lastActivityAt.HasValue || candidate.Value > lastActivityAt.Value))
                    {
                        lastActivityAt = candidate.Value;
                        lastActivityType = type;
                    }
                }

                ConsiderLatest(lastLessonActivity.GetValueOrDefault(enrollment.StudentId), "Completed Lesson");
                ConsiderLatest(lastQuizActivity.GetValueOrDefault(enrollment.StudentId), "Quiz Attempt");
                ConsiderLatest(lastAssignmentActivity.GetValueOrDefault(enrollment.StudentId), "Assignment Submitted");

                rows.Add(new StudentRosterRowViewModel
                {
                    StudentId = enrollment.StudentId,
                    StudentName = student.FullName,
                    StudentEmail = student.Email,
                    EnrollDate = enrollment.EnrollDate,
                    Status = enrollment.Status,
                    ProgressPercentage = progress,
                    LastActivityAt = lastActivityAt,
                    LastActivityType = lastActivityType
                });
            }

            return rows.OrderByDescending(r => r.EnrollDate).ToList();
        }

        public static List<CourseActivityEventViewModel> GetActivityTimeline(ApplicationDbContext context, int courseId, int maxEvents = 100)
        {
            var enrollmentEvents = context.Enrollments
                .Where(e => e.CourseId == courseId)
                .Join(context.Users, e => e.StudentId, u => u.Id, (e, u) => new { u.FullName, e.EnrollDate })
                .OrderByDescending(x => x.EnrollDate)
                .Take(maxEvents)
                .ToList()
                .Select(x => new CourseActivityEventViewModel
                {
                    StudentName = x.FullName,
                    EventType = CourseActivityEventType.Enrolled,
                    Description = "Enrolled in the course",
                    OccurredAt = x.EnrollDate
                });

            var lessonEvents = context.LessonProgresses
                .Where(p => p.IsCompleted && p.CompletedAt != null && p.Lesson.Module.CourseId == courseId)
                .Join(context.Users, p => p.StudentId, u => u.Id, (p, u) => new { u.FullName, p.CompletedAt, LessonTitle = p.Lesson.Title })
                .OrderByDescending(x => x.CompletedAt)
                .Take(maxEvents)
                .ToList()
                .Select(x => new CourseActivityEventViewModel
                {
                    StudentName = x.FullName,
                    EventType = CourseActivityEventType.LessonCompleted,
                    Description = $"Completed lesson \"{x.LessonTitle}\"",
                    OccurredAt = x.CompletedAt!.Value
                });

            var quizEvents = context.QuizResults
                .Where(r => r.Quiz.Lesson.Module.CourseId == courseId)
                .Join(context.Users, r => r.StudentId, u => u.Id, (r, u) => new { u.FullName, r.AttemptDate, r.Score, r.TotalQuestions, r.Passed, QuizTitle = r.Quiz.Title })
                .OrderByDescending(x => x.AttemptDate)
                .Take(maxEvents)
                .ToList()
                .Select(x => new CourseActivityEventViewModel
                {
                    StudentName = x.FullName,
                    EventType = CourseActivityEventType.QuizAttempted,
                    Description = $"Attempted quiz \"{x.QuizTitle}\" — {(x.Passed ? "Passed" : "Failed")} ({x.Score}/{x.TotalQuestions})",
                    OccurredAt = x.AttemptDate
                });

            var assignmentEvents = context.AssignmentSubmissions
                .Where(s => s.Assignment.Lesson.Module.CourseId == courseId)
                .Join(context.Users, s => s.StudentId, u => u.Id, (s, u) => new { u.FullName, s.SubmittedDate, AssignmentTitle = s.Assignment.Title })
                .OrderByDescending(x => x.SubmittedDate)
                .Take(maxEvents)
                .ToList()
                .Select(x => new CourseActivityEventViewModel
                {
                    StudentName = x.FullName,
                    EventType = CourseActivityEventType.AssignmentSubmitted,
                    Description = $"Submitted assignment \"{x.AssignmentTitle}\"",
                    OccurredAt = x.SubmittedDate
                });

            return enrollmentEvents
                .Concat(lessonEvents)
                .Concat(quizEvents)
                .Concat(assignmentEvents)
                .OrderByDescending(e => e.OccurredAt)
                .Take(maxEvents)
                .ToList();
        }
    }
}
