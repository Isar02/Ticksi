using Microsoft.AspNetCore.Hosting;
using Ticksi.Application.Interfaces;

namespace Ticksi.Infrastructure.Services;

public class FileStorageService : IFileStorageService
{
    private readonly IWebHostEnvironment _environment;

    public FileStorageService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public async Task<string> SaveFileAsync(Stream content, string extension, string subDirectory, CancellationToken cancellationToken = default)
    {
        var directory = Path.Combine(WebRoot, subDirectory);
        Directory.CreateDirectory(directory);

        var fileName = $"{Guid.NewGuid()}{extension.ToLowerInvariant()}";
        await using (var target = new FileStream(Path.Combine(directory, fileName), FileMode.CreateNew))
        {
            await content.CopyToAsync(target, cancellationToken);
        }

        return $"/{subDirectory.Replace('\\', '/').Trim('/')}/{fileName}";
    }

    public Task<bool> DeleteFileAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(relativePath))
            return Task.FromResult(false);

        var fullPath = Path.Combine(WebRoot, relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(fullPath))
            return Task.FromResult(false);

        File.Delete(fullPath);
        return Task.FromResult(true);
    }

    private string WebRoot =>
        string.IsNullOrEmpty(_environment.WebRootPath) ? Path.Combine(_environment.ContentRootPath, "wwwroot") : _environment.WebRootPath;
}
