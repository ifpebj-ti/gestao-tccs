namespace gestaotcc.WebApi.Config;

public static class CorsExtension
{
    public static IServiceCollection AddCorsExtension(this IServiceCollection services, IConfiguration configuration)
    {
        var corsSettings = configuration.GetSection("CorsSettings");
        var urlFront = corsSettings.GetValue<string>("URL_FRONT");
        
        services.AddCors(options =>
        {
            options.AddPolicy(name: "CorsPolicy",
                options =>
                {
                    options.WithOrigins(urlFront!)
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
        });

        return services;
    }
}