using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace EduLearn.Models
{
    public class Coupon
    {
        public int Id { get; set; }

        // Always stored/compared uppercase so "SAVE10" and "save10" are the same code.
        public string Code { get; set; }

        public int DiscountPercent { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime? ExpiresAt { get; set; }

        // Null means unlimited.
        public int? MaxRedemptions { get; set; }

        public int TimesRedeemed { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ValidateNever]
        public ICollection<CouponRedemption> Redemptions { get; set; }
    }
}
