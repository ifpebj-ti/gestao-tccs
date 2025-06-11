using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;

namespace gestaotcc.WebApi.Config;

public static class OpenTelemetryExtension
{
    public static void AddOpenTelemetryExtension(this IServiceCollection services, IWebHostEnvironment environment)
    {
        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(serviceName: environment.EnvironmentName))
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddMeter("Microsoft.AspNetCore.Hosting")
                .AddMeter("Microsoft.AspNetCore.Server.Kestrel")
                .AddMeter("System.Net.Http")
                .AddMeter("System.Net.NameResolution")
                .AddPrometheusExporter());
    }
}