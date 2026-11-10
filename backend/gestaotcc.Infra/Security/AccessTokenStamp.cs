using System.Security.Cryptography;
using System.Text;

namespace gestaotcc.Infra.Security;

public static class AccessTokenStamp
{
    public static string Create(long userId, string passwordHash, string signingKey) =>
        Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(signingKey),
            Encoding.UTF8.GetBytes($"gradus-session-v1:{userId}:{passwordHash}")));
}
