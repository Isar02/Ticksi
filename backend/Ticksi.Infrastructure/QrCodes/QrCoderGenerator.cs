using QRCoder;
using Ticksi.Application.Interfaces;

namespace Ticksi.Infrastructure.QrCodes;

public class QrCoderGenerator : IQrCodeGenerator
{
    private const int PixelsPerModule = 12;

    public byte[] RenderPng(string content)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
        return new PngByteQRCode(data).GetGraphic(PixelsPerModule);
    }
}
