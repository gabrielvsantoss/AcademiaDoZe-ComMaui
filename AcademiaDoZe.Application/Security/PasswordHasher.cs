﻿// Gabriel Velho dos Santos

using Konscious.Security.Cryptography;
using System.Security.Cryptography;
using System.Text;
namespace AcademiaDoZe.Application.Security;

public class PasswordHasher
{
    private const int SaltSize = 16;
    private const int HashSize = 32; 
    private const int DefaultIterations = 3;
    private const int DefaultMemorySizeKb = 64 * 1024; 
    public static string Hash(string password)
    {
        if (string.IsNullOrEmpty(password)) throw new ArgumentException("Password cannot be empty.", nameof(password));
       
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        int p = Math.Max(1, Environment.ProcessorCount);
        var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            DegreeOfParallelism = p,
            MemorySize = DefaultMemorySizeKb, 
            Iterations = DefaultIterations
        };
        byte[] hash = argon2.GetBytes(HashSize);
        
        return $"ARGON2ID:{DefaultIterations}:{DefaultMemorySizeKb}:{p}:{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
    }
    public static bool Verify(string password, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(passwordHash)) return false;
        var parts = passwordHash.Split(':');
        if (parts.Length != 6) return false;
        if (!parts[0].Equals("ARGON2ID", StringComparison.Ordinal)) return false;
        if (!int.TryParse(parts[1], out int t)) return false;
        if (!int.TryParse(parts[2], out int mKb)) return false;
        if (!int.TryParse(parts[3], out int p)) return false;
        byte[] salt;
        byte[] expected;
        try
        {
            salt = Convert.FromBase64String(parts[4]);
            expected = Convert.FromBase64String(parts[5]);
        }
        catch
        {
            return false;
        }
        var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            DegreeOfParallelism = Math.Max(1, p),
            MemorySize = mKb,
            Iterations = Math.Max(1, t)
        };
        byte[] actual = argon2.GetBytes(expected.Length);
     
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}