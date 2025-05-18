using gestaotcc.WebApi.Logs;
using Serilog;

namespace gestaotcc.WebApi.Config;

public static class SerilogExtension
{
    public static void AddSerilogExtension(this IHostBuilder hostBuilder)
    {
        hostBuilder.UseSerilog((context, services, configuration) =>
        {
            configuration.ReadFrom.Configuration(context.Configuration)
                .Enrich.FromLogContext()
                .WriteTo.Console(new SimpleJsonLogFormatter())
                .WriteTo.File(new SimpleJsonLogFormatter(), "Logs/log-.txt", rollingInterval: RollingInterval.Day, rollOnFileSizeLimit: true);
        });
    }
}