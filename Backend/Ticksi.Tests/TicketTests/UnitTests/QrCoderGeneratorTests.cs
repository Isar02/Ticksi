using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;
using Ticksi.Infrastructure.QrCodes;
using ZXing;
using ZXing.Common;
using ZXing.QrCode;

namespace Ticksi.Tests.TicketTests.UnitTests;

public class QrCoderGeneratorTests
{
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    [Theory]
    [InlineData("ABCDEFGH2345")]
    [InlineData("ZYXWVUTS9876")]
    public void RenderPng_ScansBackToExactlyTheTicketCode(string code)
    {
        var png = new QrCoderGenerator().RenderPng(code);

        Assert.Equal(code, Scan(png));
    }

    [Fact]
    public void RenderPng_IsLargeEnoughToScanFromAPhoneScreen()
    {
        var png = new QrCoderGenerator().RenderPng("ABCDEFGH2345");

        Assert.Equal(PngSignature, png[..8]);
        var (width, height) = Size(png);
        Assert.Equal(width, height);
        Assert.True(width >= 200, $"The code is {width} px wide.");
    }

    private static string? Scan(byte[] png)
    {
        var (width, height) = Size(png);
        var source = new RGBLuminanceSource(Grayscale(png, width, height), width, height, RGBLuminanceSource.BitmapFormat.Gray8);
        return new QRCodeReader().decode(new BinaryBitmap(new HybridBinarizer(source)))?.Text;
    }

    private static (int Width, int Height) Size(byte[] png) =>
        (BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(16, 4)), BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(20, 4)));

    // The generator writes 1-bit grayscale rows without filters, so the pixels can be read straight from the image data.
    private static byte[] Grayscale(byte[] png, int width, int height)
    {
        Assert.Equal((1, 0), (png[24], png[25]));

        using var compressed = new MemoryStream();
        for (var offset = 8; offset < png.Length;)
        {
            var length = BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(offset, 4));
            if (Encoding.ASCII.GetString(png, offset + 4, 4) == "IDAT")
                compressed.Write(png, offset + 8, length);
            offset += length + 12;
        }

        compressed.Position = 0;
        using var rows = new MemoryStream();
        using (var zlib = new ZLibStream(compressed, CompressionMode.Decompress))
            zlib.CopyTo(rows);

        var data = rows.ToArray();
        var stride = (width + 7) / 8 + 1;
        var pixels = new byte[width * height];
        for (var y = 0; y < height; y++)
        {
            Assert.Equal(0, data[y * stride]);
            for (var x = 0; x < width; x++)
            {
                var bit = (data[y * stride + 1 + x / 8] >> (7 - x % 8)) & 1;
                pixels[y * width + x] = (byte)(bit * 255);
            }
        }
        return pixels;
    }
}
