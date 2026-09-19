namespace gestaotcc.WebApi.Config;

public static class CorsExtension
{
    public static IServiceCollection AddCorsExtension(this IServiceCollection services, IConfiguration configuration)
    {
        var corsSettings = configuration.GetSection("CORS_SETTINGS");
        var urlFront = corsSettings.GetValue<string>("URL_FRONT");
        
        var allowedOrigins = new List<string> { "https://gestao-tcc.local", "https://localhost" };
        if (!string.IsNullOrEmpty(urlFront) && !allowedOrigins.Contains(urlFront))
        {
            allowedOrigins.Add(urlFront);
        }

        services.AddCors(options =>
        {
            options.AddPolicy(name: "CorsPolicy",
                policy =>
                {
                    policy.WithOrigins(allowedOrigins.ToArray())
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
        });

        return services;
    }
}