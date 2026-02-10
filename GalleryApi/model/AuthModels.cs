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

public class InviteUserRequest
{
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public long RoleId { get; set; }
}

public class CreateRoleRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public long? ParentRoleId { get; set; }
}

public class InvalidInputException : Exception
{
    public InvalidInputException(string? message) : base(message)
    {
        
    }    
}