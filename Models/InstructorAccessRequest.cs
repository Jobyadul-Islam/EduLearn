using System;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace EduLearn.Models
{
    public enum AccessRequestMethod { Google, Email }
    public enum AccessRequestStatus { Pending, Approved, Denied }

    public class InstructorAccessRequest
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public AccessRequestMethod Method { get; set; }
        public AccessRequestStatus Status { get; set; } = AccessRequestStatus.Pending;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public string? DecidedByAdminId { get; set; }
        public DateTime? DecidedAt { get; set; }

        [ValidateNever]
        [ForeignKey("DecidedByAdminId")]
        public ApplicationUser? DecidedByAdmin { get; set; }

        // Only populated once an Admin approves the request.
        public string? OtpCode { get; set; }
        public DateTime? OtpExpiresAt { get; set; }
        public string? AccessToken { get; set; }

        // Counts wrong-code attempts against OtpCode. Capped at MaxOtpAttempts so the 6-digit
        // code can't be brute-forced over the 24-hour validity window even by someone who has
        // obtained the (otherwise unguessable) AccessToken link.
        public int OtpAttempts { get; set; }
        public const int MaxOtpAttempts = 5;

        // Mirrors the old InstructorApplicationPin.IsUsed — flips true only once the
        // applicant successfully submits the full Form, not merely on OTP verification,
        // so an abandoned-then-resumed session still works.
        public bool IsConsumed { get; set; }
        public DateTime? ConsumedAt { get; set; }
    }
}
