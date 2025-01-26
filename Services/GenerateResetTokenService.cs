using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ProjektM.Settings;

namespace ProjektM.Services;

public class GenerateResetTokenService
{
    private readonly string _secretKey;
    private readonly string _encryptionKey;
    
    public GenerateResetTokenService(IOptions<JwtSettings> jwtSettings)
    {
        _secretKey = jwtSettings.Value.UserSecretKey;
        _encryptionKey = jwtSettings.Value.UserEncryptionKey;
    }
    
    public string GenerateResetToken(int userId, string email)
    {
        var secretKey = "f3R$g7@9a*Pq^2#LmZx$Vw!y5T8nB&K"; 
        var data = $"{userId}:{email}:{DateTime.UtcNow.Date}";
    
        using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey)))
        {
            byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return Convert.ToBase64String(hash);
        }
    }
    
    public ClaimsPrincipal ValidateToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_secretKey);

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = false,
            ValidateAudience = false,
            ClockSkew = TimeSpan.Zero
        };

        var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);

        if (!(validatedToken is JwtSecurityToken))
        {
            throw new SecurityTokenException("Invalid token");
        }

        return principal;
    }
    
    public string GenerateResponseToken(object responseData)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_encryptionKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Claims = new Dictionary<string, object>
            {
                { "data", System.Text.Json.JsonSerializer.Serialize(responseData) }
            },
            Expires = DateTime.UtcNow.AddMinutes(10),
            SigningCredentials = creds
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}