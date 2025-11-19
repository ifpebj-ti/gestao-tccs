using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.Signature;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Minio;
using Minio.ApiEndpoints;
using Minio.DataModel;
using Minio.DataModel.Args;

namespace gestaotcc.Infra.Gateways;

public class MinioGateway : IMinioGateway
{
    private readonly IConfiguration _configuration;
    private readonly IMinioClient _minioClient;
    private readonly IITextGateway _iTextGateway;
    private readonly string _bucketName;
    private readonly string _env;
    private readonly string? _publicDomain;
    private readonly string? _endpoint;

    public MinioGateway(IConfiguration configuration, IMinioClient minioClient, IITextGateway textGateway)
    {
        _configuration = configuration;
        _minioClient = minioClient;
        _iTextGateway = textGateway;

        var minioSettings = _configuration.GetSection("MINIO_SETTINGS");
        _bucketName = minioSettings.GetValue<string>("BUCKET_NAME")!;
        _publicDomain = minioSettings.GetValue<string>("DOMAIN");
        _endpoint = minioSettings.GetValue<string>("ENDPOINT");
        _env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
        
    }

    public async Task Send(string fileName, byte[] file, string contentType, bool isFilledPdfProcess = false)
    {
        bool bucketExists = await _minioClient.BucketExistsAsync(new BucketExistsArgs().WithBucket(_bucketName));
        if (!bucketExists)
        {
            await _minioClient.MakeBucketAsync(new MakeBucketArgs().WithBucket(_bucketName));
        }
        
        var objectName = isFilledPdfProcess ? fileName : $"signatures/{fileName}.{contentType.Split("/")[1]}";

        using var byteStream = new MemoryStream(file);
        await _minioClient.PutObjectAsync(new PutObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(objectName)
            .WithStreamData(byteStream)
            .WithObjectSize(byteStream.Length)
            .WithContentType(contentType)
        );
    }

    public async Task<byte[]> Download(string fileName, bool signedDocument = false)
    {
        using var ms = new MemoryStream();

        var objectName = signedDocument ? $"signatures/{fileName}" : $"templates/{fileName}";

        await _minioClient.GetObjectAsync(new GetObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(objectName)
            .WithCallbackStream(stream => stream.CopyTo(ms))
        );

        return ms.ToArray();
    }

    public async Task<string> GetPresignedUrl(string fileName, Dictionary<string, string> fields, bool signedDocument = false)
    {
        var objectName = signedDocument ? $"signatures/{fileName}" : $"templates/{fileName}";

        if (!signedDocument)
        {
            var fileTemplate = await Download(fileName, signedDocument);
            var fileStream = new MemoryStream(fileTemplate);
            fileStream.Position = 0;

            var outputStream = await _iTextGateway.FillPdf(fields ,fileStream);
            var fileByte = outputStream.ToArray();
            
            objectName = $"filled/{Guid.NewGuid()}_{DateTime.Now:dd-MM-yyyy}_{fileName}";
            
            await Send(objectName, fileByte, "application/pdf", true);
        }

        var args = new PresignedGetObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(objectName)
            .WithExpiry(120);

        var url = await _minioClient.PresignedGetObjectAsync(args);
        url = url.Replace($"http://{_endpoint}", _publicDomain);
        return url;
    }

    public async Task<byte[]> DownloadFolderAsZip(string folderName)
    {
        var zipStream = new MemoryStream();

        using (var archive = new System.IO.Compression.ZipArchive(zipStream, System.IO.Compression.ZipArchiveMode.Create, true))
        {
            var listArgs = new ListObjectsArgs()
                .WithBucket(_bucketName)
                .WithPrefix($"signatures/{folderName}/")
                .WithRecursive(true);

            await foreach (var item in _minioClient.ListObjectsEnumAsync(listArgs))
            {
                using var fileStream = new MemoryStream();

                var getObjectArgs = new GetObjectArgs()
                    .WithBucket(_bucketName)
                    .WithObject(item.Key)
                    .WithCallbackStream(stream => stream.CopyTo(fileStream));

                await _minioClient.GetObjectAsync(getObjectArgs);
                fileStream.Position = 0;

                string fileNameInZip = item.Key.Replace($"signatures/{folderName}/", "");
                var entry = archive.CreateEntry(fileNameInZip);

                using var entryStream = entry.Open();
                fileStream.CopyTo(entryStream);
            }
        }

        zipStream.Position = 0;
        return zipStream.ToArray();
    }

    public async Task<IAsyncEnumerable<StorageObjectDto>> ListBuckets(string folderName)
    {
        return MapObjectsAsync(_minioClient.ListObjectsEnumAsync(
            new ListObjectsArgs()
                .WithBucket(_bucketName)
                .WithPrefix(folderName)
                .WithRecursive(true)
        ));
    }

    public async Task Remove(string objectName)
    {
        await _minioClient.RemoveObjectAsync(
            new RemoveObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(objectName)
        );
    }

    private async IAsyncEnumerable<StorageObjectDto> MapObjectsAsync(IAsyncEnumerable<Item> items)
    {
        await foreach (var item in items)
        {
            yield return new StorageObjectDto(
                item.Key,
                item.Size,
                item.LastModified
            );
        }
    }
}
