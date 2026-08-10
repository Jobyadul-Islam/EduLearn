using System;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace EduLearn.Models
{
    public class AssignmentSubmission
    {
        public int Id { get; set; }
        public int AssignmentId { get; set; }

        [ValidateNever]
        public Assignment Assignment { get; set; }

        public string StudentId { get; set; }
        public string FilePath { get; set; }
        public DateTime SubmittedDate { get; set; }
    }
}