using System;
using System.Linq;
using EduLearn.Data;
using EduLearn.Models;

namespace EduLearn.Services
{
    // Shared by CartController (applying a code) and BkashController (re-validating it at
    // checkout time, never trusting whatever was true when the code was first applied) so
    // both places agree on what makes a coupon usable.
    public static class CouponService
    {
        public static (Coupon? Coupon, string? Error) Validate(ApplicationDbContext context, string? code, string studentId)
        {
            if (string.IsNullOrWhiteSpace(code)) return (null, null);

            var normalized = code.Trim().ToUpperInvariant();
            var coupon = context.Coupons.FirstOrDefault(c => c.Code == normalized);

            if (coupon == null) return (null, "That coupon code doesn't exist.");
            if (!coupon.IsActive) return (null, "That coupon is no longer active.");
            if (coupon.ExpiresAt.HasValue && coupon.ExpiresAt.Value < DateTime.Now) return (null, "That coupon has expired.");
            if (coupon.MaxRedemptions.HasValue && coupon.TimesRedeemed >= coupon.MaxRedemptions.Value) return (null, "That coupon has reached its redemption limit.");
            if (context.CouponRedemptions.Any(r => r.CouponId == coupon.Id && r.StudentId == studentId)) return (null, "You've already used this coupon.");

            return (coupon, null);
        }
    }
}
