namespace gestaotcc.WebApi.Config;

public static class CorsExtension
{
    public static IServiceCollection AddCorsExtension(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy(name: "CorsPolicy",
                options =>
                {
                    options.WithOrigins("http://localhost:3000", "http://gestao-frontend:3000", "http://4.201.204.229:3000")
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
        });

        return services;
    }
}