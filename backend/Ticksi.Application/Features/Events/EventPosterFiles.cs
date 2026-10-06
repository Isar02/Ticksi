using Microsoft.EntityFrameworkCore;
using Ticksi.Application.Interfaces;

namespace Ticksi.Application.Features.Events;

internal static class EventPosterFiles
{
    // Runs after the change is saved, so it takes no cancellation; demo events can share one poster file.
    public static async Task RemoveIfUnusedAsync(IAppDbContext context, IFileStorageService files, string? posterUrl)
    {
        if (string.IsNullOrEmpty(posterUrl) || await context.Events.AnyAsync(e => e.PosterUrl == posterUrl))
            return;

        try
        {
            await files.DeleteFileAsync(posterUrl);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // The event no longer points at the file, so a leftover file is harmless.
        }
    }
}
