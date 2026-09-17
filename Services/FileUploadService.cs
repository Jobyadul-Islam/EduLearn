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

        private string PrivateRoot => Path.Combine(_env.ContentRootPath, "PrivateUploads");

        public async Task<string> SavePrivateFileAsync(IFormFile file, string subfolder, string[] allowedExtensions, long maxSizeBytes)
        {
            if (file == null || file.Length == 0)
                throw new InvalidOperationException("Please choose a file.");

            // GetFileName strips any directory component the client might have sent
            // (e.g. "../../evil.exe") so the saved name can never escape the target folder.
            var safeOriginalName = Path.GetFileName(file.FileName);
            var extension = Path.GetExtension(safeOriginalName)?.ToLowerInvariant() ?? "";

            if (!allowedExtensions.Contains(extension))
                throw new InvalidOperationException($"That file type isn't allowed. Accepted types: {string.Join(", ", allowedExtensions)}.");
            if (file.Length > maxSizeBytes)
                throw new InvalidOperationException($"File is too large. Maximum size is {maxSizeBytes / (1024 * 1024)}MB.");

            var folderPath = Path.Combine(PrivateRoot, subfolder);
            Directory.CreateDirectory(folderPath);

            var uniqueFileName = $"{Guid.NewGuid()}_{safeOriginalName}";
            var fullPath = Path.Combine(folderPath, uniqueFileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Same "/uploads/{subfolder}/{name}" shape the DB has always stored — only the
            // physical location (private folder, not wwwroot) actually changed.
            return $"/uploads/{subfolder}/{uniqueFileName}";
        }

        public string? ResolvePrivateFilePath(string storedPath, string subfolder)
        {
            if (string.IsNullOrWhiteSpace(storedPath)) return null;

            var fileName = Path.GetFileName(storedPath);
            if (string.IsNullOrEmpty(fileName)) return null;

            var privatePath = Path.Combine(PrivateRoot, subfolder, fileName);
            if (File.Exists(privatePath)) return privatePath;

            // Legacy fallback: files uploaded before private storage existed are still
            // sitting under wwwroot/uploads/{subfolder} — serve them from there instead of
            // breaking every link created before this fix.
            var legacyPath = Path.Combine(_env.WebRootPath, "uploads", subfolder, fileName);
            return File.Exists(legacyPath) ? legacyPath : null;
        }
    }
}
