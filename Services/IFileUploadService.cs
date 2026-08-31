using Microsoft.AspNetCore.Http;

namespace EduLearn.Services
{
    public interface IFileUploadService
    {
        Task<string> SaveImageAsync(IFormFile file, string subfolder);
        Task<string> SaveImageAsync(byte[] fileBytes, string extension, string subfolder);
        void ValidateImage(string extension, long length);
        void DeleteImage(string relativePath);
    }
}