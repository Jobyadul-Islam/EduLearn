using System;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace EduLearn.Models
{
    public enum EnrollmentStatus
    {
        Pending,
        Active
    }

    public class Enrollment
    {
        public int Id { get; set; }
        public string StudentId { get; set; }
        public int CourseId { get; set; }

        [ValidateNever]
        public Course Course { get; set; }

        public DateTime EnrollDate { get; set; }

        public EnrollmentStatus Status { get; set; }
        public DateTime? PaymentDate { get; set; }
    }
}
