using System;

namespace EduLearn.Models.ViewModels
{
    public class StudentRosterRowViewModel
    {
        public string StudentId { get; set; }
        public string StudentName { get; set; }
        public string StudentEmail { get; set; }
        public DateTime EnrollDate { get; set; }
        public EnrollmentStatus Status { get; set; }
        public int ProgressPercentage { get; set; }
        public DateTime? LastActivityAt { get; set; }
        public string? LastActivityType { get; set; }
    }
}
