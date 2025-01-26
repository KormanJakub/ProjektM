namespace ProjektM.Middleware;

public class UserMiddleware
{
    private readonly RequestDelegate _next;

    public UserMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var isAuthenticated = context.User.Identity?.IsAuthenticated ?? false;

        if (!isAuthenticated)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Unauthorized. Please log in.");
            return;
        }

        await _next(context);
    }
}