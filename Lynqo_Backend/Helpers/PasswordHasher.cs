using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

public static class PasswordHasher
{
    public static string HashPassword(string password)
    {
        // Use salt/random bytes and your preferred hashing method
        // This is a simplified example only!
        using var sha = SHA256.Create();
        return Convert.ToBase64String(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password)));
    }
    public static bool VerifyPassword(string hash, string password) =>
        HashPassword(password) == hash;
}
