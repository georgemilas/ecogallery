# AWS Secrets Manager - Quick Reference

## Quick Commands

### Retrieve Secret
```bash
# Full secret (JSON)
aws secretsmanager get-secret-value \
  --secret-id ecogallery/dev/rds/master-password \
  --query SecretString --output text

# Just the password (Linux/Mac)
aws secretsmanager get-secret-value \
  --secret-id ecogallery/dev/rds/master-password \
  --query SecretString --output text | jq -r '.password'

# Just the password (Windows PowerShell)
$secret = aws secretsmanager get-secret-value `
  --secret-id ecogallery/dev/rds/master-password `
  --query SecretString --output text | ConvertFrom-Json
$secret.password
```

### Manual Rotation
```bash
# Trigger rotation now
aws secretsmanager rotate-secret \
  --secret-id ecogallery/dev/rds/master-password

# Check rotation status
aws secretsmanager describe-secret \
  --secret-id ecogallery/dev/rds/master-password \
  | jq '.RotationEnabled, .LastRotatedDate'
```

### View Rotation Logs
```bash
# Lambda logs
aws logs tail /aws/lambda/ecogallery-rds-rotation-dev --follow

# Recent errors
aws logs filter-log-events \
  --log-group-name /aws/lambda/ecogallery-rds-rotation-dev \
  --filter-pattern "ERROR"
```

### Test Database Connection
```bash
# Using psql with secret
SECRET=$(aws secretsmanager get-secret-value \
  --secret-id ecogallery/dev/rds/master-password \
  --query SecretString --output text)

HOST=$(echo $SECRET | jq -r '.host')
PASS=$(echo $SECRET | jq -r '.password')

PGPASSWORD=$PASS psql -h $HOST -U postgres -d gmpictures -c "SELECT version();"
```

## Terraform Outputs

```bash
# Get secret name
terraform output secrets_manager_name

# Get secret ARN
terraform output secrets_manager_arn

# Get Lambda ARN
terraform output rotation_lambda_arn
```

## Common Issues

### "Password authentication failed"
```bash
# Refresh the secret
aws secretsmanager get-secret-value \
  --secret-id ecogallery/dev/rds/master-password \
  --version-stage AWSCURRENT

# Force rotation
aws secretsmanager rotate-secret \
  --secret-id ecogallery/dev/rds/master-password
```

### Lambda Can't Connect to RDS
```bash
# Check security groups
aws ec2 describe-security-groups \
  --group-ids $(terraform output -raw security_group_id)

# Check Lambda has correct subnets
aws lambda get-function-configuration \
  --function-name ecogallery-rds-rotation-dev \
  | jq '.VpcConfig'
```

### Reset Password Manually
```sql
-- Connect to RDS as master user
ALTER USER postgres WITH PASSWORD 'NewSecurePassword123!';
```

```bash
# Update secret
aws secretsmanager put-secret-value \
  --secret-id ecogallery/dev/rds/master-password \
  --secret-string "{\"username\":\"postgres\",\"password\":\"NewSecurePassword123!\",\"engine\":\"postgres\",\"host\":\"your-host\",\"port\":5432,\"dbname\":\"gmpictures\"}"
```

## C# Code Snippet

```csharp
// Get connection string from Secrets Manager
var secretsManager = serviceProvider.GetRequiredService<SecretsManagerService>();
var connectionString = await secretsManager.GetConnectionStringAsync();

// Use it
using var connection = new NpgsqlConnection(connectionString);
await connection.OpenAsync();
```

## Costs

- **Storage**: $0.40/month per secret
- **API calls**: $0.05 per 10,000 calls
- **Lambda**: Free tier (1M requests/month)

**Typical monthly cost: $0.40 - $1.00**

## Architecture Decision

| Method | Security | Auto-Rotation | Cost | Complexity |
|--------|----------|---------------|------|------------|
| Hardcoded | ❌ Low | ❌ No | ✅ Free | ✅ Simple |
| Env Vars | ⚠️ Medium | ❌ No | ✅ Free | ✅ Simple |
| Secrets Manager | ✅ High | ✅ Yes | ⚠️ $0.40/mo | ⚠️ Medium |
| IAM Auth | ✅ High | ✅ Auto | ✅ Free | ❌ Complex |

**Recommendation**: 
- **Development**: Environment variables or Secrets Manager (no rotation)
- **Production**: Secrets Manager with rotation enabled
