using System;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace EduLearn.Models
{
    // One row per student per coupon — the unique index on (CouponId, StudentId) in
    // ApplicationDbContext is what actually stops a student reusing the same code twice;
    // this table is also what TimesRedeemed on Coupon is derived from.
    public class CouponRedemption
    {
        public int Id { get; set; }

        public int CouponId { get; set; }

        [ValidateNever]
        public Coupon Coupon { get; set; }

        public string StudentId { get; set; }

        // Ties the redemption back to the group of Payment rows it discounted.
        public string OrderReference { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; }

        public DateTime RedeemedAt { get; set; } = DateTime.Now;
    }
}
