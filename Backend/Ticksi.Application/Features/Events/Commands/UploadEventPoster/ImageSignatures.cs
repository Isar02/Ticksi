using Microsoft.AspNetCore.Http;

namespace Ticksi.Application.Features.Events.Commands.UploadEventPoster;

internal static class ImageSignatures
{
    private const int HeaderLength = 12;

    public static async Task<bool> MatchesExtensionAsync(IFormFile file, CancellationToken cancellationToken)
    {
        var header = new byte[HeaderLength];
        await using var stream = file.OpenReadStream();
        var read = await stream.ReadAtLeastAsync(header, HeaderLength, throwOnEndOfStream: false, cancellationToken);

        return Matches(Path.GetExtension(file.FileName).ToLowerInvariant(), header.AsSpan(0, read));
    }

    private static bool Matches(string extension, ReadOnlySpan<byte> header) => extension switch
    {
        ".jpg" or ".jpeg" => header.StartsWith(new byte[] { 0xFF, 0xD8, 0xFF }),
        ".png" => header.StartsWith(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
        ".gif" => header.StartsWith("GIF87a"u8) || header.StartsWith("GIF89a"u8),
        ".webp" => header.Length == HeaderLength && header.StartsWith("RIFF"u8) && header[8..].StartsWith("WEBP"u8),
        _ => false
    };
}
