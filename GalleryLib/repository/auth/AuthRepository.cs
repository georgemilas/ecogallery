    
using System.Security.Cryptography;
using System.Text;
using GalleryLib.Model.Auth;
using GalleryLib.model.configuration;
using GalleryLib.service.database;

namespace GalleryLib.Repository.Auth;

public class AuthRepository : IDisposable, IAsyncDisposable
{
    private readonly IDatabaseService _db;
    private readonly DatabaseConfiguration _dbConfig;

    public AuthRepository(DatabaseConfiguration dbConfig)
    {
        _dbConfig = dbConfig;
        _db = new PostgresDatabaseService(_dbConfig.ToConnectionString());
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await _db.DisposeAsync();
    }

    // User methods
    public async Task<User?> GetUserByUsernameAsync(string username)
    {
        const string sql = "SELECT * FROM public.user WHERE username = @username AND is_active = true";
        var parameters = new { username };
        var result = await _db.QueryAsync<User>(sql, reader => User.CreateFromDataReader(reader), parameters);
        return result.FirstOrDefault();
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        const string sql = "SELECT * FROM public.user WHERE email = @email AND is_active = true";
        var parameters = new { email };
        var result = await _db.QueryAsync(sql, reader => User.CreateFromDataReader(reader), parameters);
        return result.FirstOrDefault();
    }

    public async Task<User?> GetUserByIdAsync(long userId)
    {
        const string sql = "SELECT * FROM public.user WHERE id = @id AND is_active = true";
        var parameters = new { id = userId };
        var result = await _db.QueryAsync(sql, reader => User.CreateFromDataReader(reader), parameters);
        return result.FirstOrDefault();
    }

    public async Task<long> CreateUserAsync(string username, string email, string passwordHash, string? fullName = null, bool isAdmin = false)
    {
        const string sql = @"
            INSERT INTO public.user (username, email, password_hash, full_name, is_admin, created_utc)
            VALUES (@username, @email, @password_hash, @full_name, @is_admin, @created_utc)
            RETURNING id";
        User user = new User() { Username = username, Email = email, PasswordHash = passwordHash, FullName = fullName, IsAdmin = isAdmin, CreatedUtc = DateTime.UtcNow };
        var result = await _db.ExecuteScalarAsync<long>(sql, parameters: user);        
        return result;
    }

    public async Task UpdateLastLoginAsync(long userId)
    {
        const string sql = "UPDATE public.user SET last_login_utc = @last_login_utc WHERE id = @id";
        await _db.ExecuteAsync(sql, new { id = userId, last_login_utc = DateTime.UtcNow });
    }

    // Session methods
    public async Task<Session?> GetSessionByTokenAsync(string sessionToken)
    {
        const string sql = @"
            SELECT * FROM public.session 
            WHERE session_token = @session_token 
            AND expires_utc > @expired_utc";
        var parameters = new { session_token = sessionToken, expired_utc = DateTime.UtcNow };
        
        var result = await _db.QueryAsync<Session>(sql, reader => Session.CreateFromDataReader(reader), parameters);        
        return result.FirstOrDefault();
    }

    public async Task<string> CreateSessionAsync(long userId, TimeSpan duration, string? ipAddress = null, string? userAgent = null)
    {
        var sessionToken = GenerateSecureToken();
        var now = DateTime.UtcNow;
        
        const string sql = @"
            INSERT INTO public.session (session_token, user_id, created_utc, expires_utc, last_activity_utc, ip_address, user_agent)
            VALUES (@session_token, @user_id, @created_utc, @expires_utc, @last_activity_utc, @ip_address, @user_agent)";
        
        Session session = new Session() {  
            SessionToken = sessionToken, 
            UserId = userId, 
            CreatedUtc = now, 
            ExpiresUtc = now.Add(duration), 
            LastActivityUtc = now, 
            IpAddress = ipAddress, 
            UserAgent = userAgent };

        await _db.ExecuteAsync(sql, session);        
        return sessionToken;
    }

    public async Task UpdateSessionActivityAsync(string sessionToken)
    {
        const string sql = "UPDATE public.session SET last_activity_utc = @last_activity_utc WHERE session_token = @session_token";
        await _db.ExecuteAsync(sql, new { session_token = sessionToken, last_activity_utc = DateTime.UtcNow });
    }

    public async Task DeleteSessionAsync(string sessionToken)
    {
        const string sql = "DELETE FROM public.session WHERE session_token = @session_token";
        await _db.ExecuteAsync(sql, new { session_token = sessionToken });
    }

    public async Task DeleteExpiredSessionsAsync()
    {
        const string sql = "DELETE FROM public.session WHERE expires_utc < @expires_utc";
        await _db.ExecuteAsync(sql, new { expires_utc = DateTime.UtcNow });
    }

    public async Task DeleteUserSessionsAsync(long userId)
    {
        const string sql = "DELETE FROM public.session WHERE user_id = @user_id";
        await _db.ExecuteAsync(sql, new { user_id =  userId });
    }

    // Helper methods
    private static string GenerateSecureToken()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }

    public static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(password);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    public static bool VerifyPassword(string password, string passwordHash)
    {
        var hash = HashPassword(password);
        return hash == passwordHash;
    }
    public async Task UpdateUserPasswordAsync(long userId, string newPasswordHash)
    {
        const string sql = "UPDATE public.user SET password_hash = @password_hash WHERE id = @id";
        await _db.ExecuteAsync(sql, new { id = userId, password_hash = newPasswordHash });
    }
}
