using System;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace EduLearn.Models
{
    public class InstructorApplicationPin
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public bool IsUsed { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UsedAt { get; set; }

        public string GeneratedByAdminId { get; set; }

        [ValidateNever]
        [ForeignKey("GeneratedByAdminId")]
        public ApplicationUser GeneratedByAdmin { get; set; }
    }
}
