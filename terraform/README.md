# EcoGallery AWS RDS PostgreSQL Terraform Configuration

This directory contains Terraform configurations for deploying PostgreSQL on AWS RDS for the EcoGallery project.

## Files

- **main.tf** - RDS instance, security groups, parameter groups, monitoring, and CloudWatch logging
- **variables.tf** - Input variables with validation and defaults
- **outputs.tf** - Output values for connection details and configuration
- **terraform.dev.tfvars.example** - Development environment example
- **terraform.prod.tfvars.example** - Production environment example

## Prerequisites

1. **AWS Account** with appropriate permissions
2. **Terraform** (v1.0+)
   ```bash
   # Install Terraform: https://www.terraform.io/downloads
   terraform version
   ```

3. **AWS CLI** configured with credentials
   ```bash
   aws configure
   aws sts get-caller-identity  # Verify credentials
   ```

4. **VPC and Network Setup**:
   - VPC ID
   - At least 2 subnets in different Availability Zones for the DB subnet group
   - Application security group IDs

## Setup Steps

### 1. Prepare Configuration Files

```bash
cd terraform

# Copy example files and customize
cp terraform.dev.tfvars.example terraform.dev.tfvars
# Edit terraform.dev.tfvars with your values
```

Edit `terraform.dev.tfvars` with:
- Your VPC ID
- Your subnet IDs (at least 2, different AZs)
- Your application security group IDs
- Your admin IP address (for database access)

### 2. Set Database Password

**IMPORTANT**: Never commit the actual password to version control.

Option A: Environment variable (recommended)
```bash
export TF_VAR_db_password="YourSecurePassword123!@#"
```

Option B: Add to tfvars file (NOT recommended, remember to .gitignore)
```bash
echo 'db_password = "YourSecurePassword123!@#"' >> terraform.dev.tfvars
```

Password requirements:
- Minimum 16 characters
- Mix of uppercase, lowercase, numbers, and special characters
- Avoid special characters: `\ ' " @`

### 3. Initialize Terraform

```bash
terraform init
```

This downloads AWS provider plugins and initializes the working directory.

### 4. Plan the Deployment

```bash
# For development
terraform plan -var-file="terraform.dev.tfvars" -out=tfplan

# For production
terraform plan -var-file="terraform.prod.tfvars" -out=tfplan
```

Review the output to ensure all resources are created as expected.

### 5. Apply Configuration

```bash
# For development
terraform apply tfplan

# Or apply directly
terraform apply -var-file="terraform.dev.tfvars"
```

This will create:
- RDS PostgreSQL instance
- Security groups
- DB subnet group
- Parameter group
- CloudWatch log group
- IAM roles for monitoring

### 6. Retrieve Connection Details

After successful deployment:

```bash
# Get all outputs
terraform output

# Get specific outputs
terraform output rds_endpoint
terraform output appsettings_database_config
```

## Updating Application Configuration

After Terraform creates the RDS instance, update your application's `appsettings.json`:

```json
{
  "Database": {
    "Host": "ecogallery-dev.c9akciq32.us-east-1.rds.amazonaws.com",
    "Port": 5432,
    "Database": "gmpictures",
    "Username": "postgres",
    "Password": "YOUR_PASSWORD",
    "SslMode": "Require"
  }
}
```

Get the values from:
```bash
terraform output appsettings_database_config
terraform output rds_address
terraform output rds_port
```

## Database Migration

After RDS is created, run your SQL migrations:

```bash
# Using psql (install if needed)
psql -h <rds_endpoint> -U postgres -d gmpictures -f GalleryLib/db/db.sql
psql -h <rds_endpoint> -U postgres -d gmpictures -f GalleryLib/db/get_album_content_hierarchical_by_id.sql
```

Or use your application's migration tools.

## Destroying Resources

⚠️ **WARNING**: This will delete the database and cannot be undone.

```bash
# List resources to be destroyed
terraform plan -destroy -var-file="terraform.dev.tfvars"

# Destroy
terraform destroy -var-file="terraform.dev.tfvars"
```

For production, deletion protection is enabled by default. You must disable it first:

```bash
# Disable protection first
terraform apply -var-file="terraform.prod.tfvars" -var="enable_deletion_protection=false"

# Then destroy
terraform destroy -var-file="terraform.prod.tfvars"
```

## Backup and Recovery

### Automated Backups
- Development: 7 days retention
- Production: 30 days retention

View backups in AWS Console:
- RDS → Databases → Automated backups

### Manual Snapshots
```bash
# Create a snapshot
aws rds create-db-snapshot \
  --db-instance-identifier ecogallery-dev \
  --db-snapshot-identifier ecogallery-dev-manual-$(date +%Y%m%d)

# List snapshots
aws rds describe-db-snapshots --db-instance-identifier ecogallery-dev
```

### Restore from Snapshot
```bash
aws rds restore-db-instance-from-db-snapshot \
  --db-instance-identifier ecogallery-dev-restored \
  --db-snapshot-identifier ecogallery-dev-manual-20240101
```

## Monitoring

### CloudWatch Logs
Logs are automatically sent to `/aws/rds/ecogallery-{environment}`:

```bash
aws logs describe-log-groups --query 'logGroups[?contains(logGroupName, `ecogallery`)]'

aws logs tail /aws/rds/ecogallery-dev --follow
```

### Enhanced Monitoring
RDS metrics are available in CloudWatch:
- CPU utilization
- Database connections
- IOPS
- Storage usage
- Query performance

### CloudWatch Alarms (Optional)
Create alarms for production:

```bash
aws cloudwatch put-metric-alarm \
  --alarm-name ecogallery-cpu-high \
  --alarm-description "Alert when CPU > 80%" \
  --metric-name CPUUtilization \
  --namespace AWS/RDS \
  --statistic Average \
  --period 300 \
  --threshold 80 \
  --comparison-operator GreaterThanThreshold \
  --dimensions Name=DBInstanceIdentifier,Value=ecogallery-production
```

## Security Best Practices

✅ **Implemented in this configuration:**
- ✓ Encryption at rest (enabled)
- ✓ Encryption in transit (SSL/TLS required)
- ✓ Security group restricts access to application servers only
- ✓ No public internet access (production)
- ✓ Multi-AZ for production (automatic failover)
- ✓ Automated backups
- ✓ IAM roles for monitoring
- ✓ Deletion protection (production)

📋 **Additional recommendations:**
- **Use AWS Secrets Manager for password rotation** - [See detailed guide](SECRETS_MANAGER.md)
- Enable parameter group for query logging in production
- Set up CloudWatch alarms for critical metrics
- Regularly test restore procedures
- Use IAM database authentication instead of passwords (advanced)

## Troubleshooting

### Connection Refused
```bash
# Check security group rules
aws ec2 describe-security-groups --group-ids sg-xxxxxxxx

# Test connectivity from application server
psql -h <rds_endpoint> -U postgres -d gmpictures
```

### Database Not Found
```bash
# List databases
psql -h <rds_endpoint> -U postgres -l
```

### Terraform State Issues
```bash
# View current state
terraform state list

# Refresh state
terraform refresh -var-file="terraform.dev.tfvars"
```

### Destroy Fails
```bash
# Remove deletion protection
aws rds modify-db-instance \
  --db-instance-identifier ecogallery-dev \
  --no-deletion-protection \
  --apply-immediately

# Then run terraform destroy
```

## Remote State Management (Optional)

For team collaboration, use S3 + DynamoDB for state:

```bash
# Uncomment and configure the backend block in main.tf
# Then initialize:
terraform init
```

See [Terraform State](https://www.terraform.io/language/state) for details.

## Cost Estimation

```bash
# Estimate costs before applying
terraform plan -var-file="terraform.dev.tfvars"

# Use AWS Pricing Calculator: https://calculator.aws/
```

Typical costs (per month):
- **Development** (db.t3.micro): $15-20
- **Production** (db.t3.small, Multi-AZ): $50-80

## Support and Documentation

- [AWS RDS PostgreSQL](https://docs.aws.amazon.com/rds/latest/Userguide/CHAP_PostgreSQL.html)
- [Terraform AWS Provider RDS](https://registry.terraform.io/providers/hashicorp/aws/latest/docs/resources/db_instance)
- [PostgreSQL Security](https://www.postgresql.org/docs/current/sql-syntax.html)

## License

This configuration is part of the EcoGallery project.
