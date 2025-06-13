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
            var environment = context.HostingEnvironment.EnvironmentName ?? "unknown";
            
            configuration.ReadFrom.Configuration(context.Configuration)
                .Enrich.FromLogContext()
                .WriteTo.Console(new SimpleJsonLogFormatter())
                .WriteTo.GrafanaLoki("http://loki:3100", labels: new[]
                {
                    new LokiLabel { Key = "app", Value = "gestao-backend" },
                    new LokiLabel { Key = "env", Value = environment }
                });
        });
    }
}