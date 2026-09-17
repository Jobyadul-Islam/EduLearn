namespace EduLearn.Services
{
    // Per-context allowlists/size caps for the file uploads that must NOT be served as
    // public static files (lesson content, resumes, assignment submissions). Centralized so
    // every save path enforces the same rule instead of each controller inventing its own.
    public static class UploadPolicy
    {
        public static readonly string[] LessonExtensions = { ".mp4", ".webm", ".mov", ".pdf", ".pptx", ".docx" };
        public const long LessonMaxSizeBytes = 300L * 1024 * 1024; // 300MB — lecture video/slides

        public static readonly string[] ResumeExtensions = { ".pdf", ".doc", ".docx" };
        public const long ResumeMaxSizeBytes = 5L * 1024 * 1024; // 5MB

        public static readonly string[] SubmissionExtensions = { ".pdf", ".doc", ".docx", ".ppt", ".pptx", ".xls", ".xlsx", ".zip", ".txt", ".jpg", ".jpeg", ".png" };
        public const long SubmissionMaxSizeBytes = 25L * 1024 * 1024; // 25MB
    }
}
