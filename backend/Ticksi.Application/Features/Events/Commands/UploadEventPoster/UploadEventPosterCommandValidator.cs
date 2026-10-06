using System.Globalization;
using FluentValidation;
using Microsoft.Extensions.Options;
using Ticksi.Application.Options;

namespace Ticksi.Application.Features.Events.Commands.UploadEventPoster;

public class UploadEventPosterCommandValidator : AbstractValidator<UploadEventPosterCommand>
{
    public UploadEventPosterCommandValidator(IOptions<FileUploadOptions> uploadOptions)
    {
        var options = uploadOptions.Value;
        var maxMegabytes = (options.MaxFileSizeBytes / 1024d / 1024d).ToString("0.##", CultureInfo.InvariantCulture);
        var allowedTypes = string.Join(", ", options.AllowedImageTypes);

        RuleFor(x => x.PublicId).NotEmpty().WithMessage("Event is required.");

        RuleFor(x => x.File)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("Choose a poster image.")
            .Must(file => file!.Length > 0).WithMessage("The poster file is empty.")
            .Must(file => file!.Length <= options.MaxFileSizeBytes)
            .WithMessage($"The poster can be at most {maxMegabytes} MB.")
            .Must(file => options.AllowedImageTypes.Contains(file!.Extension, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"The poster must be one of: {allowedTypes}.")
            .MustAsync((file, cancellationToken) => ImageSignatures.MatchesExtensionAsync(file!, cancellationToken))
            .WithMessage("The file is not a valid image of its type.");
    }
}
