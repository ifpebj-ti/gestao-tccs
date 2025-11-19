using Minio;

namespace gestaotcc.WebApi.Config;

public static class MinioExtension
{
    public static IServiceCollection AddMinioExtension(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var minioSettings = configuration.GetSection("MINIO_SETTINGS");
        var endpoint = minioSettings.GetValue<string>("ENDPOINT");
        var accessKey = minioSettings.GetValue<string>("ACCESS_KEY");
        var secretKey = minioSettings.GetValue<string>("SECRET_KEY");
        
        services.AddMinio(configureClient => 
        {
            configureClient
                .WithEndpoint(endpoint)
                .WithCredentials(accessKey, secretKey)
                .WithSSL(!environment.IsDevelopment());
            
            if (!environment.IsDevelopment())
            {
                configureClient.WithHttpClient(new HttpClient(new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                }));
            }

            configureClient.Build();
        });
        
        return services;
    }
}