using gestaotcc.Application.UseCases.Signature;
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
        // Background jobs remain active. The dashboard is not exposed by the application.

        using var scope = serviceProvider.CreateScope();
        var recurringJobs = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();

        recurringJobs.AddOrUpdate<ResendInvitationTccEmailUseCase>(
            "resend-invite-tcc",
            useCase => useCase.Execute(),
            "0 5 * * *", new RecurringJobOptions() // Executar diariamente às 5:00
        );
        
        recurringJobs.AddOrUpdate<SendPendingSignatureUseCase>(
            "send-pending-signatures",
            useCase => useCase.Execute(0),
            "0 5 * * *", new RecurringJobOptions() // Executar diariamente às 5:00
        );
        
        recurringJobs.AddOrUpdate<RemoveFilledPdfsUseCase>(
            "remove-filled-pdfs",
            useCase => useCase.Execute(),
            "0 5 * * *", new RecurringJobOptions() // Executar diariamente às 5:00
        );

        return app;
    }
}
