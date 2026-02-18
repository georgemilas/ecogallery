using GalleryLib.model.configuration;
using GalleryApi.service;
using GalleryLib.repository.auth;
using GalleryApi.service.auth;

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
        bool isValidApp = AppAuthMiddleware.IsAppAuthenticated(context, _configuration);        
        if (!isValidApp)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new
            {
                success = false, 
                message = "Not authorized to access this API"
            });
            return;
        }

        // Skip user authentication for auth endpoints but still require valid application bearer token
        var pathsToSkip = new[]
        {
            "/api/v1/auth",                 //login, logout, register, validate-picture etc.
            "/api/v1/pictures/_thumbnails", //thumbnails are accessible without user authentication
            "/api/v1/pictures/_validate"    //nginx auth_request validation - API key check is sufficient
        };
        if (pathsToSkip.Any(path => context.Request.Path.StartsWithSegments(path)))
        {
            await _next(context);
            return;
        }
        
        //for example videos selected for public mode (aka valbum) since we don't have HD thumbnails, we allow access to original 
        Console.WriteLine($"Checking for valbum access: {context.Request.Path}, Referer: {context.Request.Headers["Referer"]}");
        if (context.Request.Headers["Referer"].ToString().Contains("/valbum?")  
            && context.Request.Path.StartsWithSegments("/api/v1/pictures")
            && !context.Request.Path.StartsWithSegments("/api/v1/pictures/_thumbnails")            
           )
        {
            // TODO: validate that the video really belongs to a public valbum 
            // extract valbum id and image id from referer /valbum?...&id=xxxx&image=yyyy and check against db  

            Console.WriteLine($"valbum referer passing through: {context.Request.Path}, Referer: {context.Request.Headers["Referer"]}"); 
            await _next(context);
            return;
        }

        var dbConfig = _configuration.GetSection(DatabaseConfiguration.SectionName).Get<DatabaseConfiguration>()
                    ?? throw new InvalidOperationException("Database configuration not found");
        var repo = new AuthRepository(dbConfig);
        using var authService = new AppAuthService(repo);


        // we may get the user session token either because the browser send the user cookie or we may have it as an "Authorization:Bearer token" header 
        var sessionToken = context.Request.Cookies["session_token"];        
        if (string.IsNullOrEmpty(sessionToken))
        {
            var authHeader = context.Request.Headers["Authorization"].ToString();
            if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                sessionToken = authHeader.Substring("Bearer ".Length).Trim();                
            }
        }

        // Virtual albums are accessible without user authentication, but the application itself still needs to be authenticated
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
            Console.WriteLine($"User authentication failed for path: {context.Request.Path}, no session token found");
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
            Console.WriteLine($"User authentication failed for path: {context.Request.Path}, invalid or expired session");
            var isHttps2 = context.Request.IsHttps;
            context.Response.Cookies.Delete("session_token", new CookieOptions
            {
                HttpOnly = true,
                Secure = isHttps2,
                SameSite = isHttps2 ? SameSiteMode.None : SameSiteMode.Lax
            });
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new
            {
                success = false,
                message = "Invalid or expired user session"
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
