namespace Ticksi.Application.Common;

public sealed record FileUpload(string FileName, long Length, Func<Stream> OpenReadStream)
{
    public string Extension => Path.GetExtension(FileName).ToLowerInvariant();
}
