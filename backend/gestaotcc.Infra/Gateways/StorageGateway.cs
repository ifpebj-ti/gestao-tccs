using Amazon.S3;
using Amazon.S3.Model;
using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.Signature;
using Microsoft.Extensions.Configuration;
using System.IO.Compression;

namespace gestaotcc.Infra.Gateways;

public class StorageGateway : IStorageGateway
{
    private readonly IAmazonS3 _s3Client;
    private readonly IITextGateway _iTextGateway; // Mantido para não quebrar a Injeção de Dependência
    private readonly string _bucketName;

    public StorageGateway(IAmazonS3 s3Client, IConfiguration configuration, IITextGateway textGateway)
    {
        _s3Client = s3Client;
        _iTextGateway = textGateway;

        var storageSettings = configuration.GetSection("STORAGE_SETTINGS");
        _bucketName = storageSettings.GetValue<string>("BUCKET_NAME")!;
    }

    public async Task Send(string fileName, byte[] file, string contentType, bool isFilledPdfProcess = false)
    {
        await EnsureBucketExistsAsync();

        var objectName = isFilledPdfProcess ? fileName : $"signatures/{fileName}.{contentType.Split("/")[1]}";

        using var byteStream = new MemoryStream(file);
        var request = new PutObjectRequest
        {
            BucketName  = _bucketName,
            Key         = objectName,
            InputStream = byteStream,
            ContentType = contentType,
            // Garage não suporta chunked upload (STREAMING-AWS4-HMAC-SHA256-PAYLOAD).
            // UseChunkEncoding=false força o SDK a calcular e enviar o SHA256 completo no header.
            UseChunkEncoding                 = false,
            DisableDefaultChecksumValidation = true
        };

        await _s3Client.PutObjectAsync(request);
    }

    public async Task<byte[]> Download(string fileName, bool signedDocument = false)
    {
        string objectName;

        if (fileName.StartsWith("filled/"))
        {
            objectName = fileName;
        }
        else
        {
            objectName = signedDocument ? $"signatures/{fileName}" : $"templates/{fileName}";
        }

        var request = new GetObjectRequest
        {
            BucketName = _bucketName,
            Key = objectName
        };

        try
        {
            using var response = await _s3Client.GetObjectAsync(request);
            using var ms = new MemoryStream();
            await response.ResponseStream.CopyToAsync(ms);
            return ms.ToArray();
        }
        catch (AmazonS3Exception ex) when (ex.ErrorCode is "NoSuchKey" or "404" || ex.Message.Contains("Key not found"))
        {
            // Arquivo não encontrado no storage (ex: migração do MinIO, arquivo antigo)
            return Array.Empty<byte>();
        }
    }

    public async Task<string> GetDocumentAsBase64(string fileName, Dictionary<string, string> fields, bool signedDocument = false)
    {
        // O processo de preenchimento agora ocorre via HTML/Scriban no FindDocumentUseCase!
        // Este método mantém apenas a responsabilidade de baixar e converter para Base64.
        var objectName = signedDocument ? $"signatures/{fileName}" : fileName;

        if (!signedDocument && !fileName.StartsWith("filled/"))
        {
            objectName = $"templates/{fileName}";
        }

        var file = await Download(objectName, signedDocument);
        return Convert.ToBase64String(file);
    }

    public async Task<byte[]> DownloadFolderAsZip(string folderName)
    {
        var zipStream = new MemoryStream();

        using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, true))
        {
            var listRequest = new ListObjectsV2Request
            {
                BucketName = _bucketName,
                Prefix = $"signatures/{folderName}/"
            };

            ListObjectsV2Response listResponse;
            do
            {
                listResponse = await _s3Client.ListObjectsV2Async(listRequest);

                foreach (var obj in listResponse.S3Objects)
                {
                    using var fileStream = new MemoryStream();

                    var getRequest = new GetObjectRequest
                    {
                        BucketName = _bucketName,
                        Key = obj.Key
                    };

                    using var getResponse = await _s3Client.GetObjectAsync(getRequest);
                    await getResponse.ResponseStream.CopyToAsync(fileStream);
                    fileStream.Position = 0;

                    string fileNameInZip = obj.Key.Replace($"signatures/{folderName}/", "");
                    var entry = archive.CreateEntry(fileNameInZip);

                    using var entryStream = entry.Open();
                    fileStream.CopyTo(entryStream);
                }

                listRequest.ContinuationToken = listResponse.NextContinuationToken;
            }
            while (listResponse.IsTruncated);
        }

        zipStream.Position = 0;
        return zipStream.ToArray();
    }

#pragma warning disable CS1998 // Desabilitando aviso de método assíncrono para manter o padrão original
    public async Task<IAsyncEnumerable<StorageObjectDto>> ListBuckets(string folderName)
    {
        return ListObjectsAsync(folderName);
    }
#pragma warning restore CS1998

    public async Task Remove(string objectName)
    {
        var request = new DeleteObjectRequest
        {
            BucketName = _bucketName,
            Key = objectName
        };

        await _s3Client.DeleteObjectAsync(request);
    }

    private async IAsyncEnumerable<StorageObjectDto> ListObjectsAsync(string folderName)
    {
        var listRequest = new ListObjectsV2Request
        {
            BucketName = _bucketName,
            Prefix = folderName
        };

        ListObjectsV2Response listResponse;
        do
        {
            listResponse = await _s3Client.ListObjectsV2Async(listRequest);

            foreach (var obj in listResponse.S3Objects)
            {
                yield return new StorageObjectDto(
                    obj.Key,
                    (ulong)obj.Size,
                    obj.LastModified.ToString("o")
                );
            }

            listRequest.ContinuationToken = listResponse.NextContinuationToken;
        }
        while (listResponse.IsTruncated);
    }

    private async Task EnsureBucketExistsAsync()
    {
        var bucketsResponse = await _s3Client.ListBucketsAsync();
        bool bucketExists = bucketsResponse.Buckets.Any(b => b.BucketName == _bucketName);

        if (!bucketExists)
        {
            await _s3Client.PutBucketAsync(new PutBucketRequest { BucketName = _bucketName });
        }
    }
}