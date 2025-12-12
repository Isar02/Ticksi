using Microsoft.AspNetCore.Http;

namespace Ticksi.Application.Interfaces
{
    public interface IFileStorageService
    {
        /// <summary>
        /// Saves a file to the specified subdirectory and returns the relative URL path.
        /// </summary>
        /// <param name="file">The file to save</param>
        /// <param name="subDirectory">The subdirectory within wwwroot (e.g., "images/events")</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The relative URL path to the saved file</returns>
        Task<string> SaveFileAsync(IFormFile file, string subDirectory, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a file at the specified relative path.
        /// </summary>
        /// <param name="relativePath">The relative path to the file</param>
        /// <returns>True if the file was deleted, false if it didn't exist</returns>
        Task<bool> DeleteFileAsync(string relativePath);

        /// <summary>
        /// Checks if a file exists at the specified relative path.
        /// </summary>
        /// <param name="relativePath">The relative path to the file</param>
        /// <returns>True if the file exists</returns>
        bool FileExists(string relativePath);
    }
}

