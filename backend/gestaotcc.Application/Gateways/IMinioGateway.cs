namespace gestaotcc.Application.Gateways;

public interface IMinioGateway
{
    Task Send(string fileName, byte[] file, string contentType, bool isFilledPdfProcess);
    Task<byte[]> Download(string fileName, bool signedDocument);
    Task<string> GetPresignedUrl(string fileName, Dictionary<string, string> field, bool signedDocument);
    Task<byte[]> DownloadFolderAsZip(string folderName);
}