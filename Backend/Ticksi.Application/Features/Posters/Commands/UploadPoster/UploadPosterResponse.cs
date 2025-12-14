namespace Ticksi.Application.Features.Posters.Commands.UploadPoster
{
    public class UploadPosterResponse
    {
        /// <summary>
        /// The relative URL path to the uploaded poster image.
        /// </summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// The original filename of the uploaded file.
        /// </summary>
        public string OriginalFileName { get; set; } = string.Empty;

        /// <summary>
        /// The generated filename stored on the server.
        /// </summary>
        public string StoredFileName { get; set; } = string.Empty;

        /// <summary>
        /// The size of the uploaded file in bytes.
        /// </summary>

        public long FileSizeBytes { get; set; }

        /// <summary>
        /// The content type of the uploaded file.
        /// </summary>
        public string ContentType { get; set; } = string.Empty;
    }
}

