using System.Threading.Tasks;
using EduLearn.Controllers;
using EduLearn.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace EduLearn.Tests.Integration
{
    // Edit Profile previously only let a user change their picture and bio — Name and Phone
    // Number were shown nowhere and could never be changed after registration/application.
    public class ProfileEditTests
    {
        private static (ApplicationUser user, ProfileController controller, Mock<UserManager<ApplicationUser>> userManager) CreateController()
        {
            var user = new ApplicationUser { Id = "profile-user-1", FullName = "Original Name", Email = "profile@example.com", UserName = "profile@example.com", PhoneNumber = "01711111111", Bio = "Old bio" };
            var userManager = TestHelpers.CreateMockUserManager(user);
            userManager.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

            var controller = new ProfileController(userManager.Object, TestHelpers.CreateFakeFileUploadService().Object);
            TestHelpers.AttachControllerContext(controller, user.Id);
            TestHelpers.AttachValidation(controller);
            return (user, controller, userManager);
        }

        [Fact]
        public async Task Edit_Get_PopulatesNamePhoneAndEmail_FromTheCurrentAccount()
        {
            var (_, controller, _) = CreateController();

            var result = await controller.Edit();

            var vm = Assert.IsType<ProfileEditViewModel>(Assert.IsType<ViewResult>(result).Model);
            Assert.Equal("Original Name", vm.FullName);
            Assert.Equal("01711111111", vm.PhoneNumber);
            Assert.Equal("profile@example.com", vm.Email);
        }

        [Fact]
        public async Task Edit_Post_WithValidNameAndPhone_UpdatesTheAccount()
        {
            var (user, controller, userManager) = CreateController();
            var model = new ProfileEditViewModel { FullName = "  Updated Name  ", PhoneNumber = "01898765432", Bio = "New bio" };
            controller.TryValidateModel(model);

            var result = await controller.Edit(model);

            Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Updated Name", user.FullName); // trimmed
            Assert.Equal("01898765432", user.PhoneNumber);
            Assert.Equal("New bio", user.Bio);
            userManager.Verify(m => m.UpdateAsync(user), Times.Once);
        }

        [Fact]
        public async Task Edit_Post_WithBlankPhoneNumber_ClearsItToNull()
        {
            var (user, controller, _) = CreateController();
            var model = new ProfileEditViewModel { FullName = "Original Name", PhoneNumber = "" }; // what a real emptied <input> submits
            controller.TryValidateModel(model);

            await controller.Edit(model);

            Assert.Null(user.PhoneNumber);
        }

        [Fact]
        public async Task Edit_Post_WithBlankName_IsRejected_AndTheAccountIsUnchanged()
        {
            var (user, controller, userManager) = CreateController();
            var model = new ProfileEditViewModel { FullName = "   ", PhoneNumber = "01711111111" };
            controller.TryValidateModel(model);

            var result = await controller.Edit(model);

            Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);
            Assert.Equal("Original Name", user.FullName);
            userManager.Verify(m => m.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Never);
        }

        [Fact]
        public async Task Edit_Post_WithAnInvalidPhoneNumber_IsRejected_AndTheAccountIsUnchanged()
        {
            var (user, controller, userManager) = CreateController();
            var model = new ProfileEditViewModel { FullName = "Original Name", PhoneNumber = "not-a-phone-number!!" };
            controller.TryValidateModel(model);

            var result = await controller.Edit(model);

            Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);
            Assert.Equal("01711111111", user.PhoneNumber);
            userManager.Verify(m => m.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Never);
        }

        [Fact]
        public async Task Edit_Post_NeverWritesTheSubmittedEmailBackToTheAccount()
        {
            // Email is rendered read-only and must stay that way even against a crafted POST
            // that includes an Email field — it's never assigned to the account anywhere.
            var (user, controller, _) = CreateController();
            var model = new ProfileEditViewModel { FullName = "Original Name", PhoneNumber = "01711111111", Email = "attacker@example.com" };
            controller.TryValidateModel(model);

            await controller.Edit(model);

            Assert.Equal("profile@example.com", user.Email);
        }
    }
}
