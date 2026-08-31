using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using EduLearn.Models;
using EduLearn.Services;

namespace EduLearn.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IFileUploadService _fileUploadService;

        public ProfileController(UserManager<ApplicationUser> userManager, IFileUploadService fileUploadService)
        {
            _userManager = userManager;
            _fileUploadService = fileUploadService;
        }

        // GET /Profile           -> own profile
        // GET /Profile?userId=x  -> Admin viewing someone else's
        [HttpGet]
        public async Task<IActionResult> Index(string? userId = null)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            var isAdmin = User.IsInRole("Admin");
            var targetId = userId ?? currentUser.Id;

            if (targetId != currentUser.Id && !isAdmin)
                return Forbid();

            var targetUser = targetId == currentUser.Id
                ? currentUser
                : await _userManager.FindByIdAsync(targetId);

            if (targetUser == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(targetUser);

            var vm = new ProfileViewModel
            {
                UserId = targetUser.Id,
                FullName = targetUser.FullName,
                Email = targetUser.Email ?? string.Empty,
                ProfilePicture = targetUser.ProfilePicture,
                Bio = targetUser.Bio,
                Role = roles.Count > 0 ? roles[0] : "Student",
                IsViewingOwnProfile = targetId == currentUser.Id
            };

            return View(vm);
        }

        // GET /Profile/Edit — self only, no userId param exists on purpose
        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            var vm = new ProfileEditViewModel
            {
                Bio = currentUser.Bio,
                CurrentProfilePicture = currentUser.ProfilePicture
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProfileEditViewModel model)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            if (!ModelState.IsValid)
            {
                model.CurrentProfilePicture = currentUser.ProfilePicture;
                return View(model);
            }

            currentUser.Bio = model.Bio;

            // The raw file the user picked only triggers the client-side cropper — what
            // actually gets saved is the cropped square JPEG it produces, sent back as
            // CroppedPictureData. Empty means "didn't change the picture."
            if (!string.IsNullOrEmpty(model.CroppedPictureData))
            {
                try
                {
                    var commaIndex = model.CroppedPictureData.IndexOf(',');
                    var base64 = commaIndex >= 0 ? model.CroppedPictureData[(commaIndex + 1)..] : model.CroppedPictureData;
                    var pictureBytes = Convert.FromBase64String(base64);

                    var newPath = await _fileUploadService.SaveImageAsync(pictureBytes, ".jpg", "profiles");
                    var oldPath = currentUser.ProfilePicture;
                    currentUser.ProfilePicture = newPath;

                    // Clean up the old picture only after the new one is safely saved
                    if (!string.IsNullOrEmpty(oldPath))
                        _fileUploadService.DeleteImage(oldPath);
                }
                catch (Exception ex) when (ex is FormatException || ex is InvalidOperationException)
                {
                    ModelState.AddModelError(nameof(model.NewProfilePicture), ex is InvalidOperationException ? ex.Message : "That image couldn't be processed. Please choose it again.");
                    model.CurrentProfilePicture = currentUser.ProfilePicture;
                    return View(model);
                }
            }

            await _userManager.UpdateAsync(currentUser);
            return RedirectToAction(nameof(Index));
        }
    }
}