using System;

namespace EduLearn.Models
{
    // Marks that a deadline-reminder email has already gone out for a given
    // (assignment, student) pair, so the periodic background check never re-sends one.
    public class AssignmentReminder
    {
        public int Id { get; set; }
        public int AssignmentId { get; set; }
        public string StudentId { get; set; }
        public DateTime SentAt { get; set; }
    }
}
