using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace gestaotcc.WebApi.Config;

public static class OwnPasswordRateLimitExtensions
{
    public static IServiceCollection AddOwnPasswordRateLimit(this IServiceCollection services) =>
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = (context, _) =>
            {
                context.HttpContext.Response.Headers.RetryAfter = "60";
                return ValueTask.CompletedTask;
            };
            options.AddPolicy("own-password", context => RateLimitPartition.GetFixedWindowLimiter(
                context.User.FindFirst("userId")?.Value ?? "anonymous",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
        });
}
