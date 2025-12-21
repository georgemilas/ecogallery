using GalleryLib.Model.Auth;
using GalleryLib.model.configuration;
using GalleryLib.Repository.Auth;

namespace GalleryLib.Service.Auth;

public class AuthService : IDisposable, IAsyncDisposable
{
    private readonly AuthRepository _authRepository;
    private readonly TimeSpan _sessionDuration = TimeSpan.FromDays(7); // 7 days default

    public AuthService(DatabaseConfiguration dbConfig)
    {
        _authRepository = new AuthRepository(dbConfig);
    }

    public void Dispose()
    {
        _authRepository.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await _authRepository.DisposeAsync();
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, string? ipAddress = null, string? userAgent = null)
    {
        try
        {
            // Get user by username
            var user = await _authRepository.GetUserByUsernameAsync(request.Username);
            
            if (user == null)
            {
                return new LoginResponse
                {
                    Success = false,
                    Message = "Invalid username or password"
                };
            }

            // Verify password
            if (!AuthRepository.VerifyPassword(request.Password, user.PasswordHash))
            {
                return new LoginResponse
                {
                    Success = false,
                    Message = "Invalid username or password"
                };
            }

            // Create session
            var sessionToken = await _authRepository.CreateSessionAsync(user.Id, _sessionDuration, ipAddress, userAgent );

            // Update last login
            await _authRepository.UpdateLastLoginAsync(user.Id);

            return new LoginResponse
            {
                Success = true,
                SessionToken = sessionToken,
                Message = "Login successful",
                User = new UserInfo
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    FullName = user.FullName,
                    IsAdmin = user.IsAdmin
                }
            };
        }
        catch (Exception ex)
        {
            return new LoginResponse
            {
                Success = false,
                Message = $"Login failed: {ex.Message}"
            };
        }
    }

    public async Task<bool> LogoutAsync(string sessionToken)
    {
        try
        {
            await _authRepository.DeleteSessionAsync(sessionToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<UserInfo?> ValidateSessionAsync(string sessionToken)
    {
        try
        {
            var session = await _authRepository.GetSessionByTokenAsync(sessionToken);
            if (session == null)
            {
                return null;
            }

            // Update session activity
            await _authRepository.UpdateSessionActivityAsync(sessionToken);

            // Get user info
            var user = await _authRepository.GetUserByIdAsync(session.UserId);
            if (user == null)
            {
                return null;
            }

            return new UserInfo
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FullName = user.FullName,
                IsAdmin = user.IsAdmin
            };
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> CreateUserAsync(string username, string email, string password, string? fullName = null, bool isAdmin = false)
    {
        try
        {
            var passwordHash = AuthRepository.HashPassword(password);
            await _authRepository.CreateUserAsync(username, email, passwordHash, fullName, isAdmin);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task CleanupExpiredSessionsAsync()
    {
        await _authRepository.DeleteExpiredSessionsAsync();
    }
}
