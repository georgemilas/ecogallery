# Example: Using Secrets Manager in Your EcoGallery Application

This example shows how to integrate AWS Secrets Manager into your .NET application.

## 1. Install NuGet Package

```bash
cd GalleryLib
dotnet add package AWSSDK.SecretsManager
```

## 2. Add Configuration

Update `appsettings.json`:

```json
{
  "AWS": {
    "SecretName": "ecogallery/dev/rds/master-password",
    "Region": "us-east-1"
  }
}
```

For production (`appsettings.Production.json`):

```json
{
  "AWS": {
    "SecretName": "ecogallery/production/rds/master-password",
    "Region": "us-east-1"
  }
}
```

## 3. Add the Service

Copy `SecretsManagerService.cs` to your `GalleryLib/service/aws/` directory.

## 4. Register in Startup/Program.cs

**For GalleryApi/Program.cs:**

```csharp
using GalleryLib.Service.AWS;

var builder = WebApplication.CreateBuilder(args);

// Register SecretsManagerService as singleton (for caching)
builder.Services.AddSingleton<SecretsManagerService>();

// Other services...
```

## 5. Update DatabaseConfiguration

**Option A: Modify existing DatabaseConfiguration class**

```csharp
using GalleryLib.Service.AWS;

public class DatabaseConfiguration
{
    private readonly SecretsManagerService? _secretsManager;
    
    public string Host { get; set; }
    public int Port { get; set; }
    public string Database { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public string SslMode { get; set; }
    
    public DatabaseConfiguration()
    {
        // Default constructor for manual configuration
    }
    
    public DatabaseConfiguration(IConfiguration configuration, SecretsManagerService secretsManager)
    {
        _secretsManager = secretsManager;
        
        // Try to load from AWS Secrets Manager if configured
        if (!string.IsNullOrEmpty(configuration["AWS:SecretName"]))
        {
            LoadFromSecretsManagerAsync().Wait();
        }
        else
        {
            // Fall back to appsettings.json
            Host = configuration["Database:Host"];
            Port = int.Parse(configuration["Database:Port"] ?? "5432");
            Database = configuration["Database:Database"];
            Username = configuration["Database:Username"];
            Password = configuration["Database:Password"];
            SslMode = configuration["Database:SslMode"] ?? "Require";
        }
    }
    
    private async Task LoadFromSecretsManagerAsync()
    {
        var secret = await _secretsManager.GetSecretAsync();
        Host = secret.Host;
        Port = secret.Port;
        Database = secret.DbName;
        Username = secret.Username;
        Password = secret.Password;
        SslMode = "Require";
    }
    
    public string ToConnectionString()
    {
        return $"Host={Host};Port={Port};Database={Database};Username={Username};Password={Password};SSL Mode={SslMode};Trust Server Certificate=true;";
    }
}
```

**Option B: Use SecretsManagerService directly in repositories**

```csharp
public class UserRepository
{
    private readonly SecretsManagerService _secretsManager;
    
    public UserRepository(SecretsManagerService secretsManager)
    {
        _secretsManager = secretsManager;
    }
    
    private async Task<NpgsqlConnection> GetConnectionAsync()
    {
        var connectionString = await _secretsManager.GetConnectionStringAsync();
        var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        return connection;
    }
    
    public async Task<User> GetUserByIdAsync(int userId)
    {
        using var connection = await GetConnectionAsync();
        // Your query logic...
    }
}
```

## 6. Handle Connection Failures (Password Rotation)

```csharp
public async Task<User> GetUserByIdAsync(int userId)
{
    int retries = 2;
    Exception lastException = null;
    
    for (int i = 0; i < retries; i++)
    {
        try
        {
            using var connection = await GetConnectionAsync();
            // Your query logic...
            return user;
        }
        catch (PostgresException ex) when (ex.SqlState == "28P01") // Invalid password
        {
            _logger.LogWarning("Database authentication failed, refreshing credentials (attempt {Attempt})", i + 1);
            
            // Force refresh the secret
            await _secretsManager.RefreshSecretAsync();
            lastException = ex;
            
            if (i == retries - 1)
            {
                throw;
            }
        }
    }
    
    throw new Exception("Failed to connect to database after credential refresh", lastException);
}
```

## 7. IAM Permissions

Your application needs an IAM role with these permissions:

**For EC2:**
1. Create IAM role: `ecogallery-app-role`
2. Attach this policy:

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": [
        "secretsmanager:GetSecretValue",
        "secretsmanager:DescribeSecret"
      ],
      "Resource": "arn:aws:secretsmanager:us-east-1:*:secret:ecogallery/*"
    }
  ]
}
```

3. Attach role to EC2 instance

**For local development:**

```bash
# Configure AWS CLI with credentials
aws configure

# Or use environment variables
export AWS_ACCESS_KEY_ID=your-key
export AWS_SECRET_ACCESS_KEY=your-secret
export AWS_DEFAULT_REGION=us-east-1
```

## 8. Testing

```bash
# Test locally
cd GalleryApi
dotnet run

# The app should now fetch credentials from Secrets Manager
# Check logs for: "Fetching database secret from AWS Secrets Manager"
```

## 9. Environment Variables (Alternative)

If you don't want to integrate the SDK, fetch the secret once:

```bash
# Get the secret
SECRET=$(aws secretsmanager get-secret-value \
  --secret-id ecogallery/dev/rds/master-password \
  --query SecretString --output text)

# Extract password
PASSWORD=$(echo $SECRET | jq -r '.password')

# Set environment variable
export DB_PASSWORD=$PASSWORD

# Run application
dotnet run --project GalleryApi
```

Then in your app:
```csharp
Password = Environment.GetEnvironmentVariable("DB_PASSWORD") 
           ?? configuration["Database:Password"];
```

## Cost Impact

- API calls: ~0.0001 per request
- With 5-minute caching: ~288 calls/day = **$0.001/day** = **$0.03/month**
- Secret storage: **$0.40/month**
- **Total: ~$0.43/month**

## Production Checklist

- [ ] Secret created in Secrets Manager
- [ ] Lambda rotation function deployed
- [ ] Rotation schedule enabled (30 days)
- [ ] IAM roles configured
- [ ] Application updated to use SecretsManagerService
- [ ] Connection retry logic implemented
- [ ] CloudWatch alarms set for rotation failures
- [ ] Tested manual rotation
- [ ] Removed hardcoded passwords from appsettings.Production.json
