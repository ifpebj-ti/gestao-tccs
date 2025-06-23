using gestaotcc.Domain.Entities.Signature;
using gestaotcc.Domain.Entities.User;

namespace gestaotcc.Application.Factories;

public class SignatureFactory
{
    public static SignatureEntity CreateSignature(UserEntity user)
    {
        return new SignatureEntityBuilder()
            .WithUser(user)
            .WithSignatureDate(DateTime.UtcNow)
            .Build();
    }
}