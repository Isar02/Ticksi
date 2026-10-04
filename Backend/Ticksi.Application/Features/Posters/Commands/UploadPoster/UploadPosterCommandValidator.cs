using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Ticksi.Application.Options;

namespace Ticksi.Application.Features.Posters.Commands.UploadPoster;

public class UploadPosterCommandValidator : AbstractValidator<UploadPosterCommand>
{
    private readonly FileUploadOptions _uploadOptions;

    public UploadPosterCommandValidator(IOptions<FileUploadOptions> uploadOptions)
    {
        _uploadOptions = uploadOptions.Value;
        var maxMegabytes = _uploadOptions.MaxFileSizeBytes / 1024 / 1024;

        RuleFor(x => x.File)
            .NotNull().WithMessage("File is required.");

        RuleFor(x => x.File)
            .Must(file => file.Length > 0).WithMessage("File cannot be empty.")
            .Must(file => file.Length <= _uploadOptions.MaxFileSizeBytes)
            .WithMessage($"File size cannot exceed {maxMegabytes}MB.")
            .Must(HaveAllowedExtension)
            .WithMessage($"Invalid file type. Allowed types: {string.Join(", ", _uploadOptions.AllowedImageTypes)}")
            .When(x => x.File != null);
    }

    private bool HaveAllowedExtension(IFormFile file) =>
        _uploadOptions.AllowedImageTypes.Contains(Path.GetExtension(file.FileName), StringComparer.OrdinalIgnoreCase);
}
