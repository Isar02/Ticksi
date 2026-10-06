using Ticksi.Application.Interfaces;

namespace Ticksi.Tests.Common;

public sealed class FakeFileStorage : IFileStorageService
{
    private readonly HashSet<string> _files = [];

    public IReadOnlyCollection<string> Files => _files;

    public void Add(string relativePath) => _files.Add(relativePath);

    public Task<string> SaveFileAsync(Stream content, string extension, string subDirectory, CancellationToken cancellationToken = default)
    {
        var path = $"/{subDirectory}/{Guid.NewGuid()}{extension}";
        _files.Add(path);
        return Task.FromResult(path);
    }

    public Task<bool> DeleteFileAsync(string relativePath, CancellationToken cancellationToken = default) =>
        Task.FromResult(_files.Remove(relativePath));
}
