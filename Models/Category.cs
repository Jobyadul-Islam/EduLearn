using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

using System.Collections.Generic;

namespace EduLearn.Models
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        [ValidateNever]
        public ICollection<Course> Courses { get; set; }
    }
}