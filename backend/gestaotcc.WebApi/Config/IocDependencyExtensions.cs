using gestaotcc.Application.Gateways;
using gestaotcc.Infra.Gateways;

namespace gestaotcc.WebApi.Config;

public static class IocDependencyExtensions
{
    public static void AddIocDependencies(this IServiceCollection services)
    {
        // Outros servicos
        services.AddScoped<IEmailGateway, EmailGateway>();
    }
}