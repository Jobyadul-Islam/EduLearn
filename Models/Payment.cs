using System;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace EduLearn.Models
{
    public enum PaymentStatus
    {
        Success,
        Failed
    }

    public class Payment
    {
        public int Id { get; set; }

        public string StudentId { get; set; }

        public int CourseId { get; set; }

        [ValidateNever]
        public Course Course { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public string TransactionId { get; set; }

        public PaymentStatus Status { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
