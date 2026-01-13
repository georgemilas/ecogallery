# AWS Secrets Manager Password Rotation Guide

This guide explains how to use AWS Secrets Manager for secure password storage and automatic rotation for your RDS PostgreSQL database.

## Why Use AWS Secrets Manager?

✅ **Benefits:**
- **No hardcoded passwords** in code or config files
- **Automatic rotation** on a schedule (e.g., every 30 days)
- **Encryption at rest** using AWS KMS
- **Audit trail** of who accessed secrets and when
- **Fine-grained access control** using IAM policies
- **Automatic rollback** if rotation fails

## Architecture

```
┌─────────────────┐         ┌──────────────────┐         ┌─────────────┐
│   Application   │────────>│ Secrets Manager  │────────>│  AWS KMS    │
│  (.NET Core)    │         │   (Secret)       │         │ (Encryption)│
└─────────────────┘         └──────────────────┘         └─────────────┘
                                     │
                                     │ Triggers rotation
                                     ▼
                            ┌──────────────────┐
                            │ Lambda Function  │
                            │   (Rotation)     │
                            └──────────────────┘
                                     │
                                     ▼
                            ┌──────────────────┐
                            │  RDS PostgreSQL  │
                            └──────────────────┘
```

## Setup Steps

### 1. Enable Secrets Manager in Terraform

The `secrets-manager.tf` file is already created. To enable it:

**Edit your `terraform.dev.tfvars`:**
```hcl
# Enable password rotation
enable_password_rotation = true
password_rotation_days   = 30  # Rotate every 30 days

# Lambda needs subnets with NAT Gateway or VPC endpoints
lambda_subnet_ids = [
  "subnet-xxxxxxxx",
  "subnet-yyyyyyyy"
]
```

### 2. Build the Lambda Rotation Function

```bash
cd terraform/lambda_rotation

# On Linux/Mac:
bash build.sh

# On Windows PowerShell:
.\build.ps1
```

This creates `lambda_rotation.zip` which Terraform will deploy.

### 3. Deploy with Terraform

```bash
cd ..
terraform init
terraform plan -var-file="terraform.dev.tfvars"
terraform apply -var-file="terraform.dev.tfvars"
```

This creates:
- Secrets Manager secret with your RDS password
- Lambda function for password rotation
- IAM roles and policies
- Rotation schedule (if enabled)

### 4. Get Secret Details

```bash
# Get the secret name
terraform output secrets_manager_name

# Example output: ecogallery/dev/rds/master-password
```

## Using Secrets in Your Application

### Option 1: AWS SDK in .NET (Recommended)

Install the NuGet package:
```bash
dotnet add package AWSSDK.SecretsManager
```

**Example code:**

```csharp
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using System.Text.Json;

public class DatabaseConfiguration
{
    public string Host { get; set; }
    public int Port { get; set; }
    public string Database { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
}

public async Task<DatabaseConfiguration> GetDatabaseConfigAsync()
{
    var secretName = "ecogallery/dev/rds/master-password";
    var region = "us-east-1";

    var client = new AmazonSecretsManagerClient(Amazon.RegionEndpoint.GetBySystemName(region));

    GetSecretValueRequest request = new GetSecretValueRequest
    {
        SecretId = secretName,
        VersionStage = "AWSCURRENT"
    };

    GetSecretValueResponse response = await client.GetSecretValueAsync(request);
    
    var secretJson = response.SecretString;
    var secret = JsonSerializer.Deserialize<DatabaseSecret>(secretJson);
    
    return new DatabaseConfiguration
    {
        Host = secret.host,
        Port = secret.port,
        Database = secret.dbname,
        Username = secret.username,
        Password = secret.password
    };
}

private class DatabaseSecret
{
    public string username { get; set; }
    public string password { get; set; }
    public string engine { get; set; }
    public string host { get; set; }
    public int port { get; set; }
    public string dbname { get; set; }
}
```

### Option 2: Environment Variables (Simpler, but no auto-rotation)

Fetch once at startup:

```bash
# Get secret and set environment variable
aws secretsmanager get-secret-value \
  --secret-id ecogallery/dev/rds/master-password \
  --query SecretString --output text | jq -r '.password'
```

Then read from environment in your app:
```csharp
var password = Environment.GetEnvironmentVariable("DB_PASSWORD");
```

### Option 3: AWS Parameter Store Integration

Store the secret reference in appsettings.json:

```json
{
  "Database": {
    "SecretName": "ecogallery/dev/rds/master-password",
    "Region": "us-east-1"
  }
}
```

## Testing Password Rotation

### Manual Rotation

Test rotation manually before enabling automatic schedule:

```bash
# Rotate immediately
aws secretsmanager rotate-secret \
  --secret-id ecogallery/dev/rds/master-password

# Check rotation status
aws secretsmanager describe-secret \
  --secret-id ecogallery/dev/rds/master-password
```

### Monitor Rotation

```bash
# View CloudWatch logs for Lambda rotation function
aws logs tail /aws/lambda/ecogallery-rds-rotation-dev --follow

# Check rotation history
aws secretsmanager list-secret-version-ids \
  --secret-id ecogallery/dev/rds/master-password
```

## Handling Rotation in Application

### Best Practices

1. **Cache secrets with TTL**
   - Cache for 5-10 minutes
   - Refresh before TTL expires
   - Handle rotation gracefully

2. **Retry on auth failure**
   - If database auth fails, refresh secret
   - Retry connection with new credentials

3. **Use connection pooling**
   - Connection pools automatically reconnect
   - Failed connections are removed from pool

**Example with caching:**

```csharp
private DatabaseConfiguration _cachedConfig;
private DateTime _cacheExpiry;
private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(5);

public async Task<DatabaseConfiguration> GetDatabaseConfigAsync()
{
    if (_cachedConfig != null && DateTime.UtcNow < _cacheExpiry)
    {
        return _cachedConfig;
    }

    _cachedConfig = await FetchFromSecretsManagerAsync();
    _cacheExpiry = DateTime.UtcNow.Add(_cacheDuration);
    
    return _cachedConfig;
}
```

## IAM Permissions

Your application needs these permissions to read secrets:

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
      "Resource": "arn:aws:secretsmanager:us-east-1:123456789012:secret:ecogallery/dev/rds/master-password-*"
    },
    {
      "Effect": "Allow",
      "Action": [
        "kms:Decrypt"
      ],
      "Resource": "arn:aws:kms:us-east-1:123456789012:key/your-kms-key-id"
    }
  ]
}
```

Attach this policy to:
- EC2 instance role (if running on EC2)
- ECS task role (if running on ECS/Fargate)
- Lambda execution role (if running on Lambda)

## Retrieving Secrets via CLI

```bash
# Get the full secret as JSON
aws secretsmanager get-secret-value \
  --secret-id ecogallery/dev/rds/master-password \
  --query SecretString --output text

# Extract just the password (Linux/Mac)
aws secretsmanager get-secret-value \
  --secret-id ecogallery/dev/rds/master-password \
  --query SecretString --output text | jq -r '.password'

# Extract just the password (Windows PowerShell)
$secret = aws secretsmanager get-secret-value `
  --secret-id ecogallery/dev/rds/master-password `
  --query SecretString --output text | ConvertFrom-Json
$secret.password
```

## Cost

AWS Secrets Manager pricing (as of 2024):
- **$0.40 per secret per month**
- **$0.05 per 10,000 API calls**

Example costs:
- 1 secret + rotation (monthly): **~$0.40**
- 100K API calls: **$0.50**
- **Total: ~$1/month per secret**

For comparison, storing in environment variables is free but:
- ❌ No rotation
- ❌ No encryption
- ❌ No audit trail
- ❌ Visible in process list

## Troubleshooting

### Connection Fails After Rotation

```bash
# Check current secret version
aws secretsmanager describe-secret \
  --secret-id ecogallery/dev/rds/master-password

# Test database connection with secret
SECRET=$(aws secretsmanager get-secret-value \
  --secret-id ecogallery/dev/rds/master-password \
  --query SecretString --output text)

HOST=$(echo $SECRET | jq -r '.host')
PASSWORD=$(echo $SECRET | jq -r '.password')

psql -h $HOST -U postgres -d gmpictures
```

### Lambda Rotation Fails

```bash
# Check Lambda logs
aws logs tail /aws/lambda/ecogallery-rds-rotation-dev --follow

# Common issues:
# 1. Lambda can't reach RDS (check security groups)
# 2. Lambda can't reach Secrets Manager (check VPC endpoints or NAT)
# 3. Current password is wrong (check RDS)
```

### Force Password Reset

If rotation fails and you need to reset manually:

```bash
# 1. Update password in RDS
psql -h your-rds-endpoint -U postgres -d gmpictures
ALTER USER postgres WITH PASSWORD 'your-new-password';

# 2. Update secret in Secrets Manager
aws secretsmanager update-secret \
  --secret-id ecogallery/dev/rds/master-password \
  --secret-string "{\"username\":\"postgres\",\"password\":\"your-new-password\",\"engine\":\"postgres\",\"host\":\"your-rds-endpoint\",\"port\":5432,\"dbname\":\"gmpictures\"}"
```

## Disable Rotation

If you need to disable automatic rotation:

```bash
# In terraform.dev.tfvars, set:
enable_password_rotation = false

# Then apply:
terraform apply -var-file="terraform.dev.tfvars"
```

## Security Best Practices

✅ **Do:**
- Use IAM roles, not access keys
- Enable CloudTrail to audit secret access
- Use VPC endpoints for Secrets Manager (no internet traffic)
- Rotate passwords regularly (30-90 days)
- Use separate secrets for dev/staging/prod

❌ **Don't:**
- Store secrets in code or version control
- Share secrets across environments
- Use overly broad IAM permissions
- Disable rotation in production

## References

- [AWS Secrets Manager Documentation](https://docs.aws.amazon.com/secretsmanager/)
- [Rotating RDS Database Credentials](https://docs.aws.amazon.com/secretsmanager/latest/userguide/rotating-secrets-rds.html)
- [.NET SDK for Secrets Manager](https://docs.aws.amazon.com/sdk-for-net/v3/developer-guide/csharp_secrets-manager_code_examples.html)
