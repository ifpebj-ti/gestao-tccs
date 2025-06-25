using gestaotcc.Application.Gateways;
using Microsoft.Extensions.Configuration;
using Minio;
using Minio.DataModel.Args;

namespace gestaotcc.Infra.Gateways;

public class MinioGateway : IMinioGateway
{
    private readonly IConfiguration _configuration;
    private readonly MinioClient _minioClient;
    private readonly string _bucketName;
    
    public MinioGateway(IConfiguration configuration, MinioClient minioClient)
    {
        _configuration = configuration;
        _minioClient = minioClient;
        
        var minioSettings = configuration.GetSection("MINIO_SETTINGS");
        var bucketName = minioSettings.GetValue<string>("BUCKET_NAME");

        _bucketName = bucketName;
    }
    public async Task Send(string fileName, byte[] file, string contentType)
    {
        // Cria o bucket se não existir
        bool bucketExists = await _minioClient.BucketExistsAsync(new BucketExistsArgs().WithBucket(_bucketName));
        if (!bucketExists)
        {
            await _minioClient.MakeBucketAsync(new MakeBucketArgs().WithBucket(_bucketName));
        }
        
        using var byteStream = new MemoryStream(file);
        await _minioClient.PutObjectAsync(new PutObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(fileName)
            .WithStreamData(byteStream)
            .WithObjectSize(byteStream.Length)
            .WithContentType(contentType)
        );
    }

    public async Task<byte[]> Download(string fileName)
    {
        using var ms = new MemoryStream();

        await _minioClient.GetObjectAsync(new GetObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(fileName)
            .WithCallbackStream(stream => stream.CopyTo(ms))
        );

        return ms.ToArray();
    }

    public async Task<string> GetPresignedUrl(string fileName)
    {
        var urlExperiment = 3000;
        
        var args = new PresignedGetObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(fileName)
            .WithExpiry(urlExperiment);

        string url = await _minioClient.PresignedGetObjectAsync(args);

        return url;
    }
}