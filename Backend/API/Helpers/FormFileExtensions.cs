using Ticksi.Application.Common;

namespace API.Helpers;

public static class FormFileExtensions
{
    public static FileUpload? ToUpload(this IFormFile? file) =>
        file is null ? null : new FileUpload(file.FileName, file.Length, file.OpenReadStream);
}
