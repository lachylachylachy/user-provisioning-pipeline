using System.Security.Cryptography;

namespace EntraFlow.Core.Graph;

/// <summary>
/// Generates strong temporary passwords for newly created users. Uses a
/// cryptographic RNG and guarantees at least one character from each class so the
/// result satisfies Entra's default password complexity policy.
/// </summary>
public static class PasswordGenerator
{
    private const string Lower = "abcdefghijkmnpqrstuvwxyz";
    private const string Upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const string Digits = "23456789";
    private const string Symbols = "!@#$%^&*-_=+";
    private const string All = Lower + Upper + Digits + Symbols;

    public static string Generate(int length = 16)
    {
        if (length < 8)
        {
            length = 8;
        }

        Span<char> chars = stackalloc char[length];
        chars[0] = Pick(Lower);
        chars[1] = Pick(Upper);
        chars[2] = Pick(Digits);
        chars[3] = Pick(Symbols);

        for (var i = 4; i < length; i++)
        {
            chars[i] = Pick(All);
        }

        // Fisher–Yates shuffle so the guaranteed classes aren't always up front.
        for (var i = length - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }

        return new string(chars);
    }

    private static char Pick(string set) => set[RandomNumberGenerator.GetInt32(set.Length)];
}
