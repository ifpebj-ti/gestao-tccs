using gestaotcc.WebApi.Logs;
using Serilog;
using Serilog.Sinks.Grafana.Loki;

namespace gestaotcc.WebApi.Config;

public static class SerilogExtension
{
    public static void AddSerilogExtension(this IHostBuilder hostBuilder)
    {
        
        hostBuilder.UseSerilog((context, services, configuration) =>
        {
            configuration.ReadFrom.Configuration(context.Configuration)
                .MinimumLevel.Information()
                .Enrich.FromLogContext()
                .Filter.ByExcluding(logEvent =>
                    logEvent.Properties.TryGetValue("SourceContext", out var source)
                    && source.ToString().Contains("Microsoft.AspNetCore"))
                .WriteTo.Console(new SimpleJsonLogFormatter());
        });
    }
}