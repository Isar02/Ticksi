using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Ticksi.Application.Interfaces;

namespace Ticksi.Infrastructure.Services
{
    public class FileStorageService : IFileStorageService
    {
        private readonly IWebHostEnvironment _environment;

        public FileStorageService(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public async Task<string> SaveFileAsync(IFormFile file, string subDirectory, CancellationToken cancellationToken = default)
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("File is empty or null", nameof(file));
            }

            // Get the wwwroot path
            var wwwrootPath = _environment.WebRootPath;
            
            // If wwwroot doesn't exist, create it
            if (string.IsNullOrEmpty(wwwrootPath))
            {
                wwwrootPath = Path.Combine(_environment.ContentRootPath, "wwwroot");
            }

            // Create the full directory path
            var directoryPath = Path.Combine(wwwrootPath, subDirectory);

            // Create directory if it doesn't exist
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            // Generate unique filename
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(directoryPath, uniqueFileName);

            // Save the file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream, cancellationToken);
            }

            // Return the relative URL path (using forward slashes for URL)
            var relativePath = $"/{subDirectory.Replace("\\", "/")}/{uniqueFileName}";
            return relativePath;
        }

        public Task<bool> DeleteFileAsync(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
            {
                return Task.FromResult(false);
            }

            var wwwrootPath = _environment.WebRootPath;
            if (string.IsNullOrEmpty(wwwrootPath))
            {
                wwwrootPath = Path.Combine(_environment.ContentRootPath, "wwwroot");
            }

            // Remove leading slash and convert to system path
            var cleanPath = relativePath.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString());
            var fullPath = Path.Combine(wwwrootPath, cleanPath);

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }

        public bool FileExists(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
            {
                return false;
            }

            var wwwrootPath = _environment.WebRootPath;
            if (string.IsNullOrEmpty(wwwrootPath))
            {
                wwwrootPath = Path.Combine(_environment.ContentRootPath, "wwwroot");
            }

            var cleanPath = relativePath.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString());
            var fullPath = Path.Combine(wwwrootPath, cleanPath);

            return File.Exists(fullPath);
        }
    }
}

