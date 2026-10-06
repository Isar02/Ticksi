namespace Ticksi.Application.Interfaces;

public interface IFileStorageService
{
    Task<string> SaveFileAsync(Stream content, string extension, string subDirectory, CancellationToken cancellationToken = default);
    Task<bool> DeleteFileAsync(string relativePath, CancellationToken cancellationToken = default);
}
