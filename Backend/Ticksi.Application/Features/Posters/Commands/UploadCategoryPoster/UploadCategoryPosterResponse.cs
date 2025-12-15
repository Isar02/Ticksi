namespace Ticksi.Application.Features.Categories.Commands.UploadCategoryPoster
{
    public class UploadCategoryPosterResponse
    {
        public string Url { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public string ContentType { get; set; } = string.Empty;
    }
}
