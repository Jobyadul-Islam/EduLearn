using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EduLearn.Controllers;
using EduLearn.Data;
using EduLearn.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace EduLearn.Tests.Integration
{
    // The homepage's featured-courses section used to just be "newest 6 approved courses,"
    // with rating shown but not used for ordering at all. It now ranks by average review
    // rating first — but a course with no reviews yet sinks to the bottom rather than
    // disappearing, so a brand-new course can still be featured until enough rated courses
    // exist to fill all 6 slots.
    public class HomeFeaturedCoursesTests
    {
        private static ApplicationDbContext SeedCourses()
        {
            var context = TestHelpers.CreateInMemoryContext();

            var instructor = new ApplicationUser { Id = "home-instr", FullName = "Instructor", Email = "home.instr@example.com", UserName = "home.instr@example.com" };
            var reviewers = Enumerable.Range(1, 10)
                .Select(i => new ApplicationUser { Id = $"home-reviewer-{i}", FullName = $"Reviewer {i}", Email = $"home.reviewer{i}@example.com", UserName = $"home.reviewer{i}@example.com" })
                .ToList();
            context.Users.Add(instructor);
            context.Users.AddRange(reviewers);
            context.Categories.Add(new Category { Id = 80, Name = "Cat", Description = "Cat" });

            Course Make(int id, CourseStatus status) => new Course { Id = id, Title = $"Course {id}", Description = "d", CategoryId = 80, InstructorId = instructor.Id, Status = status };

            var c201 = Make(201, CourseStatus.Approved); // avg 5.0, 2 reviews  -> rank 1
            var c202 = Make(202, CourseStatus.Approved); // avg 5.0, 1 review   -> rank 2 (tie on avg, fewer reviews)
            var c203 = Make(203, CourseStatus.Approved); // avg 4.0, 3 reviews  -> rank 3
            var c204 = Make(204, CourseStatus.Approved); // avg 3.0, 1 review   -> rank 4
            var c205 = Make(205, CourseStatus.Approved); // no reviews, oldest -> dropped (Take(6) cap)
            var c206 = Make(206, CourseStatus.Approved); // no reviews, middle -> rank 6
            var c207 = Make(207, CourseStatus.Approved); // no reviews, newest -> rank 5
            var c208 = Make(208, CourseStatus.Rejected); // 5.0 avg, but never approved -> must never appear
            context.Courses.AddRange(c201, c202, c203, c204, c205, c206, c207, c208);
            context.SaveChanges();

            void Review(int id, int courseId, int studentIndex, int rating) =>
                context.Reviews.Add(new Review { Id = id, CourseId = courseId, StudentId = reviewers[studentIndex - 1].Id, Rating = rating, CreatedAt = System.DateTime.Now });

            Review(1, 201, 1, 5); Review(2, 201, 2, 5);
            Review(3, 202, 3, 5);
            Review(4, 203, 4, 4); Review(5, 203, 5, 4); Review(6, 203, 6, 4);
            Review(7, 204, 7, 3);
            Review(8, 208, 8, 5); Review(9, 208, 9, 5); Review(10, 208, 10, 5);

            context.SaveChanges();
            return context;
        }

        private static HomeController CreateController(ApplicationDbContext context)
        {
            var store = new Mock<IUserStore<ApplicationUser>>();
            var userManager = new Mock<UserManager<ApplicationUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
            userManager.Setup(m => m.GetUsersInRoleAsync("Admin")).ReturnsAsync(new List<ApplicationUser>());
            return new HomeController(context, userManager.Object);
        }

        [Fact]
        public async Task Index_OrdersFeaturedCoursesByAverageRatingDescending_ThenReviewCount_ThenNewest_AndExcludesUnapproved()
        {
            using var context = SeedCourses();
            var controller = CreateController(context);

            var result = await controller.Index();

            var courses = Assert.IsType<List<Course>>(Assert.IsType<ViewResult>(result).Model);
            Assert.Equal(new[] { 201, 202, 203, 204, 207, 206 }, courses.Select(c => c.Id).ToArray());
            Assert.DoesNotContain(courses, c => c.Id == 205); // dropped by the Take(6) cap
            Assert.DoesNotContain(courses, c => c.Id == 208); // Rejected — never featured regardless of rating
        }

        [Fact]
        public async Task Index_RatingsByCourseId_IsScopedToOnlyTheFeaturedCourses()
        {
            using var context = SeedCourses();
            var controller = CreateController(context);

            await controller.Index();

            var ratings = (Dictionary<int, (double Average, int Count)>)controller.ViewBag.RatingsByCourseId;
            Assert.Equal((5.0, 2), ratings[201]);
            Assert.Equal((5.0, 1), ratings[202]);
            Assert.Equal((4.0, 3), ratings[203]);
            Assert.False(ratings.ContainsKey(205)); // dropped course's rating isn't carried along
            Assert.False(ratings.ContainsKey(206)); // 206 has no reviews at all, so no dictionary entry
        }
    }
}
