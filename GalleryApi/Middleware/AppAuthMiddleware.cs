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

        bool isAppAuthenticated = IsAppAuthenticated(context, _configuration);

        if (!isAppAuthenticated)        
        {
            Console.WriteLine($"App authentication X-API-Key failed for path: {context.Request.Path}");
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new
            {
                success = false,
                message = "Not authorized to access this API"
            });
            return;
        }

        // App is authenticated, continue to session auth
        await _next(context);
    }

    public static bool IsAppAuthenticated(HttpContext context, IConfiguration configuration)
    {
        var apiKey = context.Request.Headers["X-API-Key"].ToString();
        if (string.IsNullOrEmpty(apiKey))
        {
            apiKey = context.Request.Query["q"].ToString();  //"q" is the query string param for api key
        }

        var expectedApiKey = configuration["AppAuth:ApiKey"];
        
        return !string.IsNullOrEmpty(apiKey) && apiKey == expectedApiKey;
    }
}

public static class AppAuthMiddlewareExtensions
{
    public static IApplicationBuilder UseAppAuth(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<AppAuthMiddleware>();
    }
}
