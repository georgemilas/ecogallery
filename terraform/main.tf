# AWS RDS PostgreSQL Database Configuration
# This Terraform script creates a production-ready PostgreSQL database on AWS RDS

terraform {
  required_version = ">= 1.0"
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
  }
  
  # Uncomment and configure for remote state storage
  # backend "s3" {
  #   bucket         = "your-terraform-state-bucket"
  #   key            = "ecogallery/rds.tfstate"
  #   region         = "us-east-1"
  #   encrypt        = true
  #   dynamodb_table = "terraform-lock"
  # }
}

provider "aws" {
  region = var.aws_region
  
  default_tags {
    tags = {
      Project     = "ecogallery"
      Environment = var.environment
      ManagedBy   = "Terraform"
      CreatedDate = timestamp()
    }
  }
}

# Security Group for RDS
resource "aws_security_group" "rds_sg" {
  name        = "ecogallery-rds-sg-${var.environment}"
  description = "Security group for EcoGallery RDS database"
  vpc_id      = var.vpc_id

  # PostgreSQL from application security group
  ingress {
    from_port       = 5432
    to_port         = 5432
    protocol        = "tcp"
    security_groups = var.app_security_group_ids
    description     = "PostgreSQL from application"
  }

  # Optional: Allow from specific IPs for administrative access
  dynamic "ingress" {
    for_each = var.allowed_admin_ips
    content {
      from_port   = 5432
      to_port     = 5432
      protocol    = "tcp"
      cidr_blocks = [ingress.value]
      description = "PostgreSQL admin access"
    }
  }

  # Allow all outbound traffic
  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
    description = "Allow all outbound"
  }

  tags = {
    Name = "ecogallery-rds-sg"
  }
}

# DB Subnet Group
resource "aws_db_subnet_group" "rds_subnet_group" {
  name       = "ecogallery-db-subnet-${var.environment}"
  subnet_ids = var.db_subnet_ids

  tags = {
    Name = "ecogallery-db-subnet-group"
  }
}

# DB Parameter Group for PostgreSQL customization
resource "aws_db_parameter_group" "postgres_params" {
  name   = "ecogallery-postgres-${var.postgres_version}-${var.environment}"
  family = "postgres${var.postgres_version}"

  # Connection settings
  parameter {
    name  = "max_connections"
    value = var.db_max_connections
  }

  # SSL/TLS enforcement
  parameter {
    name  = "rds.force_ssl"
    value = "1"
  }

  # Query logging (optional, uncomment to enable)
  # parameter {
  #   name  = "log_statement"
  #   value = "all"
  # }

  tags = {
    Name = "ecogallery-postgres-params"
  }
}

# RDS PostgreSQL Instance
resource "aws_db_instance" "ecogallery" {
  identifier     = "ecogallery-${var.environment}"
  engine         = "postgres"
  engine_version = var.postgres_version
  instance_class = var.db_instance_class

  # Storage
  allocated_storage     = var.db_allocated_storage
  storage_type          = "gp3"
  storage_encrypted     = true
  iops                  = var.db_iops

  # Database configuration
  db_name  = var.db_name
  username = var.db_username
  # Password should be provided via environment variable or terraform.tfvars
  password = var.db_password

  # Network configuration
  db_subnet_group_name            = aws_db_subnet_group.rds_subnet_group.name
  vpc_security_group_ids          = [aws_security_group.rds_sg.id]
  publicly_accessible             = var.publicly_accessible
  port                            = 5432

  # Parameter group
  parameter_group_name = aws_db_parameter_group.postgres_params.name

  # Backup configuration
  backup_retention_period = var.backup_retention_days
  backup_window          = "03:00-04:00"
  copy_tags_to_snapshot  = true

  # Maintenance
  maintenance_window       = "sun:04:00-sun:05:00"
  auto_minor_version_upgrade = true

  # Enhanced monitoring
  enabled_cloudwatch_logs_exports = ["postgresql"]
  monitoring_interval             = 60
  monitoring_role_arn             = aws_iam_role.rds_monitoring.arn

  # Multi-AZ for production
  multi_az = var.enable_multi_az

  # Deletion protection
  deletion_protection = var.enable_deletion_protection

  # Skip final snapshot for dev, require for prod
  skip_final_snapshot       = var.environment != "production"
  final_snapshot_identifier = var.environment == "production" ? "ecogallery-final-snapshot-${formatdate("YYYY-MM-DD-hhmm", timestamp())}" : null

  tags = {
    Name = "ecogallery-db-${var.environment}"
  }

  depends_on = [aws_iam_role_policy.rds_monitoring]
}

# IAM Role for RDS Enhanced Monitoring
resource "aws_iam_role" "rds_monitoring" {
  name = "ecogallery-rds-monitoring-${var.environment}"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Action = "sts:AssumeRole"
        Effect = "Allow"
        Principal = {
          Service = "monitoring.rds.amazonaws.com"
        }
      }
    ]
  })
}

resource "aws_iam_role_policy_attachment" "rds_monitoring" {
  role       = aws_iam_role.rds_monitoring.name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AmazonRDSEnhancedMonitoringRole"
}

resource "aws_iam_role_policy" "rds_monitoring" {
  name = "ecogallery-rds-monitoring-policy"
  role = aws_iam_role.rds_monitoring.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Action = [
          "logs:CreateLogGroup",
          "logs:CreateLogStream",
          "logs:PutLogEvents"
        ]
        Resource = "arn:aws:logs:*:*:*"
      }
    ]
  })
}

# CloudWatch Log Group for PostgreSQL
resource "aws_cloudwatch_log_group" "postgres" {
  name              = "/aws/rds/ecogallery-${var.environment}"
  retention_in_days = var.log_retention_days

  tags = {
    Name = "ecogallery-postgres-logs"
  }
}
