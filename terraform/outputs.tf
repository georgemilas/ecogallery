output "rds_endpoint" {
  description = "RDS database endpoint"
  value       = aws_db_instance.ecogallery.endpoint
}

output "rds_address" {
  description = "RDS database hostname (without port)"
  value       = aws_db_instance.ecogallery.address
}

output "rds_port" {
  description = "RDS database port"
  value       = aws_db_instance.ecogallery.port
}

output "rds_resource_id" {
  description = "RDS database resource ID"
  value       = aws_db_instance.ecogallery.resource_id
}

output "rds_arn" {
  description = "ARN of the RDS instance"
  value       = aws_db_instance.ecogallery.arn
}

output "db_name" {
  description = "Database name"
  value       = aws_db_instance.ecogallery.db_name
}

output "db_username" {
  description = "Database master username"
  value       = aws_db_instance.ecogallery.username
  sensitive   = true
}

output "security_group_id" {
  description = "RDS security group ID"
  value       = aws_security_group.rds_sg.id
}

output "db_subnet_group_name" {
  description = "DB subnet group name"
  value       = aws_db_subnet_group.rds_subnet_group.name
}

output "cloudwatch_log_group" {
  description = "CloudWatch log group name"
  value       = aws_cloudwatch_log_group.postgres.name
}

# Connection string for application (requires password)
output "connection_string_template" {
  description = "Connection string template (replace PASSWORD with actual password)"
  value       = "postgresql://${aws_db_instance.ecogallery.username}:PASSWORD@${aws_db_instance.ecogallery.address}:${aws_db_instance.ecogallery.port}/${aws_db_instance.ecogallery.db_name}"
  sensitive   = true
}

# JSON format for appsettings
output "appsettings_database_config" {
  description = "Database configuration for appsettings.json (you need to add the password)"
  value = jsonencode({
    Database = {
      Host     = aws_db_instance.ecogallery.address
      Port     = aws_db_instance.ecogallery.port
      Database = aws_db_instance.ecogallery.db_name
      Username = aws_db_instance.ecogallery.username
      Password = "CHANGE_ME_IN_PRODUCTION"
      SslMode  = "Require"
    }
  })
}

# AWS Secrets Manager outputs
output "secrets_manager_arn" {
  description = "ARN of the Secrets Manager secret"
  value       = aws_secretsmanager_secret.rds_master_password.arn
}

output "secrets_manager_name" {
  description = "Name of the Secrets Manager secret"
  value       = aws_secretsmanager_secret.rds_master_password.name
}

output "rotation_lambda_arn" {
  description = "ARN of the password rotation Lambda function"
  value       = aws_lambda_function.rotate_rds_password.arn
}
