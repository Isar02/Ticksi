using Ticksi.Application.Common;

namespace Ticksi.Tests.Common;

public static class TestImages
{
    public static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52];
    public static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01];
    public static readonly byte[] Gif = "GIF89a\u0001\0\u0001\0\0\0"u8.ToArray();
    public static readonly byte[] Webp = "RIFF$\0\0\0WEBPVP8 "u8.ToArray();
    public static readonly byte[] Text = "just some text, not an image"u8.ToArray();

    public static FileUpload File(string fileName, byte[] content) =>
        new(fileName, content.Length, () => new MemoryStream(content));
}
