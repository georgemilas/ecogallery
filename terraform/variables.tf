variable "aws_region" {
  type        = string
  description = "AWS region for RDS deployment"
  default     = "us-east-1"
}

variable "environment" {
  type        = string
  description = "Environment name (dev, staging, production)"
  default     = "dev"

  validation {
    condition     = contains(["dev", "staging", "production"], var.environment)
    error_message = "Environment must be dev, staging, or production."
  }
}

# VPC and Network Configuration
variable "vpc_id" {
  type        = string
  description = "VPC ID where RDS will be deployed"
}

variable "db_subnet_ids" {
  type        = list(string)
  description = "List of subnet IDs for DB subnet group (must be in different AZs for multi-az)"
}

variable "app_security_group_ids" {
  type        = list(string)
  description = "Security group IDs of application servers that need database access"
}

variable "allowed_admin_ips" {
  type        = list(string)
  description = "CIDR blocks allowed for administrative database access"
  default     = []
}

variable "publicly_accessible" {
  type        = bool
  description = "Make RDS publicly accessible (not recommended for production)"
  default     = false
}

# Database Configuration
variable "postgres_version" {
  type        = string
  description = "PostgreSQL version"
  default     = "15"

  validation {
    condition     = contains(["13", "14", "15", "16"], var.postgres_version)
    error_message = "PostgreSQL version must be 13, 14, 15, or 16."
  }
}

variable "db_name" {
  type        = string
  description = "Initial database name"
  default     = "gmpictures"
}

variable "db_username" {
  type        = string
  description = "Master username for database"
  default     = "postgres"
  sensitive   = true
}

variable "db_password" {
  type        = string
  description = "Master password for database (use environment variable: TF_VAR_db_password)"
  sensitive   = true

  validation {
    condition     = length(var.db_password) >= 16
    error_message = "Database password must be at least 16 characters long."
  }
}

# RDS Instance Configuration
variable "db_instance_class" {
  type        = string
  description = "RDS instance type"
  
  default = "db.t3.micro"

  validation {
    condition     = can(regex("^db\\.[a-z0-9]+\\.[a-z0-9]+$", var.db_instance_class))
    error_message = "Invalid instance class format."
  }
}

variable "db_allocated_storage" {
  type        = number
  description = "Allocated storage in GB"
  default     = 20

  validation {
    condition     = var.db_allocated_storage >= 20 && var.db_allocated_storage <= 65536
    error_message = "Allocated storage must be between 20 and 65536 GB."
  }
}

variable "db_iops" {
  type        = number
  description = "IOPS for gp3 storage (only applies to gp3)"
  default     = 3000

  validation {
    condition     = var.db_iops >= 3000 && var.db_iops <= 64000
    error_message = "IOPS must be between 3000 and 64000."
  }
}

variable "db_max_connections" {
  type        = number
  description = "Maximum number of database connections"
  default     = 100
}

# Backup and Maintenance
variable "backup_retention_days" {
  type        = number
  description = "Number of days to retain automated backups"
  default     = 7

  validation {
    condition     = var.backup_retention_days >= 1 && var.backup_retention_days <= 35
    error_message = "Backup retention must be between 1 and 35 days."
  }
}

variable "log_retention_days" {
  type        = number
  description = "CloudWatch log retention in days"
  default     = 7
}

variable "enable_multi_az" {
  type        = bool
  description = "Enable Multi-AZ deployment"
  default     = true
}

variable "enable_deletion_protection" {
  type        = bool
  description = "Enable deletion protection"
  default     = true
}

# AWS Secrets Manager Configuration
variable "enable_password_rotation" {
  type        = bool
  description = "Enable automatic password rotation via AWS Secrets Manager"
  default     = false
}

variable "password_rotation_days" {
  type        = number
  description = "Number of days between automatic password rotations"
  default     = 30

  validation {
    condition     = var.password_rotation_days >= 1 && var.password_rotation_days <= 365
    error_message = "Password rotation days must be between 1 and 365."
  }
}

variable "lambda_subnet_ids" {
  type        = list(string)
  description = "Subnet IDs for Lambda rotation function (should have access to RDS and Secrets Manager endpoint)"
  default     = []
}

# Environment-specific defaults
locals {
  environment_defaults = {
    dev = {
      db_instance_class       = "db.t3.micro"
      db_allocated_storage    = 20
      backup_retention_days   = 7
      enable_multi_az         = false
      enable_deletion_protection = false
      publicly_accessible     = true
    }
    staging = {
      db_instance_class       = "db.t3.small"
      db_allocated_storage    = 50
      backup_retention_days   = 14
      enable_multi_az         = true
      enable_deletion_protection = true
      publicly_accessible     = false
    }
    production = {
      db_instance_class       = "db.t3.small"
      db_allocated_storage    = 100
      backup_retention_days   = 30
      enable_multi_az         = true
      enable_deletion_protection = true
      publicly_accessible     = false
    }
  }
}
