using GalleryLib.model.configuration;
using GalleryLib.Service.Auth;

namespace GalleryApi.Middleware;

public class SessionAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;

    public SessionAuthMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _configuration = configuration;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip user authentication for auth endpoints
        if (context.Request.Path.StartsWithSegments("/api/v1/auth"))
        {
            await _next(context);
            return;
        }

        var dbConfig = _configuration.GetSection(DatabaseConfiguration.SectionName).Get<DatabaseConfiguration>()
                    ?? throw new InvalidOperationException("Database configuration not found");
        using var authService = new AuthService(dbConfig);


        // All endpoints require authentication and we may have only Authorization:Bearer header or we have a fully managed cookie from a logged in user
        var sessionToken = context.Request.Cookies["session_token"];        
        if (string.IsNullOrEmpty(sessionToken))
        {
            // Try to get from Authorization Bearer header
            var authHeader = context.Request.Headers["Authorization"].ToString();
            if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                sessionToken = authHeader.Substring("Bearer ".Length).Trim();
            }
        }


        // Virtual albums are accessible without user auth, but app still needs to be authenticated
        // User auth is optional here - if user is logged in, include their info for role-based filtering
        if (context.Request.Path.StartsWithSegments("/api/v1/valbums"))
        {
            // If we have a session token, validate it and add user info
            if (!string.IsNullOrEmpty(sessionToken))
            {                
                var sessionUser = await authService.ValidateSessionAsync(sessionToken);
                if (sessionUser != null)
                {
                    context.Items["User"] = sessionUser;
                    await _next(context);
                    return;
                }
            }

            // No valid user session, but public valbums are still accessible
            await _next(context);
            return;
        }

        // For all other endpoints, user authentication is required
        if (string.IsNullOrEmpty(sessionToken))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new
            {
                success = false,
                message = "Authentication required"
            });
            return;
        }

        // Validate session
        var user = await authService.ValidateSessionAsync(sessionToken);
        if (user == null)
        {
            context.Response.Cookies.Delete("session_token");
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new
            {
                success = false,
                message = "Invalid or expired session"
            });
            return;
        }

        // Add user info to request items for use in controllers
        context.Items["User"] = user;

        await _next(context);
    }
}

public static class SessionAuthMiddlewareExtensions
{
    public static IApplicationBuilder UseSessionAuth(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SessionAuthMiddleware>();
    }
}
