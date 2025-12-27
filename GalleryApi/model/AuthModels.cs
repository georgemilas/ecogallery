using System.ComponentModel.DataAnnotations;

namespace GalleryApi.model;

public class RegisterRequest
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
}

public class PasswordResetRequest
{
    [Required]
    public string Email { get; set; } = string.Empty;
}

public class SetPasswordRequest
{
    public string Token { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class InvalidInputException : Exception
{
    public InvalidInputException(string? message) : base(message)
    {
        
    }    
}