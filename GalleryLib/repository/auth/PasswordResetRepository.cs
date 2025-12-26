using System;
using System.Threading.Tasks;
using GalleryLib.Model.Auth;
using GalleryLib.model.configuration;
using GalleryLib.service.database;

namespace GalleryLib.Repository.Auth
{
    public class PasswordResetRepository: IDisposable, IAsyncDisposable
    {
        private readonly IDatabaseService _db;
        private readonly DatabaseConfiguration _dbConfig;
        public PasswordResetRepository(DatabaseConfiguration dbConfig)
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

        public async Task<PasswordResetToken?> GetByTokenAsync(string token)
        {
            const string sql = "SELECT * FROM password_reset_token WHERE token = @token AND used = false AND expires_utc > @now";
            var parameters = new { token, now = DateTimeOffset.UtcNow };
            var result = await _db.QueryAsync(sql,(reader) => PasswordResetToken.CreateFromDataReader(reader), parameters);
            return result.FirstOrDefault();
        }

        public async Task CreateAsync(long userId, string token, DateTime expiresUtc)
        {
            const string sql = @"INSERT INTO password_reset_token (user_id, token, created_utc, expires_utc, used) VALUES (@user_id, @token, @created_utc, @expires_utc, false)";
            await _db.ExecuteAsync(sql, new { user_id = userId, token, created_utc = DateTime.UtcNow, expires_utc = expiresUtc });
        }

        public async Task MarkUsedAsync(string token)
        {
            const string sql = "UPDATE password_reset_token SET used = true WHERE token = @token";
            await _db.ExecuteAsync(sql, new { token });
        }
    }
}
