using gestaotcc.Domain.Entities.AccessCode;

using System.Security.Cryptography;
namespace gestaotcc.Application.Factories;
public class AccessCodeFactory
{
    public static AccessCodeEntity CreateAccessCodeEntity(string combination)
    {
        // A configurable alphabet must not silently reduce the code's entropy.
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

        var code = new string(Enumerable.Repeat(chars, 6)
            .Select(s => s[RandomNumberGenerator.GetInt32(s.Length)]).ToArray());

        var expiration = DateTime.UtcNow.AddMinutes(5);

        return new AccessCodeEntityBuilder()
            .WithCode(code)
            .WithExpirationDate(expiration)
            .WithIsActive(true)
            .WithIsUserUpdatePassword(false)
            .Build();
    }
}
