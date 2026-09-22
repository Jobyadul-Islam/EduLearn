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
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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

        public static Mock<IFileUploadService> CreateFakeFileUploadService()
        {
            var mock = new Mock<IFileUploadService>();
            mock.Setup(m => m.SavePrivateFileAsync(It.IsAny<IFormFile>(), It.IsAny<string>(), It.IsAny<string[]>(), It.IsAny<long>()))
                .ReturnsAsync("/uploads/test/fake-file.dat");
            mock.Setup(m => m.SaveImageAsync(It.IsAny<IFormFile>(), It.IsAny<string>()))
                .ReturnsAsync("/uploads/test/fake-image.jpg");
            mock.Setup(m => m.ResolvePrivateFilePath(It.IsAny<string>(), It.IsAny<string>()))
                .Returns((string _, string _) => null);
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

        private static readonly IServiceProvider ValidationServices = new ServiceCollection()
            .AddControllersWithViews()
            .Services
            .BuildServiceProvider();

        /// <summary>
        /// Wires up the real ASP.NET Core model-metadata/validation pipeline (the same one
        /// Program.cs registers via AddControllersWithViews) on a controller built with
        /// `new`, so controller.TryValidateModel(x) — and therefore [Required]/[Range]/
        /// IValidatableObject — actually runs instead of throwing (a bare controller has no
        /// ObjectValidator/MetadataProvider wired unless the real MVC host constructed it).
        /// </summary>
        public static void AttachValidation(ControllerBase controller)
        {
            controller.MetadataProvider = ValidationServices.GetRequiredService<IModelMetadataProvider>();
            controller.ObjectValidator = ValidationServices.GetRequiredService<IObjectModelValidator>();
        }
    }
}
