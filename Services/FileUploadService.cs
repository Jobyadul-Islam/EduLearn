using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace EduLearn.Services
{
    public class FileUploadService : IFileUploadService
    {
        private readonly IWebHostEnvironment _env;
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
        private const long MaxSizeBytes = 1 * 1024 * 1024; // 1MB

        public FileUploadService(IWebHostEnvironment env)
        {
            _env = env;
        }

        public void ValidateImage(string extension, long length)
        {
            if (length == 0)
                throw new InvalidOperationException("Profile picture is required.");
            if (!AllowedExtensions.Contains(extension?.ToLowerInvariant()))
                throw new InvalidOperationException("Only JPG, PNG, or WEBP images are allowed.");
            if (length > MaxSizeBytes)
                throw new InvalidOperationException("Image must be under 1MB.");
        }

        public async Task<string> SaveImageAsync(IFormFile file, string subfolder)
        {
            var ext = Path.GetExtension(file.FileName);
            ValidateImage(ext, file.Length);

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            return await SaveImageAsync(ms.ToArray(), ext, subfolder);
        }

        public async Task<string> SaveImageAsync(byte[] fileBytes, string extension, string subfolder)
        {
            var fileName = $"{Guid.NewGuid()}{extension.ToLowerInvariant()}";
            var folderPath = Path.Combine(_env.WebRootPath, "uploads", subfolder);
            Directory.CreateDirectory(folderPath);

            var fullPath = Path.Combine(folderPath, fileName);
            await File.WriteAllBytesAsync(fullPath, fileBytes);

            return $"/uploads/{subfolder}/{fileName}";
        }

        public void DeleteImage(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath)) return;
            var fullPath = Path.Combine(_env.WebRootPath, relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(fullPath)) File.Delete(fullPath);
        }
    }
}