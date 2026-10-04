using MediatR;
using Microsoft.Extensions.Options;
using Ticksi.Application.Interfaces;
using Ticksi.Application.Options;

namespace Ticksi.Application.Features.Categories.Commands.UploadCategoryPoster
{
    public class UploadCategoryPosterCommandHandler
        : IRequestHandler<UploadCategoryPosterCommand, UploadCategoryPosterResponse>
    {
        private readonly IFileStorageService _fileStorageService;
        private readonly FileUploadOptions _uploadOptions;

        public UploadCategoryPosterCommandHandler(IFileStorageService fileStorageService, IOptions<FileUploadOptions> uploadOptions)
        {
            _fileStorageService = fileStorageService;
            _uploadOptions = uploadOptions.Value;
        }

        public async Task<UploadCategoryPosterResponse> Handle(UploadCategoryPosterCommand request, CancellationToken cancellationToken)
        {
            var url = await _fileStorageService.SaveFileAsync(request.File, _uploadOptions.CategoryPosterPath, cancellationToken);

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
