using System;

namespace EduLearn.Models.ViewModels
{
    public class CourseActivityEventViewModel
    {
        public string StudentName { get; set; }
        public CourseActivityEventType EventType { get; set; }
        public string Description { get; set; }
        public DateTime OccurredAt { get; set; }
    }
}
