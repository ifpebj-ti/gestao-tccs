using gestaotcc.Domain.Dtos.Signature;

namespace gestaotcc.Application.Gateways;

public interface IMinioGateway
{
    Task Send(string fileName, byte[] file, string contentType, bool isFilledPdfProcess = false);
    Task<byte[]> Download(string fileName, bool signedDocument);
    Task<string> GetDocumentAsBase64(string fileName, Dictionary<string, string> field, bool signedDocument);
    Task<byte[]> DownloadFolderAsZip(string folderName);
    Task<IAsyncEnumerable<StorageObjectDto>> ListBuckets(string folderName);
    Task Remove(string objectName);
}