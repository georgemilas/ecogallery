using System;
using System.Threading.Tasks;
using GalleryLib.Model.Auth;
using GalleryLib.model.configuration;
using GalleryLib.service.database;

namespace GalleryLib.Repository.Auth
{
    public class UserTokenRepository: IDisposable, IAsyncDisposable
    {
        private readonly IDatabaseService _db;
        private readonly DatabaseConfiguration _dbConfig;
        public UserTokenRepository(DatabaseConfiguration dbConfig)
        {
            _dbConfig = dbConfig;
            _db = new PostgresDatabaseService(dbConfig.ToConnectionString());
        }

        public void Dispose()
        {
            _db.Dispose();
        }

        public async ValueTask DisposeAsync()
        {
            await _db.DisposeAsync();
        }

        public async Task<UserToken?> GetByTokenAsync(string token, string tokenType= "password_reset")
        {
            const string sql = "SELECT * FROM user_token WHERE token = @token AND token_type = @token_type AND used = false AND expires_utc > @expires_utc";
            var parameters = new { token, token_type = tokenType, expires_utc = DateTimeOffset.UtcNow };
            var result = await _db.QueryAsync(sql,(reader) => UserToken.CreateFromDataReader(reader), parameters);
            return result.FirstOrDefault();
        }

        public async Task CreateTokenAsync(long userId, string token, DateTime expiresUtc, string tokenType = "password_reset")
        {
            const string sql = @"INSERT INTO user_token (user_id, token, token_type, created_utc, expires_utc, used) VALUES (@user_id, @token, @token_type, @created_utc, @expires_utc, false)";
            await _db.ExecuteAsync(sql, new { user_id = userId, token, token_type = tokenType, created_utc = DateTime.UtcNow, expires_utc = expiresUtc });
        }

        public async Task MarkUsedAsync(string token, string tokenType = "password_reset")
        {
            const string sql = "UPDATE user_token SET used = true WHERE token = @token AND token_type = @token_type";
            await _db.ExecuteAsync(sql, new { token, token_type = tokenType });
        }
    }
}
