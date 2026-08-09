using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace EduLearn.Models
{
    public class Module
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public int CourseId { get; set; }

        [ValidateNever]
        public Course Course { get; set; }

        [ValidateNever]
        public ICollection<Lesson> Lessons { get; set; }
    }
}