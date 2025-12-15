using FluentValidation;
using Microsoft.Extensions.Configuration;

namespace Ticksi.Application.Features.Categories.Commands.UploadCategoryPoster
{
    public class UploadCategoryPosterCommandValidator : AbstractValidator<UploadCategoryPosterCommand>
    {
        private readonly long _maxFileSizeBytes;
        private readonly string[] _allowedImageTypes;

        public UploadCategoryPosterCommandValidator(IConfiguration configuration)
        {
            _maxFileSizeBytes = configuration.GetValue<long>("FileUpload:MaxFileSizeBytes", 5242880);
            _allowedImageTypes = configuration.GetSection("FileUpload:AllowedImageTypes").Get<string[]>()
                ?? new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

            RuleFor(x => x.File).NotNull().WithMessage("File is required.");

            RuleFor(x => x.File)
                .Must(f => f != null && f.Length > 0)
                .WithMessage("File cannot be empty.")
                .When(x => x.File != null);

            RuleFor(x => x.File)
                .Must(f => f != null && f.Length <= _maxFileSizeBytes)
                .WithMessage($"File size cannot exceed {_maxFileSizeBytes / 1024 / 1024}MB.")
                .When(x => x.File != null);

            RuleFor(x => x.File)
                .Must(BeValidImageType)
                .WithMessage($"Invalid file type. Allowed types: {string.Join(", ", _allowedImageTypes)}")
                .When(x => x.File != null);
        }

        private bool BeValidImageType(Microsoft.AspNetCore.Http.IFormFile? file)
        {
            if (file == null) return false;
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            return _allowedImageTypes.Contains(extension);
        }
    }
}
