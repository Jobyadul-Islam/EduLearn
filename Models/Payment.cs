using System;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace EduLearn.Models
{
    public enum PaymentStatus
    {
        Success,
        Failed,
        Refunded
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

        // Shared by every Payment row created from the same checkout (cart purchases create
        // one row per course but one shared bKash charge) — lets Order History group them
        // back into a single order instead of showing unrelated-looking line items.
        public string? OrderReference { get; set; }
    }
}
