using gestaotcc.WebApi.Logs;
using Serilog;
using Serilog.Enrichers.OpenTelemetry;
using Serilog.Sinks.Grafana.Loki;

namespace gestaotcc.WebApi.Config;

public static class SerilogExtension
{
    public static void AddSerilogExtension(this IHostBuilder hostBuilder, IConfiguration _configuration)
    {
        var urlLoki = _configuration.GetValue<string>("URL_LOKI");
        
        hostBuilder.UseSerilog((context, services, configuration) =>
        {
            configuration.ReadFrom.Configuration(context.Configuration)
                .MinimumLevel.Information()
                .Enrich.FromLogContext()
                .Enrich.WithOpenTelemetrySpanId()
                .Enrich.WithOpenTelemetryTraceId()
                .Filter.ByExcluding(logEvent =>
                    logEvent.Properties.TryGetValue("SourceContext", out var source) &&
                    (
                        source.ToString().Contains("Microsoft.AspNetCore.StaticFiles") || // arquivos estáticos
                        source.ToString()
                            .Contains("Microsoft.AspNetCore.Routing.EndpointMiddleware") || // roteamento básico
                        source.ToString().Contains("Microsoft.AspNetCore.Server.Kestrel") // conexões HTTP/TCP
                    ))
                .WriteTo.GrafanaLoki(
                    urlLoki!,
                    labels: new[]
                    {
                        new LokiLabel { Key = "app", Value = "gestaotcc-api" },
                        new LokiLabel { Key = "env", Value = context.HostingEnvironment.EnvironmentName },
                    })
                .WriteTo.Console();
        });
    }
}