using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace EduLearn.Models
{
    public class Lesson
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string? VideoUrl { get; set; }
        public string? FilePath { get; set; }

        public int ModuleId { get; set; }

        [ValidateNever]
        public Module Module { get; set; }

        [ValidateNever]
        public ICollection<Assignment> Assignments { get; set; }
    }
}