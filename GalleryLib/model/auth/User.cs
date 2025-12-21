using System.Data.Common;

namespace GalleryLib.Model.Auth;

public class User
{
    public long Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsAdmin { get; set; } = false;
    public DateTime CreatedUtc { get; set; }
    public DateTime? LastLoginUtc { get; set; }



    public User CreateFromDataReader(DbDataReader reader)
    {
        return new User
        {
            Id = reader.GetInt64(reader.GetOrdinal("id")),
            Username = reader.GetString(reader.GetOrdinal("username")),
            Email = reader.GetString(reader.GetOrdinal("email")),
            PasswordHash = reader.GetString(reader.GetOrdinal("password_hash")),
            FullName = reader.IsDBNull(reader.GetOrdinal("full_name")) ? null : reader.GetString(reader.GetOrdinal("full_name")),
            IsActive = reader.GetBoolean(reader.GetOrdinal("is_active")),
            IsAdmin = reader.GetBoolean(reader.GetOrdinal("is_admin")),
            CreatedUtc = reader.GetDateTime(reader.GetOrdinal("created_utc")),
            LastLoginUtc = reader.IsDBNull(reader.GetOrdinal("last_login_utc")) ? null : reader.GetDateTime(reader.GetOrdinal("last_login_utc"))
        };
    }   

}
