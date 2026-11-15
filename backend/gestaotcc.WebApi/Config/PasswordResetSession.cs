using System.Security.Cryptography;
using System.Text;
using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Entities.User;
using Microsoft.AspNetCore.DataProtection;

namespace gestaotcc.WebApi.Config;

public sealed class PasswordResetSession(IDataProtectionProvider provider, IUserGateway users, IWebHostEnvironment environment)
{
    public const string CookieName = "gradus-password-reset";
    private readonly ITimeLimitedDataProtector protector = provider
        .CreateProtector("Gradus.PasswordReset.v1").ToTimeLimitedDataProtector();

    private static string Fingerprint(UserEntity user) => Convert.ToHexString(SHA256.HashData(
        Encoding.UTF8.GetBytes($"{user.Id}:{user.Password}:{user.AccessCode.Id}:{user.AccessCode.Code}")));

    private static bool Eligible(UserEntity? user) => user is not null && user.Status != "INACTIVE" &&
        user.AccessCode is { IsActive: false, IsUserUpdatePassword: false } &&
        user.AccessCode.ExpirationDate > DateTime.UtcNow;

    private CookieOptions Options(HttpContext context) => new()
    {
        HttpOnly = true, Secure = !environment.IsDevelopment() || context.Request.IsHttps,
        SameSite = SameSiteMode.Strict, Path = context.Request.PathBase.Value + "/api/Auth/update-password",
        MaxAge = TimeSpan.FromMinutes(5), IsEssential = true
    };

    public async Task Issue(HttpContext context, string email)
    {
        var user = await users.FindByEmail(email);
        if (!Eligible(user)) return;
        var expires = user!.AccessCode.ExpirationDate < DateTime.UtcNow.AddMinutes(5)
            ? user.AccessCode.ExpirationDate : DateTime.UtcNow.AddMinutes(5);
        var token = protector.Protect(Fingerprint(user), new DateTimeOffset(DateTime.SpecifyKind(expires, DateTimeKind.Utc)));
        context.Response.Cookies.Append(CookieName, token, Options(context));
    }

    public async Task<bool> Validate(HttpContext context, string email)
    {
        if (!context.Request.Cookies.TryGetValue(CookieName, out var token) || token.Length > 4096) return false;
        var user = await users.FindByEmail(email);
        if (!Eligible(user)) return false;
        try
        {
            var actual = protector.Unprotect(token, out _);
            return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(actual),
                Encoding.UTF8.GetBytes(Fingerprint(user!)));
        }
        catch (CryptographicException) { return false; }
    }

    public void Clear(HttpContext context) => context.Response.Cookies.Delete(CookieName, Options(context));
}
