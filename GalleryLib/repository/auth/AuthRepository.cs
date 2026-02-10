    
using System.Linq;
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
        const string sql = "SELECT * FROM public.users WHERE username = @username AND is_active = true";
        var parameters = new { username };
        var result = await _db.QueryAsync<User>(sql, reader => User.CreateFromDataReader(reader), parameters);
        return result.FirstOrDefault();
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        const string sql = "SELECT * FROM public.users WHERE email = @email AND is_active = true";
        var parameters = new { email };
        var result = await _db.QueryAsync(sql, reader => User.CreateFromDataReader(reader), parameters);
        return result.FirstOrDefault();
    }

    public async Task<User?> GetUserByIdAsync(long userId)
    {
        const string sql = "SELECT * FROM public.users WHERE id = @id AND is_active = true";
        var parameters = new { id = userId };
        var result = await _db.QueryAsync(sql, reader => User.CreateFromDataReader(reader), parameters);
        return result.FirstOrDefault();
    }

    public async Task<IReadOnlyList<string>> GetEffectiveRolesAsync(long userId)
    {
        const string sql = @"
            WITH RECURSIVE role_tree AS (
                SELECT r.id, r.name
                FROM public.user_roles ur
                JOIN public.roles r ON r.id = ur.role_id
                WHERE ur.user_id = @user_id
                
                UNION
                
                SELECT r2.id, r2.name
                FROM role_tree rt
                JOIN public.role_hierarchy rh ON rh.child_role_id = rt.id
                JOIN public.roles r2 ON r2.id = rh.parent_role_id
            )
            SELECT DISTINCT name FROM role_tree ORDER BY name;";

        var result = await _db.QueryAsync(sql, reader => reader.GetString(0), new { user_id = userId });
        return result.ToList();
    }

    public async Task<long> CreateUserAsync(string username, string email, string passwordHash, string? fullName = null, bool isAdmin = false)
    {
        const string sql = @"
            INSERT INTO public.users (username, email, password_hash, full_name, is_admin, created_utc)
            VALUES (@username, @email, @password_hash, @full_name, @is_admin, @created_utc)
            RETURNING id";
        User user = new User() { Username = username, Email = email, PasswordHash = passwordHash, FullName = fullName, IsAdmin = isAdmin, CreatedUtc = DateTime.UtcNow };
        var result = await _db.ExecuteScalarAsync<long>(sql, parameters: user);        
        return result;
    }

    public async Task UpdateLastLoginAsync(long userId)
    {
        const string sql = "UPDATE public.users SET last_login_utc = @last_login_utc WHERE id = @id";
        await _db.ExecuteAsync(sql, new { id = userId, last_login_utc = DateTime.UtcNow });
    }

    // Session methods
    public async Task<Session?> GetSessionByTokenAsync(string sessionToken)
    {
        const string sql = @"
            SELECT * FROM public.sessions 
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
            INSERT INTO public.sessions (session_token, user_id, created_utc, expires_utc, last_activity_utc, ip_address, user_agent)
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
        const string sql = "UPDATE public.sessions SET last_activity_utc = @last_activity_utc WHERE session_token = @session_token";
        await _db.ExecuteAsync(sql, new { session_token = sessionToken, last_activity_utc = DateTime.UtcNow });
    }

    public async Task DeleteSessionAsync(string sessionToken)
    {
        const string sql = "DELETE FROM public.sessions WHERE session_token = @session_token";
        await _db.ExecuteAsync(sql, new { session_token = sessionToken });
    }

    public async Task DeleteExpiredSessionsAsync()
    {
        const string sql = "DELETE FROM public.sessions WHERE expires_utc < @expires_utc";
        await _db.ExecuteAsync(sql, new { expires_utc = DateTime.UtcNow });
    }

    public async Task DeleteUserSessionsAsync(long userId)
    {
        const string sql = "DELETE FROM public.sessions WHERE user_id = @user_id";
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
        const string sql = "UPDATE public.users SET password_hash = @password_hash WHERE id = @id";
        await _db.ExecuteAsync(sql, new { id = userId, password_hash = newPasswordHash });
    }

    // Role methods
    public async Task<List<Role>> GetAllRolesAsync()
    {
        const string sql = "SELECT id, name, description FROM public.roles ORDER BY name";
        var result = await _db.QueryAsync(sql, reader => Role.CreateFromDataReader(reader));
        return result.ToList();
    }

    public async Task<IReadOnlyList<string>> GetEffectiveRolesForRoleAsync(long roleId)
    {
        const string sql = @"
            WITH RECURSIVE role_tree AS (
                SELECT r.id, r.name
                FROM public.roles r
                WHERE r.id = @role_id

                UNION

                SELECT r2.id, r2.name
                FROM role_tree rt
                JOIN public.role_hierarchy rh ON rh.child_role_id = rt.id
                JOIN public.roles r2 ON r2.id = rh.parent_role_id
            )
            SELECT DISTINCT name FROM role_tree ORDER BY name;";

        var result = await _db.QueryAsync(sql, reader => reader.GetString(0), new { role_id = roleId });
        return result.ToList();
    }

    public async Task AssignRoleToUserAsync(long userId, long roleId)
    {
        const string sql = "INSERT INTO public.user_roles (user_id, role_id) VALUES (@user_id, @role_id) ON CONFLICT DO NOTHING";
        await _db.ExecuteAsync(sql, new { user_id = userId, role_id = roleId });
    }

    public async Task<long> CreateRoleAsync(string name, string? description)
    {
        const string sql = "INSERT INTO public.roles (name, description) VALUES (@name, @description) RETURNING id";
        return await _db.ExecuteScalarAsync<long>(sql, parameters: new { name, description });
    }

    public async Task AddRoleHierarchyAsync(long parentRoleId, long childRoleId)
    {
        const string sql = "INSERT INTO public.role_hierarchy (parent_role_id, child_role_id) VALUES (@parent_role_id, @child_role_id) ON CONFLICT DO NOTHING";
        await _db.ExecuteAsync(sql, new { parent_role_id = parentRoleId, child_role_id = childRoleId });
    }
}
