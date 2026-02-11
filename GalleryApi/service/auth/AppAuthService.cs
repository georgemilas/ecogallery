using System;
using System.Linq;
using GalleryLib.Model.Auth;
using GalleryLib.repository.auth;

namespace GalleryApi.service.auth;

public class AppAuthService: IDisposable, IAsyncDisposable
{
    protected readonly AuthRepository _authRepository;

    public AppAuthService(AuthRepository authRepository)
    {
        _authRepository = authRepository;
    }
    public virtual void Dispose()
    {
        _authRepository.Dispose();
    }

    public virtual async ValueTask DisposeAsync()
    {
        await _authRepository.DisposeAsync();
    }


    /// <summary>
    /// Validates a user session already exists and is valid and then return the user associated with the session 
    /// </summary>
    /// <param name="sessionToken"></param>
    /// <returns>UserInfo?</returns>
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

            var roles = await _authRepository.GetEffectiveRolesAsync(user.Id);
            if (user.IsAdmin && !roles.Contains("admin", StringComparer.OrdinalIgnoreCase))
            {
                roles = roles.Concat(new[] { "admin" }).ToList();
            }

            return new UserInfo
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FullName = user.FullName,
                IsAdmin = user.IsAdmin,
                Roles = roles
            };
        }
        catch
        {
            return null;
        }
    }


}
