using System;
using System.Collections.Generic;
using System.Linq;
using EduLearn.Data;
using EduLearn.Models;

namespace EduLearn.Services
{
    // Shared by InstructorController (scoped to their own courses) and AdminController
    // (any course) — access control is entirely the caller's responsibility.
    public static class CourseReportService
    {
        public static List<(DateTime PeriodStart, int NewEnrollments, int LessonsCompleted, int QuizAttempts, double? AverageQuizScorePercent, int AssignmentsSubmitted)>
            GetWeeklyReport(ApplicationDbContext context, int courseId)
        {
            var firstBucketStart = GetWeekStart(DateTime.Now).AddDays(-7 * 5);
            return BuildReport(context, courseId, firstBucketStart, GetWeekStart, i => firstBucketStart.AddDays(7 * i));
        }

        public static List<(DateTime PeriodStart, int NewEnrollments, int LessonsCompleted, int QuizAttempts, double? AverageQuizScorePercent, int AssignmentsSubmitted)>
            GetMonthlyReport(ApplicationDbContext context, int courseId)
        {
            var firstBucketStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(-5);
            return BuildReport(context, courseId, firstBucketStart, GetMonthStart, i => firstBucketStart.AddMonths(i));
        }

        private static DateTime GetMonthStart(DateTime date) => new DateTime(date.Year, date.Month, 1);

        // Monday-start ISO week. DayOfWeek: Sunday=0 .. Saturday=6.
        private static DateTime GetWeekStart(DateTime date)
        {
            var offsetFromMonday = ((int)date.DayOfWeek + 6) % 7;
            return date.Date.AddDays(-offsetFromMonday);
        }

        private static List<(DateTime PeriodStart, int NewEnrollments, int LessonsCompleted, int QuizAttempts, double? AverageQuizScorePercent, int AssignmentsSubmitted)>
            BuildReport(ApplicationDbContext context, int courseId, DateTime rangeStart, Func<DateTime, DateTime> getBucketStart, Func<int, DateTime> bucketAt)
        {
            // Only Active enrollments count as "new students" — a Pending (unpaid) row
            // isn't really a student yet, mirroring the Revenue report's success-only filter.
            var enrollmentsRaw = context.Enrollments
                .Where(e => e.CourseId == courseId && e.Status == EnrollmentStatus.Active && e.EnrollDate >= rangeStart)
                .Select(e => e.EnrollDate)
                .ToList();
            var enrollmentsByBucket = enrollmentsRaw.GroupBy(getBucketStart).ToDictionary(g => g.Key, g => g.Count());

            var lessonsRaw = context.LessonProgresses
                .Where(p => p.IsCompleted && p.CompletedAt != null && p.CompletedAt >= rangeStart && p.Lesson.Module.CourseId == courseId)
                .Select(p => p.CompletedAt!.Value)
                .ToList();
            var lessonsByBucket = lessonsRaw.GroupBy(getBucketStart).ToDictionary(g => g.Key, g => g.Count());

            var quizzesRaw = context.QuizResults
                .Where(r => r.AttemptDate >= rangeStart && r.Quiz.Lesson.Module.CourseId == courseId)
                .Select(r => new { r.AttemptDate, r.Score, r.TotalQuestions })
                .ToList();
            var quizzesByBucket = quizzesRaw.GroupBy(x => getBucketStart(x.AttemptDate)).ToDictionary(g => g.Key, g => g.Count());
            var quizScoresByBucket = quizzesRaw
                .GroupBy(x => getBucketStart(x.AttemptDate))
                .ToDictionary(g => g.Key, g => g.Where(x => x.TotalQuestions > 0).Select(x => x.Score * 100.0 / x.TotalQuestions).ToList());

            var assignmentsRaw = context.AssignmentSubmissions
                .Where(s => s.SubmittedDate >= rangeStart && s.Assignment.Lesson.Module.CourseId == courseId)
                .Select(s => s.SubmittedDate)
                .ToList();
            var assignmentsByBucket = assignmentsRaw.GroupBy(getBucketStart).ToDictionary(g => g.Key, g => g.Count());

            return Enumerable.Range(0, 6)
                .Select(bucketAt)
                .Select(bucket =>
                {
                    var scores = quizScoresByBucket.GetValueOrDefault(bucket);
                    return (
                        PeriodStart: bucket,
                        NewEnrollments: enrollmentsByBucket.GetValueOrDefault(bucket, 0),
                        LessonsCompleted: lessonsByBucket.GetValueOrDefault(bucket, 0),
                        QuizAttempts: quizzesByBucket.GetValueOrDefault(bucket, 0),
                        AverageQuizScorePercent: scores != null && scores.Count > 0 ? scores.Average() : (double?)null,
                        AssignmentsSubmitted: assignmentsByBucket.GetValueOrDefault(bucket, 0)
                    );
                })
                .ToList();
        }
    }
}
