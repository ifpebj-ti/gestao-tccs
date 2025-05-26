using System.Security.Cryptography;

namespace gestaotcc.Application.Helpers;
public static class PasswordHelper
{
    private static readonly char[] AvailableChars =
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!@#$%^&*()_+[]{}?".ToCharArray();

    public static string GenerateRandomPassword(int length = 12)
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[length];
        rng.GetBytes(bytes);

        var chars = new char[length];
        for (int i = 0; i < length; i++)
        {
            chars[i] = AvailableChars[bytes[i] % AvailableChars.Length];
        }

        return new string(chars);
    }
}
