using Microsoft.AspNetCore.Http;
using Ticksi.Application.Interfaces;

namespace Ticksi.Tests.Common;

public sealed class FakeFileStorage : IFileStorageService
{
    private readonly HashSet<string> _files = [];

    public IReadOnlyCollection<string> Files => _files;

    public void Add(string relativePath) => _files.Add(relativePath);

    public Task<string> SaveFileAsync(IFormFile file, string subDirectory, CancellationToken cancellationToken = default)
    {
        var path = $"/{subDirectory}/{Guid.NewGuid()}{Path.GetExtension(file.FileName).ToLowerInvariant()}";
        _files.Add(path);
        return Task.FromResult(path);
    }

    public Task<bool> DeleteFileAsync(string relativePath) => Task.FromResult(_files.Remove(relativePath));

    public bool FileExists(string relativePath) => _files.Contains(relativePath);
}
