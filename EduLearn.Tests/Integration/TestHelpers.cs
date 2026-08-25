using System;
using System.Security.Claims;
using System.Threading.Tasks;
using EduLearn.Data;
using EduLearn.Models;
using EduLearn.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace EduLearn.Tests.Integration
{
    /// <summary>
    /// Shared scaffolding for exercising real controller actions against a fresh
    /// EF Core InMemory database, without needing a full ASP.NET Core host.
    /// </summary>
    public static class TestHelpers
    {
        public static ApplicationDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }

        public static Mock<UserManager<ApplicationUser>> CreateMockUserManager(ApplicationUser user)
        {
            var store = new Mock<IUserStore<ApplicationUser>>();
            var mgr = new Mock<UserManager<ApplicationUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
            mgr.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(user.Id);
            mgr.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
            return mgr;
        }

        public static Mock<IEmailService> CreateFakeEmailService()
        {
            var mock = new Mock<IEmailService>();
            mock.Setup(m => m.IsConfigured).Returns(false);
            mock.Setup(m => m.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(true);
            return mock;
        }

        /// <summary>
        /// Wires up HttpContext/TempData on a controller so actions that read TempData
        /// (e.g. TempData["CertificateError"]) don't throw outside a real request pipeline.
        /// </summary>
        public static void AttachControllerContext(ControllerBase controller, string userId)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId)
            }, "TestAuth"));

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext,
                ActionDescriptor = new ControllerActionDescriptor()
            };

            if (controller is Controller mvcController)
            {
                mvcController.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
            }
        }
    }
}
