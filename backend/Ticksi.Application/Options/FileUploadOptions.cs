using System.ComponentModel.DataAnnotations;

namespace Ticksi.Application.Options;

public sealed class FileUploadOptions
{
    public const string SectionName = "FileUpload";

    [Range(1, 50 * 1024 * 1024)] public long MaxFileSizeBytes { get; init; } = 5 * 1024 * 1024;
    [Required, MinLength(1)] public string[] AllowedImageTypes { get; init; } = [];
    [Required] public string EventPosterPath { get; init; } = string.Empty;
    [Required] public string CategoryPosterPath { get; init; } = string.Empty;
}
