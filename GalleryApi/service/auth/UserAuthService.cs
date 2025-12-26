using System.Net;
using System.Net.Mail;
using GalleryApi.model;
using GalleryLib.Model.Auth;
using GalleryLib.Repository.Auth;

namespace GalleryApi.service.auth;  

public class UserAuthService : AppAuthService, IDisposable, IAsyncDisposable
{
    private readonly TimeSpan _sessionDuration = TimeSpan.FromDays(7); // 7 days default
    private readonly PasswordResetRepository _passwordResetRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _configuration;

    public UserAuthService(AuthRepository authRepository, PasswordResetRepository passwordResetRepository, IConfiguration configuration,  IHttpContextAccessor httpContextAccessor)
        : base(authRepository)
    {
        _passwordResetRepository = passwordResetRepository;
        _httpContextAccessor = httpContextAccessor;
        _configuration = configuration;
    }
    
    public override void Dispose()
    {
        base.Dispose();
        _passwordResetRepository.Dispose();
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _passwordResetRepository.DisposeAsync();  
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
        await _passwordResetRepository.CreateAsync(user.Id, token, expires);

        // Build reset link
        var frontendUrl = ServiceBase.GetBaseUrl(_httpContextAccessor);
        var link = $"{frontendUrl}/login/set-password?token={WebUtility.UrlEncode(token)}";

        // Send email (simple SMTP, configure in appsettings)
        var smtpHost = _configuration["Smtp:Host"];
        var smtpPort = int.Parse(_configuration["Smtp:Port"] ?? "25");
        var smtpUser = _configuration["Smtp:User"];
        var smtpPass = _configuration["Smtp:Pass"];
        var from = _configuration["Smtp:From"] ?? "noreply@example.com";
        var subject = "Password Reset Request";
        var body = $"Click the link to reset your password: {link}\nThis link will expire in 1 hour.";
        
        using var client = new SmtpClient(smtpHost, smtpPort)
        {
            Credentials = new NetworkCredential(smtpUser, smtpPass),
            EnableSsl = true
        };
        var mail = new MailMessage(from, request.Email, subject, body);
        await client.SendMailAsync(mail);
                
    }


    public async Task UpdateUserPasswordAsync(SetPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Token) || string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
            throw new InvalidInputException("Reset token is invalid or expired or password is not at least 6 characters.");

        var tokenEntry = await _passwordResetRepository.GetByTokenAsync(request.Token);
        if (tokenEntry == null)
            throw new InvalidInputException("Reset token is invalid or expired or password is not at least 6 characters.");

        var user = await GetUserByIdAsync(tokenEntry.UserId);
        if (user == null)
            throw new InvalidInputException("Invalid User");  

        var newPasswordHash = AuthRepository.HashPassword(request.Password);
        await _authRepository.UpdateUserPasswordAsync(tokenEntry.UserId, newPasswordHash);
        await _passwordResetRepository.MarkUsedAsync(request.Token);
    }

}
