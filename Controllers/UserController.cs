using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjektM.Models;
using ProjektM.Requests;
using ProjektM.Services;

namespace ProjektM.Controllers;

[ApiController]
[Route("[controller]")]
public class UserController : ControllerBase
{
    private readonly PmiContext _context;
    private readonly PasswordHasherService _passwordHasher;
    private readonly GenerateResetTokenService _tokenService;

    public UserController(PmiContext context, PasswordHasherService passwordHasher, GenerateResetTokenService tokenService)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }
    
    private int GetUserIdFromToken()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid or missing UserId in token.");
        }

        return userId;
    }
    
    [HttpGet("current-user")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = GetUserIdFromToken();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);

        if (user == null)
        {
            return NotFound(new { Message = "User not found." });
        }

        return Ok(new { user.UserId, user.Username, user.Email });
    }
    
    [HttpPut("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] string token)
    {
        try
        {
            var claimsPrincipal = _tokenService.ValidateToken(token);

            var userId = int.Parse(claimsPrincipal.FindFirst("userId")?.Value ?? throw new UnauthorizedAccessException("Invalid token."));
            var oldPassword = claimsPrincipal.FindFirst("oldPassword")?.Value;
            var newPassword = claimsPrincipal.FindFirst("newPassword")?.Value;

            if (string.IsNullOrEmpty(oldPassword) || string.IsNullOrEmpty(newPassword))
            {
                return BadRequest(new { Message = "Invalid token payload." });
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null)
            {
                return NotFound(new { Message = "User not found." });
            }

            if (!_passwordHasher.VerifyPassword(user.PasswordHash, oldPassword))
            {
                return BadRequest(new { Message = "Invalid current password." });
            }

            user.PasswordHash = _passwordHasher.HashPassword(newPassword);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Password changed successfully." });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { Message = ex.Message });
        }
    }
    
    [HttpGet("available-products")]
    public async Task<IActionResult> GetAvailableProducts()
    {
        var products = await _context.Products.Where(p => p.Stock > 0).ToListAsync();
        return Ok(products);
    }

    [HttpPost("create-order")]
    public async Task<IActionResult> CreateOrder([FromBody] string token)
    {
        try
        {
            var claimsPrincipal = _tokenService.ValidateToken(token);
            
            var userId = int.Parse(claimsPrincipal.FindFirst("userId")?.Value ?? throw new UnauthorizedAccessException("Invalid token."));
            var items = claimsPrincipal.FindFirst("items")?.Value;

            if (string.IsNullOrEmpty(items))
            {
                return BadRequest(new { Message = "Invalid token payload." });
            }

            var order = new Order
            {
                UserId = userId,
                OrderDate = DateTime.UtcNow,
                Status = "Pending"
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Order created successfully.", OrderId = order.OrderId });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { Message = ex.Message });
        }
    }

    [HttpDelete("cancel-order")]
    public async Task<IActionResult> CancelOrder([FromBody] string token)
    {
        try
        {
            var claimsPrincipal = _tokenService.ValidateToken(token);

            var userId = int.Parse(claimsPrincipal.FindFirst("userId")?.Value ?? throw new UnauthorizedAccessException("Invalid token."));
            var orderId = int.Parse(claimsPrincipal.FindFirst("orderId")?.Value ?? throw new UnauthorizedAccessException("Invalid token."));

            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
            {
                return NotFound(new { Message = "Order not found." });
            }

            if (order.UserId != userId)
            {
                return BadRequest(new { Message = "You are not authorized to cancel this order." });
            }

            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Order canceled successfully." });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { Message = ex.Message });
        }
    }
}