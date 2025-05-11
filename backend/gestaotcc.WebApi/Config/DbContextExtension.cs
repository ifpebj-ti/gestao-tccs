using gestaotcc.Infra.Database;
using Microsoft.EntityFrameworkCore;

namespace gestaotcc.WebApi.Config;

public static class DbContextExtension
{
    public static IServiceCollection AddDbContextExtension(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"));
        });

        return services;
    }
}