using System;

namespace EduLearn.Models
{
    public class Enrollment
    {
        public int Id { get; set; }
        public string StudentId { get; set; }
        public int CourseId { get; set; }
        public Course Course { get; set; }
        public DateTime EnrollDate { get; set; }
    }
}