using MediatR;
using Microsoft.Extensions.Options;
using Ticksi.Application.Interfaces;
using Ticksi.Application.Options;

namespace Ticksi.Application.Features.Posters.Commands.UploadPoster;

public class UploadPosterCommandHandler : IRequestHandler<UploadPosterCommand, UploadPosterResponse>
{
    private readonly IFileStorageService _fileStorageService;
    private readonly FileUploadOptions _uploadOptions;

    public UploadPosterCommandHandler(IFileStorageService fileStorageService, IOptions<FileUploadOptions> uploadOptions)
    {
        _fileStorageService = fileStorageService;
        _uploadOptions = uploadOptions.Value;
    }

    public async Task<UploadPosterResponse> Handle(UploadPosterCommand request, CancellationToken cancellationToken)
    {
        var posterPath = Path.Combine(_uploadOptions.EventPosterPath, request.EventPublicId.ToString());
        var url = await _fileStorageService.SaveFileAsync(request.File, posterPath, cancellationToken);

        return new UploadPosterResponse
        {
            Url = url,
            OriginalFileName = request.File.FileName,
            StoredFileName = Path.GetFileName(url),
            FileSizeBytes = request.File.Length,
            ContentType = request.File.ContentType
        };
    }
}
