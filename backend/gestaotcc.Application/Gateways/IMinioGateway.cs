namespace gestaotcc.Application.Gateways;

public interface IMinioGateway
{
    Task Send(string fileName, byte[] file, string contentType);
    Task<byte[]> Download(string fileName, bool signedDocument);
    Task<string> GetPresignedUrl(string fileName, bool signedDocument);
    Task<byte[]> DownloadFolderAsZip(string folderName);
}