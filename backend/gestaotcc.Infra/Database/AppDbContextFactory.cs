using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace gestaotcc.Infra.Database;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        // Using a dummy connection string just for generating migrations
        optionsBuilder.UseNpgsql("Host=localhost;Database=gestaotcc;Username=postgres;Password=postgres");

        return new AppDbContext(optionsBuilder.Options);
    }
}
