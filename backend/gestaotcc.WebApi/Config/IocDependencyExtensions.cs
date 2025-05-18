using gestaotcc.Application.Gateways;
using gestaotcc.Application.UseCases.AccessCode;
using gestaotcc.Application.UseCases.Auth;
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
        services.AddScoped<IBcryptGateway, BcryptGateway>();
        services.AddScoped<ITokenGateway, TokenGateway>();

        //AccessCode
        services.AddScoped<CreateAccessCodeUseCase>();
        services.AddScoped<VerifyAccessCodeUseCase>();
        services.AddScoped<ResendAccessCodeUseCase>();

        //Auth
        services.AddScoped<LoginUseCase>();
        services.AddScoped<UpdatePasswordUseCase>();

        //User
        services.AddScoped<CreateUserUseCase>();
        services.AddScoped<FindUserByEmailUseCase>();
    }
}