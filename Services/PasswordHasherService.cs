using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace ProjektM.Services;

public class PasswordHasherService
{
    public string HashPassword(string password)
    {
        byte[] salt = new byte[16];
        using (var range = RandomNumberGenerator.Create())
        {
            range.GetBytes(salt);
        }

        var hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
            password: password,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: 10000,
            numBytesRequested: 32
        ));

        return $"{Convert.ToBase64String(salt)}:{hashed}";
    }

    public bool VerifyPassword(string hashedPassword, string inputPassword)
    {
        var parts = hashedPassword.Split(':');
        if (parts.Length != 2)
            return false;
        
        byte[] salt = Convert.FromBase64String(parts[0]);
        var inputHashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
            password: inputPassword,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: 10000,
            numBytesRequested: 32
        ));

        return parts[1] == inputHashed;
    }
}