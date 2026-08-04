using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace EduLearn.Models
{
    public class Course
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        public int CategoryId { get; set; }
        public Category Category { get; set; }
        public ICollection<Module> Modules { get; set; }
        public ICollection<Enrollment> Enrollments { get; set; }
    }
}