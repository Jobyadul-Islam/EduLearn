using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using EduLearn.Models;
using EduLearn.Models.ViewModels;
using Xunit;

namespace EduLearn.Tests.Services
{
    // Phone numbers must be exactly 11 digits (the Bangladeshi mobile format this app is
    // built around, e.g. 01712345678) everywhere one is collected: the profile page (where
    // it's optional) and the instructor application (where it's required).
    public class PhoneNumberValidationTests
    {
        private static bool IsValid(object model, out List<ValidationResult> errors)
        {
            errors = new List<ValidationResult>();
            return Validator.TryValidateObject(model, new ValidationContext(model), errors, validateAllProperties: true);
        }

        [Theory]
        [InlineData("01712345678")] // exactly 11 digits
        [InlineData(null)]          // optional — blank is fine
        [InlineData("")]
        public void ProfileEditViewModel_AcceptsElevenDigitsOrBlank(string? phone)
        {
            var model = new ProfileEditViewModel { FullName = "Someone", PhoneNumber = phone };
            Assert.True(IsValid(model, out _));
        }

        [Theory]
        [InlineData("0171234567")]      // 10 digits
        [InlineData("017123456789")]    // 12 digits
        [InlineData("+8801712345678")]  // digits but with a country-code prefix
        [InlineData("01712-45678")]     // digits but with a dash
        [InlineData("phone number")]    // not digits at all
        public void ProfileEditViewModel_RejectsAnythingThatIsNotExactlyElevenDigits(string phone)
        {
            var model = new ProfileEditViewModel { FullName = "Someone", PhoneNumber = phone };
            Assert.False(IsValid(model, out var errors));
            Assert.Contains(errors, e => e.MemberNames.Contains(nameof(ProfileEditViewModel.PhoneNumber)));
        }

        private static InstructorApplicationViewModel ValidApplication(string phone) => new InstructorApplicationViewModel
        {
            FullName = "Someone",
            Email = "someone@example.com",
            PhoneNumber = phone,
            Qualification = "MSc",
            Institution = "Test University",
            Skills = "Testing",
            YearsOfExperience = 3,
            Bio = "A bio"
        };

        [Fact]
        public void InstructorApplicationViewModel_AcceptsElevenDigits()
        {
            Assert.True(IsValid(ValidApplication("01712345678"), out _));
        }

        [Theory]
        [InlineData("")]                // required — unlike the profile page, blank is NOT allowed here
        [InlineData("0171234567")]      // 10 digits
        [InlineData("017123456789")]    // 12 digits
        public void InstructorApplicationViewModel_RejectsBlankOrWrongLength(string phone)
        {
            Assert.False(IsValid(ValidApplication(phone), out var errors));
            Assert.Contains(errors, e => e.MemberNames.Contains(nameof(InstructorApplicationViewModel.PhoneNumber)));
        }
    }
}
