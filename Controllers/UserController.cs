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
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        if (request.UserId != GetUserIdFromToken())
            return Unauthorized(new { Message = "Unauthorized access." });

        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == request.UserId);
        if (user == null)
            return NotFound(new { Message = "User not found." });

        if (!_passwordHasher.VerifyPassword(user.PasswordHash, request.OldPassword))
            return BadRequest(new { Message = "Invalid current password." });

        user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Password changed successfully." });
    }
    
    [HttpGet("available-products")]
    public async Task<IActionResult> GetAvailableProducts()
    {
        var products = await _context.Products.Where(p => p.Stock > 0).ToListAsync();
        return Ok(products);
    }

    [HttpPost("create-order")]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        if (request.UserId != GetUserIdFromToken())
            return Unauthorized(new { Message = "Unauthorized access." });

        var order = new Order
        {
            UserId = request.UserId,
            OrderDate = DateTime.UtcNow,
            Status = "Pending",
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Order created successfully.", OrderId = order.OrderId });
    }

    [HttpDelete("cancel-order/{orderId}")]
    public async Task<IActionResult> CancelOrder(int orderId)
    {
        var userId = GetUserIdFromToken();

        var order = await _context.Orders.Where(o => o.OrderId == orderId).FirstOrDefaultAsync();
        if (order == null)
            return NotFound(new { Message = "Order not found." });

        if (order.UserId != userId)
            return Unauthorized(new { Message = "You are not authorized to cancel this order." });

        _context.Orders.Remove(order);
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Order canceled successfully." });
    }
}