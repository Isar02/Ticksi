using FluentValidation;
using Microsoft.Extensions.Configuration;

namespace Ticksi.Application.Features.Posters.Commands.UploadPoster
{
    public class UploadPosterCommandValidator : AbstractValidator<UploadPosterCommand>
    {
        private readonly long _maxFileSizeBytes;
        private readonly string[] _allowedImageTypes;

        public UploadPosterCommandValidator(IConfiguration configuration)
        {
            // Get configuration values with defaults
            _maxFileSizeBytes = configuration.GetValue<long>("FileUpload:MaxFileSizeBytes", 5242880); // 5MB default
            _allowedImageTypes = configuration.GetSection("FileUpload:AllowedImageTypes").Get<string[]>() 
                ?? new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

            RuleFor(x => x.File)
                .NotNull()
                .WithMessage("File is required.");

            RuleFor(x => x.File)
                .Must(file => file != null && file.Length > 0)
                .WithMessage("File cannot be empty.")
                .When(x => x.File != null);

            RuleFor(x => x.File)
                .Must(file => file != null && file.Length <= _maxFileSizeBytes)
                .WithMessage($"File size cannot exceed {_maxFileSizeBytes / 1024 / 1024}MB.")
                .When(x => x.File != null);

            RuleFor(x => x.File)
                .Must(BeValidImageType)
                .WithMessage($"Invalid file type. Allowed types: {string.Join(", ", _allowedImageTypes)}")
                .When(x => x.File != null);
        }

        private bool BeValidImageType(Microsoft.AspNetCore.Http.IFormFile? file)
        {
            if (file == null)
                return false;

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            return _allowedImageTypes.Contains(extension);
        }
    }
}

