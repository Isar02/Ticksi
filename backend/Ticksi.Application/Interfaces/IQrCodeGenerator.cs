namespace Ticksi.Application.Interfaces;

public interface IQrCodeGenerator
{
    byte[] RenderPng(string content);
}
