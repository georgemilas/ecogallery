using GalleryLib.Model.Auth;

namespace GalleryApi.Middleware;

public class AppAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;

    public AppAuthMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _configuration = configuration;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip app auth for auth endpoints (login, register don't need app token)
        if (context.Request.Path.StartsWithSegments("/api/v1/auth"))
        {
            await _next(context);
            return;
        }

        // Get app API key from header or query param
        var apiKey = context.Request.Headers["X-API-Key"].ToString() 
                  ?? context.Request.Query["api_key"].ToString();

        var expectedApiKey = _configuration["AppAuth:ApiKey"];
        
        if (string.IsNullOrEmpty(apiKey) || apiKey != expectedApiKey)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new
            {
                success = false,
                message = "App authentication required"
            });
            return;
        }

        // App is authenticated, continue to session auth
        await _next(context);
    }
}

public static class AppAuthMiddlewareExtensions
{
    public static IApplicationBuilder UseAppAuth(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<AppAuthMiddleware>();
    }
}
