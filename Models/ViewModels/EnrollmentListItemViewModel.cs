using System;

namespace EduLearn.Models.ViewModels
{
    public class EnrollmentListItemViewModel
    {
        public int Id { get; set; }
        public string StudentName { get; set; }
        public string StudentEmail { get; set; }
        public int CourseId { get; set; }
        public string CourseTitle { get; set; }
        public EnrollmentStatus Status { get; set; }
        public DateTime EnrollDate { get; set; }
        public DateTime? PaymentDate { get; set; }
    }
}
