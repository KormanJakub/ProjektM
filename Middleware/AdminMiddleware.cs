using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using ProjektM.Models;

namespace ProjektM.Middleware;

public class AdminMiddleware
{
    private readonly RequestDelegate _next;
    private readonly PmiContext _context;
    
    public AdminMiddleware(RequestDelegate next, PmiContext context)
    {
        _next = next;
        _context = context;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Unauthorized. User not logged in.");
            return;
        }

        var userId = int.Parse(userIdClaim.Value);
        
        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
        if (user == null || !user.IsAdmin) 
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync("Access denied. Admins only.");
            return;
        }

        await _next(context);
    }
}