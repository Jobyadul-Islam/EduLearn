namespace EduLearn.Services
{
    public static class CourseProgressCalculator
    {
        public static bool IsComplete(int totalLessons, int completedLessons)
        {
            return totalLessons > 0 && completedLessons >= totalLessons;
        }
    }
}
