using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GalleryLib.Service.AWS
{
    /// <summary>
    /// Service to retrieve database credentials from AWS Secrets Manager
    /// Implements caching to reduce API calls and costs
    /// </summary>
    public class SecretsManagerService
    {
        private readonly ILogger<SecretsManagerService> _logger;
        private readonly string _secretName;
        private readonly string _region;
        private readonly IAmazonSecretsManager _secretsManager;
        
        // Cache the secret for 5 minutes to reduce API calls
        private DatabaseSecret? _cachedSecret;
        private DateTime _cacheExpiry = DateTime.MinValue;
        private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(5);
        
        public SecretsManagerService(IConfiguration configuration, ILogger<SecretsManagerService> logger)
        {
            _logger = logger;
            _secretName = configuration["AWS:SecretName"] ?? "ecogallery/dev/rds/master-password";
            _region = configuration["AWS:Region"] ?? "us-east-1";
            _secretsManager = new AmazonSecretsManagerClient(Amazon.RegionEndpoint.GetBySystemName(_region));
        }
        
        /// <summary>
        /// Get database connection string from AWS Secrets Manager
        /// </summary>
        public async Task<string> GetConnectionStringAsync()
        {
            var secret = await GetSecretAsync();
            
            // Build connection string for Npgsql
            return $"Host={secret.Host};Port={secret.Port};Database={secret.DbName};Username={secret.Username};Password={secret.Password};SSL Mode=Require;Trust Server Certificate=true;";
        }
        
        /// <summary>
        /// Get database configuration from AWS Secrets Manager
        /// Returns cached value if still valid
        /// </summary>
        public async Task<DatabaseSecret> GetSecretAsync()
        {
            // Return cached secret if still valid
            if (_cachedSecret != null && DateTime.UtcNow < _cacheExpiry)
            {
                _logger.LogDebug("Returning cached database secret");
                return _cachedSecret;
            }
            
            _logger.LogInformation("Fetching database secret from AWS Secrets Manager: {SecretName}", _secretName);
            
            try
            {
                var request = new GetSecretValueRequest
                {
                    SecretId = _secretName,
                    VersionStage = "AWSCURRENT" // Use current version
                };
                
                var response = await _secretsManager.GetSecretValueAsync(request);
                
                // Parse the JSON secret
                var secretJson = response.SecretString;
                var secretData = JsonSerializer.Deserialize<SecretData>(secretJson);
                
                if (secretData == null)
                {
                    throw new Exception("Failed to deserialize secret data");
                }
                
                // Create DatabaseSecret from the data
                _cachedSecret = new DatabaseSecret
                {
                    Host = secretData.host,
                    Port = secretData.port,
                    DbName = secretData.dbname,
                    Username = secretData.username,
                    Password = secretData.password
                };
                
                // Set cache expiry
                _cacheExpiry = DateTime.UtcNow.Add(_cacheDuration);
                
                _logger.LogInformation("Successfully retrieved and cached database secret");
                
                return _cachedSecret;
            }
            catch (ResourceNotFoundException ex)
            {
                _logger.LogError(ex, "Secret not found: {SecretName}", _secretName);
                throw new Exception($"Database secret not found: {_secretName}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving secret from Secrets Manager");
                throw new Exception("Failed to retrieve database credentials from AWS Secrets Manager", ex);
            }
        }
        
        /// <summary>
        /// Force refresh the cached secret (useful after rotation)
        /// </summary>
        public async Task<DatabaseSecret> RefreshSecretAsync()
        {
            _logger.LogInformation("Forcing refresh of database secret");
            _cachedSecret = null;
            _cacheExpiry = DateTime.MinValue;
            return await GetSecretAsync();
        }
        
        /// <summary>
        /// Internal class for deserializing the Secrets Manager JSON
        /// </summary>
        private class SecretData
        {
            public string username { get; set; } = string.Empty;
            public string password { get; set; } = string.Empty;
            public string engine { get; set; } = string.Empty;
            public string host { get; set; } = string.Empty;
            public int port { get; set; }
            public string dbname { get; set; } = string.Empty;
            public string? dbInstanceIdentifier { get; set; }
        }
    }
    
    /// <summary>
    /// Database configuration retrieved from Secrets Manager
    /// </summary>
    public class DatabaseSecret
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; }
        public string DbName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
