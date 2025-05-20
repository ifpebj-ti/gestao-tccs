using gestaotcc.Domain.Entities.AccessCode;

namespace gestaotcc.Application.Factories;
public class AccessCodeFactory
{
    public static AccessCodeEntity CreateAccessCodeEntity(string combination)
    {
        var chars = combination;
        var random = new Random();

        var code = new string(Enumerable.Repeat(chars, 6)
            .Select(s => s[random.Next(s.Length)]).ToArray());

        var expiration = DateTime.UtcNow.AddMinutes(5);

        return new AccessCodeEntityBuilder()
            .WithCode(code)
            .WithExpirationDate(expiration)
            .WithIsActive(true)
            .WithIsUserUpdatePassword(false)
            .Build();
    }
}
