using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Exporter;
using OpenTelemetry.Trace;

namespace gestaotcc.WebApi.Config;

public static class OpenTelemetryExtension
{
    public static void AddOpenTelemetryExtension(this IServiceCollection services, IWebHostEnvironment environment, IConfiguration configuration)
    {
        var urlTempo = configuration.GetValue<string>("URL_TEMPO");
        
        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(
                    serviceName: "gestaotcc-api",
                    serviceVersion: "1.0.0"
                )
                .AddAttributes(new Dictionary<string, object>
                {
                    { "deployment_environment", environment.EnvironmentName }
                })
            )
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddProcessInstrumentation()
                .AddRuntimeInstrumentation()
                .AddMeter("Microsoft.AspNetCore.Hosting")
                .AddMeter("Microsoft.AspNetCore.Server.Kestrel")
                .AddMeter("System.Net.Http")
                .AddMeter("System.Net.NameResolution")
                .AddPrometheusExporter())
            .WithTracing(tracing => tracing
                .AddSource("gestaotcc-api") 
                .AddAspNetCoreInstrumentation() 
                .AddHttpClientInstrumentation()
                .AddEntityFrameworkCoreInstrumentation()
                .AddOtlpExporter(opt =>
                {
                    opt.Endpoint = new Uri(urlTempo);
                    opt.Protocol = OtlpExportProtocol.HttpProtobuf;
                })
            );
    }
}