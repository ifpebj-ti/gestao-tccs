using gestaotcc.Application.UseCases.Signature;
using gestaotcc.Application.UseCases.Tcc;
using gestaotcc.WebApi.Config;
using Hangfire;
using Hangfire.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace gestaotcc.Test.Integration;

public class RecurringJobRegistrationTests
{
    [Fact]
    public void StartupRegistersTypedJobsUsingTheScopedManagerWithoutStaticStorage()
    {
        var jobs = Substitute.For<IRecurringJobManager>();
        var services = new ServiceCollection(); services.AddSingleton(jobs);
        using var provider = services.BuildServiceProvider();
        var application = new ApplicationBuilder(provider);
        Assert.Same(application, application.UseHangfireExtension(provider));
        jobs.Received(1).AddOrUpdate("resend-invite-tcc",
            Arg.Is<Job>(job => job.Type == typeof(ResendInvitationTccEmailUseCase) && job.Method.Name == "Execute"),
            "0 5 * * *", Arg.Any<RecurringJobOptions>());
        jobs.Received(1).AddOrUpdate("send-pending-signatures",
            Arg.Is<Job>(job => job.Type == typeof(SendPendingSignatureUseCase) && job.Method.Name == "Execute" && Convert.ToInt64(job.Args[0]) == 0),
            "0 5 * * *", Arg.Any<RecurringJobOptions>());
        jobs.Received(1).AddOrUpdate("remove-filled-pdfs",
            Arg.Is<Job>(job => job.Type == typeof(RemoveFilledPdfsUseCase) && job.Method.Name == "Execute"),
            "0 5 * * *", Arg.Any<RecurringJobOptions>());
        Assert.Equal(3, jobs.ReceivedCalls().Count());
    }
}
