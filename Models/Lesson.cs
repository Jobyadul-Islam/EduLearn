using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace EduLearn.Models
{
    public class Lesson
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string? VideoUrl { get; set; }      // now nullable — not every lesson needs a video
        public string? FilePath { get; set; }        // NEW — stores uploaded PDF/video file path

        public int ModuleId { get; set; }

        [ValidateNever]
        public Module Module { get; set; }
    }
}