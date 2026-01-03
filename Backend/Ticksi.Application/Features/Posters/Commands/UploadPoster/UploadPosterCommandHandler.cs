using MediatR;
using Microsoft.Extensions.Configuration;
using Ticksi.Application.Interfaces;

using System.IO;

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
            var posterBasePath = _configuration["FileUpload:EventPosterPath"] ?? "images/events";

            var posterPath = Path.Combine(
                posterBasePath,
                request.EventPublicId.ToString()
            );

            // Save the file and get the URL
            var url = await _fileStorageService.SaveFileAsync(request.File, posterPath, cancellationToken);

            var storedFileName = Path.GetFileName(url);


            return new UploadPosterResponse
            {
                Url = url,
                OriginalFileName = request.File.FileName,
                StoredFileName = storedFileName,
                FileSizeBytes = request.File.Length,
                ContentType = request.File.ContentType
            };
        }
    }
}

