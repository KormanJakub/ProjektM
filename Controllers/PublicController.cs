using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjektM.Models;
using ProjektM.Requests;
using ProjektM.Services;

namespace ProjektM.Controllers;

[ApiController]
[Route("[controller]")]
public class PublicController : ControllerBase
{
    private readonly PasswordHasherService _passwordHasher;
    private readonly PmiContext _context;
    private readonly GenerateResetTokenService _token;

    public PublicController(PmiContext context, PasswordHasherService passwordHasher, GenerateResetTokenService token)
    {
        _passwordHasher = passwordHasher;
        _context = context;
        _token = token;
    }
    
    [HttpPost("register")]
    public async Task<IActionResult>  Register([FromBody] RegisterRequest model)
    {
        if (await _context.Users.AnyAsync(u => u.Email == model.Email || u.Username == model.Username))
            return BadRequest(new { Message = "User with the same email or username already exists." });
        
        var hashedPassword = _passwordHasher.HashPassword(model.Password);
        
        int newUserId = 1; 
        if (await _context.Users.AnyAsync())
            newUserId = await _context.Users.MaxAsync(u => u.UserId) + 1;

        var user = new User()
        {
            UserId = newUserId,
            Username = model.Username,
            Email = model.Email,
            PasswordHash = hashedPassword,
            IsAdmin = false,
            CreatedAt = DateTime.Now
        };
        
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return Ok(new { Message = "User registered successfully", User = user });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest model)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == model.Username);
        if (user == null)
            return Unauthorized(new { Message = "Invalid credentials" });

        if (!_passwordHasher.VerifyPassword(user.PasswordHash, model.Password)) 
            return Unauthorized(new { Message = "Invalid credentials" });

        return Ok(new { Message = "Login successful" });
    }

    [HttpPost("forget-password")]
    public async Task<IActionResult> ForgetPassword([FromBody] ForgetPasswordRequest model)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
        if (user == null)
            return BadRequest(new { Message = "User with this email does not exist." });

        var resetToken = _token.GenerateResetToken(user.UserId, user.Email);

        return Ok(new { Message = "Password reset email sent", UserId = user.UserId, ResetToken = resetToken });
    }
    
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest model)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == model.UserId);
        if (user == null)
            return BadRequest(new { Message = "Invalid user ID." });

        var expectedToken = _token.GenerateResetToken(user.UserId, user.Email);
        if (expectedToken != model.ResetToken) 
            return BadRequest(new { Message = "Invalid reset token." });

        user.PasswordHash = _passwordHasher.HashPassword(model.NewPassword);
        
        _context.Users.Update(user);
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Password reset successful." });
    }
}