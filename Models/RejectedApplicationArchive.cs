using System;

namespace EduLearn.Models
{
    // A permanent historical record of a rejected instructor application, written just
    // before the live ApplicationUser account is deleted. This table is intentionally
    // standalone — no foreign keys back to AspNetUsers (the row it's copied from no longer
    // exists) and nothing in the live app ever queries it. It exists purely so admins keep
    // a record of who applied and was turned down, without that account blocking the same
    // email from applying again.
    public class RejectedApplicationArchive
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Qualification { get; set; }
        public string? Institution { get; set; }
        public string? Skills { get; set; }
        public int? YearsOfExperience { get; set; }
        public string? Bio { get; set; }
        public string? ResumePath { get; set; }
        public DateTime AppliedAt { get; set; }
        public DateTime RejectedAt { get; set; }
    }
}
