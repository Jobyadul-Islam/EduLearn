using EduLearn.Services;
using Xunit;

namespace EduLearn.Tests.Services
{
    public class CourseProgressCalculatorTests
    {
        [Theory]
        [InlineData(5, 5, true)]
        [InlineData(5, 3, false)]
        [InlineData(5, 0, false)]
        [InlineData(0, 0, false)] // a course with zero lessons can never be "complete"
        public void IsComplete_ReturnsExpectedResult(int totalLessons, int completedLessons, bool expected)
        {
            var result = CourseProgressCalculator.IsComplete(totalLessons, completedLessons);

            Assert.Equal(expected, result);
        }
    }
}
