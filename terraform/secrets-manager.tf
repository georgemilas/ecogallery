# AWS Secrets Manager for RDS Password Management
# This adds automatic password rotation and secure secret storage

# Secret for RDS master password
resource "aws_secretsmanager_secret" "rds_master_password" {
  name                    = "ecogallery/${var.environment}/rds/master-password"
  description             = "Master password for EcoGallery RDS PostgreSQL database"
  recovery_window_in_days = var.environment == "production" ? 30 : 7

  tags = {
    Name        = "ecogallery-rds-password"
    Environment = var.environment
    Database    = "ecogallery-${var.environment}"
  }
}

# Store the initial password in Secrets Manager
resource "aws_secretsmanager_secret_version" "rds_master_password" {
  secret_id = aws_secretsmanager_secret.rds_master_password.id
  secret_string = jsonencode({
    username            = var.db_username
    password            = var.db_password
    engine              = "postgres"
    host                = aws_db_instance.ecogallery.address
    port                = aws_db_instance.ecogallery.port
    dbname              = aws_db_instance.ecogallery.db_name
    dbInstanceIdentifier = aws_db_instance.ecogallery.identifier
  })
}

# IAM role for Lambda rotation function
resource "aws_iam_role" "secrets_manager_rotation" {
  name = "ecogallery-secrets-rotation-${var.environment}"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Action = "sts:AssumeRole"
        Effect = "Allow"
        Principal = {
          Service = "lambda.amazonaws.com"
        }
      }
    ]
  })

  tags = {
    Name = "ecogallery-secrets-rotation-role"
  }
}

# Attach AWS managed policy for Lambda basic execution
resource "aws_iam_role_policy_attachment" "lambda_basic_execution" {
  role       = aws_iam_role.secrets_manager_rotation.name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AWSLambdaBasicExecutionRole"
}

# Attach AWS managed policy for Lambda VPC execution (if RDS is in VPC)
resource "aws_iam_role_policy_attachment" "lambda_vpc_execution" {
  role       = aws_iam_role.secrets_manager_rotation.name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AWSLambdaVPCAccessExecutionRole"
}

# Custom policy for Secrets Manager rotation
resource "aws_iam_role_policy" "secrets_manager_rotation_policy" {
  name = "ecogallery-secrets-rotation-policy"
  role = aws_iam_role.secrets_manager_rotation.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Action = [
          "secretsmanager:DescribeSecret",
          "secretsmanager:GetSecretValue",
          "secretsmanager:PutSecretValue",
          "secretsmanager:UpdateSecretVersionStage"
        ]
        Resource = aws_secretsmanager_secret.rds_master_password.arn
      },
      {
        Effect = "Allow"
        Action = [
          "secretsmanager:GetRandomPassword"
        ]
        Resource = "*"
      },
      {
        Effect = "Allow"
        Action = [
          "ec2:CreateNetworkInterface",
          "ec2:DescribeNetworkInterfaces",
          "ec2:DeleteNetworkInterface",
          "ec2:AssignPrivateIpAddresses",
          "ec2:UnassignPrivateIpAddresses"
        ]
        Resource = "*"
      }
    ]
  })
}

# Lambda function for password rotation (uses AWS provided template)
# Note: AWS provides a pre-built rotation function for RDS PostgreSQL
resource "aws_lambda_permission" "allow_secrets_manager" {
  statement_id  = "AllowExecutionFromSecretsManager"
  action        = "lambda:InvokeFunction"
  function_name = aws_lambda_function.rotate_rds_password.function_name
  principal     = "secretsmanager.amazonaws.com"
}

# Lambda function for rotation
resource "aws_lambda_function" "rotate_rds_password" {
  filename         = "lambda_rotation.zip"  # You need to create this
  function_name    = "ecogallery-rds-rotation-${var.environment}"
  role            = aws_iam_role.secrets_manager_rotation.arn
  handler         = "lambda_function.lambda_handler"
  source_code_hash = fileexists("lambda_rotation.zip") ? filebase64sha256("lambda_rotation.zip") : null
  runtime         = "python3.11"
  timeout         = 30

  vpc_config {
    subnet_ids         = var.lambda_subnet_ids
    security_group_ids = [aws_security_group.lambda_rotation.id]
  }

  environment {
    variables = {
      SECRETS_MANAGER_ENDPOINT = "https://secretsmanager.${var.aws_region}.amazonaws.com"
    }
  }

  tags = {
    Name = "ecogallery-rds-rotation"
  }
}

# Security group for Lambda rotation function
resource "aws_security_group" "lambda_rotation" {
  name        = "ecogallery-lambda-rotation-${var.environment}"
  description = "Security group for Lambda rotation function"
  vpc_id      = var.vpc_id

  # Allow Lambda to connect to RDS
  egress {
    from_port   = 5432
    to_port     = 5432
    protocol    = "tcp"
    security_groups = [aws_security_group.rds_sg.id]
    description = "PostgreSQL to RDS"
  }

  # Allow Lambda to reach Secrets Manager
  egress {
    from_port   = 443
    to_port     = 443
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
    description = "HTTPS to AWS services"
  }

  tags = {
    Name = "ecogallery-lambda-rotation-sg"
  }
}

# Update RDS security group to allow Lambda
resource "aws_security_group_rule" "rds_from_lambda" {
  type                     = "ingress"
  from_port                = 5432
  to_port                  = 5432
  protocol                 = "tcp"
  source_security_group_id = aws_security_group.lambda_rotation.id
  security_group_id        = aws_security_group.rds_sg.id
  description              = "PostgreSQL from Lambda rotation function"
}

# Enable automatic rotation (optional)
resource "aws_secretsmanager_secret_rotation" "rds_master_password" {
  count = var.enable_password_rotation ? 1 : 0

  secret_id           = aws_secretsmanager_secret.rds_master_password.id
  rotation_lambda_arn = aws_lambda_function.rotate_rds_password.arn

  rotation_rules {
    automatically_after_days = var.password_rotation_days
  }

  depends_on = [aws_lambda_permission.allow_secrets_manager]
}
