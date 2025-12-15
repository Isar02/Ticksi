using MediatR;
using Microsoft.Extensions.Configuration;
using Ticksi.Application.Interfaces;

namespace Ticksi.Application.Features.Categories.Commands.UploadCategoryPoster
{
    public class UploadCategoryPosterCommandHandler
        : IRequestHandler<UploadCategoryPosterCommand, UploadCategoryPosterResponse>
    {
        private readonly IFileStorageService _fileStorageService;
        private readonly IConfiguration _configuration;

        public UploadCategoryPosterCommandHandler(IFileStorageService fileStorageService, IConfiguration configuration)
        {
            _fileStorageService = fileStorageService;
            _configuration = configuration;
        }

        public async Task<UploadCategoryPosterResponse> Handle(UploadCategoryPosterCommand request, CancellationToken cancellationToken)
        {
            var categoryPosterPath = _configuration["FileUpload:CategoryPosterPath"] ?? "images/categories";

            var url = await _fileStorageService.SaveFileAsync(request.File, categoryPosterPath, cancellationToken);

            return new UploadCategoryPosterResponse
            {
                Url = url,
                OriginalFileName = request.File.FileName,
                FileSizeBytes = request.File.Length,
                ContentType = request.File.ContentType
            };
        }
    }
}
