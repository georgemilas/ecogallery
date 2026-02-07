using System;
using System.Linq;
using System.Net;
using System.Net.Mail;
using GalleryApi.model;
using GalleryApi.service.email;
using GalleryLib.Model.Auth;
using GalleryLib.Repository.Auth;

namespace GalleryApi.service.auth;  

public class UserAuthService : AppAuthService, IDisposable, IAsyncDisposable
{
    private readonly TimeSpan _sessionDuration = TimeSpan.FromDays(7); // 7 days default
    private readonly UserTokenRepository _userTokenRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _configuration;
    private readonly IEmailSender _emailSender;

    public UserAuthService(
        AuthRepository authRepository, 
        UserTokenRepository userTokenRepository, 
        IConfiguration configuration,  
        IHttpContextAccessor httpContextAccessor,
        IEmailSender emailSender)
        : base(authRepository)
    {
        _userTokenRepository = userTokenRepository;
        _httpContextAccessor = httpContextAccessor;
        _configuration = configuration;
        _emailSender = emailSender;
    }
    
    public override void Dispose()
    {
        base.Dispose();
        _userTokenRepository.Dispose();
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _userTokenRepository.DisposeAsync();  
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

            var roles = await _authRepository.GetEffectiveRolesAsync(user.Id);
            if (user.IsAdmin && !roles.Contains("admin", StringComparer.OrdinalIgnoreCase))
            {
                roles = roles.Concat(new[] { "admin" }).ToList();
            }

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
                    IsAdmin = user.IsAdmin,
                    Roles = roles
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

    public async Task CreateUserAsync(RegisterRequest request, bool isAdmin = false)
    {        
        if (string.IsNullOrWhiteSpace(request.Token) || string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
            throw new InvalidInputException("Token is either invalid or expired or password is insecure.");

        var tokenEntry = await _userTokenRepository.GetByTokenAsync(request.Token, "user_registration");
        if (tokenEntry == null)
            throw new InvalidInputException("Token is either invalid or expired or password is insecure.");

        var passwordHash = AuthRepository.HashPassword(request.Password);
        await _authRepository.CreateUserAsync(request.Username, request.Email, passwordHash, request.FullName, isAdmin);                    
        await _userTokenRepository.MarkUsedAsync(request.Token, "user_registration");
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {        
        var user = await _authRepository.GetUserByEmailAsync(email);
        if (user == null)
        {
            throw new Exception("User not found");
        } 
        return user;        
    }
    public async Task<User?> GetUserByIdAsync(long userId)
    {        
        var user = await _authRepository.GetUserByIdAsync(userId);
        if (user == null)
        {
            throw new Exception("User not found");
        } 
        return user;        
    }

    public async Task CleanupExpiredSessionsAsync()
    {
        await _authRepository.DeleteExpiredSessionsAsync();
    }


    public async Task SetPasswordResetRequest(PasswordResetRequest request)
    {
        // Always return success to avoid leaking user existence
        var user = await GetUserByEmailAsync(request.Email);
        if (user == null)
            throw new InvalidInputException("Invalid Request");

        // Generate token
        var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray()).Replace("=", "").Replace("+", "");
        var expires = DateTime.UtcNow.AddHours(1);
        await _userTokenRepository.CreateTokenAsync(user.Id, token, expires);

        // Build reset link
        var frontendUrl = ServiceBase.GetBaseUrl(_httpContextAccessor);
        var link = $"{frontendUrl}/login/set-password?token={WebUtility.UrlEncode(token)}";

        // Send email via IEmailSender
        var subject = "Password Reset Request";
        var body = $"Click the link to reset your password: {link}\nThis link will expire in 1 hour.";
        
        await _emailSender.SendEmailAsync(request.Email, subject, body);
    }


    public async Task CreateUserInvitationAsync(InviteUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Name))
            throw new InvalidInputException("Email and name are required.");

        var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray()).Replace("=", "").Replace("+", "");
        var expires = DateTime.UtcNow.AddDays(7);
        await _userTokenRepository.CreateTokenAsync(token, expires, "user_registration");

        var frontendUrl = ServiceBase.GetBaseUrl(_httpContextAccessor);
        var link = $"{frontendUrl}/login/register?token={WebUtility.UrlEncode(token)}";

        var subject = "You've been invited to join the Gallery";
        var body = $"Hello {request.Name},\n\nYou've been invited to join the gallery. Click the link below to register:\n\n{link}\n\nThis invitation link will expire in 7 days.";

        await _emailSender.SendEmailAsync(request.Email, subject, body);
    }

    public async Task UpdateUserPasswordAsync(SetPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Token) || string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
            throw new InvalidInputException("Either token is invalid or expired or password is insecure.");

        var tokenEntry = await _userTokenRepository.GetByTokenAsync(request.Token, "password_reset");
        if (tokenEntry == null || tokenEntry.UserId == null)
            throw new InvalidInputException("Either token is invalid or expired or password is insecure.");

        var user = await GetUserByIdAsync(tokenEntry.UserId ?? 0);
        if (user == null)
            throw new InvalidInputException("Invalid User");  

        var newPasswordHash = AuthRepository.HashPassword(request.Password);
        await _authRepository.UpdateUserPasswordAsync(tokenEntry.UserId ?? 0, newPasswordHash);
        await _userTokenRepository.MarkUsedAsync(request.Token, "password_reset");
    }

}
