namespace gestaotcc.Application.Gateways;

public interface IMinioGateway
{
    Task Send(string fileName, byte[] file, string contentType);
    Task<byte[]> Download(string fileName);
    Task<string> GetPresignedUrl(string fileName);
}