using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;

namespace PortailInterne.Api.Services;

public class PasswordService
{
	private readonly PasswordHasher<object> _hasher = new();

	public string Hash(string password)
	{
		return _hasher.HashPassword(null!, password);
	}

	public bool Verify(string passwordHash, string password)
	{
		var result = _hasher.VerifyHashedPassword(null!, passwordHash, password);
		return result != PasswordVerificationResult.Failed;
	}

    public static string GenerateTemporaryPassword(int length = 12)
    {
        const string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string lower = "abcdefghijklmnopqrstuvwxyz";
        const string digits = "0123456789";
        const string symbols = "!#$%?&*";
        const string all = upper + lower + digits + symbols;

        if (length < 8)
            throw new ArgumentOutOfRangeException(nameof(length));

        var chars = new char[length];

        // 1 caractère garanti par catégorie
        chars[0] = upper[RandomNumberGenerator.GetInt32(upper.Length)];
        chars[1] = lower[RandomNumberGenerator.GetInt32(lower.Length)];
        chars[2] = digits[RandomNumberGenerator.GetInt32(digits.Length)];
        chars[3] = symbols[RandomNumberGenerator.GetInt32(symbols.Length)];

        // le reste depuis l'alphabet complet
        for (int i = 4; i < length; i++)
            chars[i] = all[RandomNumberGenerator.GetInt32(all.Length)];

        // mélange (Fisher-Yates)
        for (int i = chars.Length - 1; i > 0; i--)
        {
            int j = RandomNumberGenerator.GetInt32(i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }

        return new string(chars);
    }
}

