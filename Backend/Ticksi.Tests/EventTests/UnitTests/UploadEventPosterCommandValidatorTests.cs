using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Ticksi.Application.Features.Events.Commands.UploadEventPoster;
using Ticksi.Application.Options;
using Ticksi.Tests.Common;

namespace Ticksi.Tests.EventTests.UnitTests;

public class UploadEventPosterCommandValidatorTests
{
    private static readonly FileUploadOptions UploadOptions = new()
    {
        MaxFileSizeBytes = 3 * 512 * 1024,
        AllowedImageTypes = [".jpg", ".jpeg", ".png", ".gif", ".webp"],
        EventPosterPath = "images/events",
        CategoryPosterPath = "images/categories"
    };

    private readonly UploadEventPosterCommandValidator _validator = new(Options.Create(UploadOptions));

    [Theory]
    [InlineData("poster.png", "png")]
    [InlineData("poster.JPG", "jpeg")]
    [InlineData("poster.jpeg", "jpeg")]
    [InlineData("poster.gif", "gif")]
    [InlineData("poster.webp", "webp")]
    public async Task Validate_ImageMatchingItsExtension_Passes(string fileName, string format)
    {
        var content = format switch
        {
            "png" => TestImages.Png,
            "jpeg" => TestImages.Jpeg,
            "gif" => TestImages.Gif,
            _ => TestImages.Webp
        };

        var result = await _validator.ValidateAsync(Command(TestImages.File(fileName, content)));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("poster.png", "text")]
    [InlineData("poster.jpg", "png")]
    [InlineData("poster.webp", "riff")]
    public async Task Validate_ContentThatIsNotThatImageType_IsRejected(string fileName, string content)
    {
        var bytes = content switch
        {
            "text" => TestImages.Text,
            "png" => TestImages.Png,
            _ => "RIFF$\0\0\0WAVEfmt "u8.ToArray()
        };

        var result = await _validator.ValidateAsync(Command(TestImages.File(fileName, bytes)));

        Assert.Equal("The file is not a valid image of its type.", Assert.Single(result.Errors).ErrorMessage);
    }

    [Fact]
    public async Task Validate_DisallowedExtension_IsRejectedBeforeReadingTheContent()
    {
        var result = await _validator.ValidateAsync(Command(TestImages.File("poster.svg", TestImages.Png)));

        Assert.Equal("The poster must be one of: .jpg, .jpeg, .png, .gif, .webp.", Assert.Single(result.Errors).ErrorMessage);
    }

    [Fact]
    public async Task Validate_FileOverTheSizeLimit_IsRejected()
    {
        var content = TestImages.Png.Concat(new byte[3 * 512 * 1024]).ToArray();

        var result = await _validator.ValidateAsync(Command(TestImages.File("poster.png", content)));

        Assert.Equal("The poster can be at most 1.5 MB.", Assert.Single(result.Errors).ErrorMessage);
    }

    [Fact]
    public async Task Validate_EmptyOrMissingFileAndEvent_AreRejected()
    {
        var empty = await _validator.ValidateAsync(Command(TestImages.File("poster.png", [])));
        var missing = await _validator.ValidateAsync(new UploadEventPosterCommand());

        Assert.Equal("The poster file is empty.", Assert.Single(empty.Errors).ErrorMessage);
        Assert.Equal(["File", "PublicId"], missing.Errors.Select(e => e.PropertyName).Order());
    }

    private static UploadEventPosterCommand Command(IFormFile file) => new() { PublicId = Guid.NewGuid(), File = file };
}
