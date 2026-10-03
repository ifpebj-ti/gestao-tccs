using Amazon.Runtime;
using Amazon.S3;

namespace gestaotcc.WebApi.Config;

public static class StorageExtension
{
    public static IServiceCollection AddStorageExtension(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var storageSettings = configuration.GetSection("STORAGE_SETTINGS");
        var endpoint  = storageSettings.GetValue<string>("ENDPOINT")!;
        var accessKey = storageSettings.GetValue<string>("ACCESS_KEY")!;
        var secretKey = storageSettings.GetValue<string>("SECRET_KEY")!;

        var config = new AmazonS3Config
        {
            ServiceURL           = endpoint,
            ForcePathStyle       = true,     // Obrigatório para Garage (não usa Virtual Hosted-Style)
            AuthenticationRegion = "garage", // Deve bater com s3_region no garage.toml
            UseHttp              = !endpoint.StartsWith("https://")
        };

        var credentials = new BasicAWSCredentials(accessKey, secretKey);
        services.AddSingleton<IAmazonS3>(new AmazonS3Client(credentials, config));

        return services;
    }
}