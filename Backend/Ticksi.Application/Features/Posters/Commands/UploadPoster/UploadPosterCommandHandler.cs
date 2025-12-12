using MediatR;
using Microsoft.Extensions.Configuration;
using Ticksi.Application.Interfaces;

namespace Ticksi.Application.Features.Posters.Commands.UploadPoster
{
    public class UploadPosterCommandHandler : IRequestHandler<UploadPosterCommand, UploadPosterResponse>
    {
        private readonly IFileStorageService _fileStorageService;
        private readonly IConfiguration _configuration;

        public UploadPosterCommandHandler(
            IFileStorageService fileStorageService,
            IConfiguration configuration)
        {
            _fileStorageService = fileStorageService;
            _configuration = configuration;
        }

        public async Task<UploadPosterResponse> Handle(UploadPosterCommand request, CancellationToken cancellationToken)
        {
            // Get the configured path for event posters
            var posterPath = _configuration["FileUpload:EventPosterPath"] ?? "images/events";

            // Save the file and get the URL
            var url = await _fileStorageService.SaveFileAsync(request.File, posterPath, cancellationToken);

            return new UploadPosterResponse
            {
                Url = url,
                OriginalFileName = request.File.FileName,
                FileSizeBytes = request.File.Length,
                ContentType = request.File.ContentType
            };
        }
    }
}

