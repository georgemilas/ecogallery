using System;
using System.Data.Common;

namespace GalleryLib.Model.Auth
{
    public class UserToken
    {
        public long Id { get; set; }
        public long? UserId { get; set; }
        public long? RoleId { get; set; }
        public string Token { get; set; } = string.Empty;
        public string TokenType { get; set; } = "password_reset"; // e.g., 'password_reset', 'user_registration'
        public string? Email { get; set; }
        public string? Name { get; set; }
        public DateTimeOffset CreatedUtc { get; set; }
        public DateTimeOffset ExpiresUtc { get; set; }
        public bool Used { get; set; }

        public static UserToken CreateFromDataReader(DbDataReader reader)
        {
            return new UserToken
            {
                Id = reader.GetInt64(reader.GetOrdinal("id")),
                UserId = reader.IsDBNull(reader.GetOrdinal("user_id")) ? null : reader.GetInt64(reader.GetOrdinal("user_id")),
                RoleId = reader.IsDBNull(reader.GetOrdinal("role_id")) ? null : reader.GetInt64(reader.GetOrdinal("role_id")),
                Token = reader.GetString(reader.GetOrdinal("token")),
                TokenType = reader.GetString(reader.GetOrdinal("token_type")),
                Email = reader.IsDBNull(reader.GetOrdinal("email")) ? null : reader.GetString(reader.GetOrdinal("email")),
                Name = reader.IsDBNull(reader.GetOrdinal("name")) ? null : reader.GetString(reader.GetOrdinal("name")),
                CreatedUtc = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("created_utc")),
                ExpiresUtc = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("expires_utc")),
                Used = reader.GetBoolean(reader.GetOrdinal("used"))
            };
        }
    }



}
