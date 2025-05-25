using gestaotcc.Application.UseCases.Tcc;
using Hangfire;
using Hangfire.PostgreSql;

namespace gestaotcc.WebApi.Config;

public static class HangfireExtension
{
    public static IServiceCollection AddHangfireExtension(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHangfire(config =>
            config.UsePostgreSqlStorage(configuration.GetConnectionString("DefaultConnection")));
        
        services.AddHangfireServer();
        
        return services;
    }
    
    public static IApplicationBuilder UseHangfireExtension(this IApplicationBuilder app, IServiceProvider serviceProvider)
    {
        app.UseHangfireDashboard();

        using var scope = serviceProvider.CreateScope();
        var resendInvitationUseCase = scope.ServiceProvider.GetRequiredService<ResendInvitationTccEmailUseCase>();

        RecurringJob.AddOrUpdate(
            "resend-invite-tcc",
            () => resendInvitationUseCase.Execute(),
            "0 5 * * *" // Executar diariamente às 5:00
        );

        return app;
    }
}