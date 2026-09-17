using Microsoft.AspNetCore.Http;

namespace EduLearn.Services
{
    public interface IFileUploadService
    {
        Task<string> SaveImageAsync(IFormFile file, string subfolder);
        Task<string> SaveImageAsync(byte[] fileBytes, string extension, string subfolder);
        void ValidateImage(string extension, long length);
        void DeleteImage(string relativePath);

        // Saves to a folder outside wwwroot so the file can never be reached by a direct
        // static-file URL — only through an authenticated controller action that resolves
        // the stored path back with ResolvePrivateFilePath. Returns the same
        // "/uploads/{subfolder}/{name}"-shaped string the DB has always stored, so no data
        // migration is needed; only where the bytes physically live changes.
        Task<string> SavePrivateFileAsync(IFormFile file, string subfolder, string[] allowedExtensions, long maxSizeBytes);

        // Resolves a stored path back to a physical file. Checks the private folder first;
        // falls back to the legacy wwwroot/uploads location so files saved before this
        // folder existed still serve correctly. Returns null if the file isn't found in either.
        string? ResolvePrivateFilePath(string storedPath, string subfolder);
    }
}
