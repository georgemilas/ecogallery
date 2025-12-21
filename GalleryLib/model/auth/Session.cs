using System.Data.Common;

namespace GalleryLib.Model.Auth;

public class Session
{
    public long Id { get; set; }
    public string SessionToken { get; set; } = string.Empty;
    public long UserId { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime ExpiresUtc { get; set; }
    public DateTime LastActivityUtc { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }


    public static Session CreateFromDataReader(DbDataReader reader)
    {
        return new Session
        {
            Id = reader.GetInt64(reader.GetOrdinal("id")),
            SessionToken = reader.GetString(reader.GetOrdinal("session_token")),
            UserId = reader.GetInt64(reader.GetOrdinal("user_id")),
            CreatedUtc = reader.GetDateTime(reader.GetOrdinal("created_utc")),
            ExpiresUtc = reader.GetDateTime(reader.GetOrdinal("expires_utc")),
            LastActivityUtc = reader.GetDateTime(reader.GetOrdinal("last_activity_utc")),
            IpAddress = reader.IsDBNull(reader.GetOrdinal("ip_address")) ? null : reader.GetString(reader.GetOrdinal("ip_address")),
            UserAgent = reader.IsDBNull(reader.GetOrdinal("user_agent")) ? null : reader.GetString(reader.GetOrdinal("user_agent"))
        };
    } 


}
