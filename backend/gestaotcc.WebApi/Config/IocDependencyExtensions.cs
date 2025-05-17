using gestaotcc.Application.Gateways;
using gestaotcc.Application.UseCases.AccessCode;
using gestaotcc.Application.UseCases.User;
using gestaotcc.Infra.Gateways;

namespace gestaotcc.WebApi.Config;

public static class IocDependencyExtensions
{
    public static void AddIocDependencies(this IServiceCollection services)
    {
        // Gateways
        services.AddScoped<IEmailGateway, EmailGateway>();
        services.AddScoped<ICourseGateway, CourseGateway>();
        services.AddScoped<IProfileGateway, ProfileGateway>();
        services.AddScoped<IUserGateway, UserGateway>();

        // UseCases
        services.AddScoped<CreateAccessCodeUseCase>();
        services.AddScoped<CreateUserUseCase>();
    }
}