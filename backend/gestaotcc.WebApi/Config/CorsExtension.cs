namespace gestaotcc.WebApi.Config;

public static class CorsExtension
{
    public static IServiceCollection AddCorsExtension(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var corsSettings = configuration.GetSection("CORS_SETTINGS");
        var urlFront = corsSettings.GetValue<string>("URL_FRONT");
        
        var allowedOrigins = environment.IsDevelopment()
            ? new List<string> { "http://localhost:3000", "http://127.0.0.1:3000" }
            : new List<string>();
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
                        .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS")
                        .AllowCredentials();
                });
        });

        return services;
    }
}
