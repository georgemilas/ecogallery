#!/usr/bin/env python3
"""
AWS Lambda function for rotating RDS PostgreSQL passwords in AWS Secrets Manager.
This is a basic implementation - AWS provides official rotation templates.

For production, use AWS's official rotation template:
https://github.com/aws-samples/aws-secrets-manager-rotation-lambdas
"""

import json
import boto3
import psycopg2
import os
from botocore.exceptions import ClientError

def lambda_handler(event, context):
    """
    AWS Secrets Manager rotation handler
    """
    service_client = boto3.client('secretsmanager')
    arn = event['SecretId']
    token = event['ClientRequestToken']
    step = event['Step']

    # Setup the metadata for the secret
    metadata = service_client.describe_secret(SecretId=arn)
    if not metadata['RotationEnabled']:
        raise ValueError(f"Secret {arn} is not enabled for rotation")
    
    versions = metadata['VersionIdsToStages']
    if token not in versions:
        raise ValueError(f"Secret version {token} has no stage for rotation")
    
    if "AWSCURRENT" in versions[token]:
        print(f"Secret version {token} already set as AWSCURRENT")
        return
    elif "AWSPENDING" not in versions[token]:
        raise ValueError(f"Secret version {token} not set as AWSPENDING")

    # Execute the appropriate rotation step
    if step == "createSecret":
        create_secret(service_client, arn, token)
    elif step == "setSecret":
        set_secret(service_client, arn, token)
    elif step == "testSecret":
        test_secret(service_client, arn, token)
    elif step == "finishSecret":
        finish_secret(service_client, arn, token)
    else:
        raise ValueError(f"Invalid step parameter: {step}")


def create_secret(service_client, arn, token):
    """Generate a new password and store it in a new version"""
    
    # Get the current secret
    current_secret = get_secret_dict(service_client, arn, "AWSCURRENT")
    
    # Try to get the pending secret
    try:
        get_secret_dict(service_client, arn, "AWSPENDING", token)
        print(f"createSecret: Successfully retrieved secret for {arn}.")
        return
    except service_client.exceptions.ResourceNotFoundException:
        pass
    
    # Generate a new random password
    passwd_response = service_client.get_random_password(
        PasswordLength=32,
        ExcludeCharacters='/@"\'\\'
    )
    
    # Create new secret version with new password
    current_secret['password'] = passwd_response['RandomPassword']
    
    # Put the secret
    service_client.put_secret_value(
        SecretId=arn,
        ClientRequestToken=token,
        SecretString=json.dumps(current_secret),
        VersionStages=['AWSPENDING']
    )
    
    print(f"createSecret: Successfully created secret for {arn}.")


def set_secret(service_client, arn, token):
    """Set the new password in the database"""
    
    # Get both current and pending secrets
    current_secret = get_secret_dict(service_client, arn, "AWSCURRENT")
    pending_secret = get_secret_dict(service_client, arn, "AWSPENDING", token)
    
    # Connect using the current credentials
    conn = get_connection(current_secret)
    
    try:
        with conn.cursor() as cur:
            # Update the password
            alter_sql = f"ALTER USER {pending_secret['username']} WITH PASSWORD %s"
            cur.execute(alter_sql, (pending_secret['password'],))
            conn.commit()
            print(f"setSecret: Successfully set password for user {pending_secret['username']}")
    finally:
        conn.close()


def test_secret(service_client, arn, token):
    """Test that the new password works"""
    
    # Get the pending secret
    pending_secret = get_secret_dict(service_client, arn, "AWSPENDING", token)
    
    # Try to connect using the new credentials
    conn = get_connection(pending_secret)
    
    try:
        with conn.cursor() as cur:
            cur.execute("SELECT 1")
            result = cur.fetchone()
            if result[0] != 1:
                raise ValueError("testSecret: Failed to validate connection")
        print(f"testSecret: Successfully validated connection with new credentials")
    finally:
        conn.close()


def finish_secret(service_client, arn, token):
    """Finalize the rotation by marking the new secret as current"""
    
    # Get metadata
    metadata = service_client.describe_secret(SecretId=arn)
    current_version = None
    
    for version, stages in metadata["VersionIdsToStages"].items():
        if "AWSCURRENT" in stages:
            if version == token:
                # Already set as current
                print(f"finishSecret: Version {version} already marked as AWSCURRENT")
                return
            current_version = version
            break
    
    # Move the AWSCURRENT stage to the new version
    service_client.update_secret_version_stage(
        SecretId=arn,
        VersionStage="AWSCURRENT",
        MoveToVersionId=token,
        RemoveFromVersionId=current_version
    )
    
    print(f"finishSecret: Successfully moved AWSCURRENT from {current_version} to {token}")


def get_connection(secret_dict):
    """Get a database connection using credentials from secret"""
    
    # Parse SSL mode
    ssl_mode = 'require'  # RDS enforces SSL
    
    conn = psycopg2.connect(
        host=secret_dict['host'],
        port=secret_dict['port'],
        database=secret_dict['dbname'],
        user=secret_dict['username'],
        password=secret_dict['password'],
        sslmode=ssl_mode,
        connect_timeout=5
    )
    
    return conn


def get_secret_dict(service_client, arn, stage, token=None):
    """Get secret value from Secrets Manager"""
    
    required_fields = ['host', 'port', 'dbname', 'username', 'password']
    
    # Get the secret value
    if token:
        secret = service_client.get_secret_value(
            SecretId=arn,
            VersionId=token,
            VersionStage=stage
        )
    else:
        secret = service_client.get_secret_value(
            SecretId=arn,
            VersionStage=stage
        )
    
    secret_dict = json.loads(secret['SecretString'])
    
    # Validate required fields
    for field in required_fields:
        if field not in secret_dict:
            raise KeyError(f"{field} not found in secret {arn}")
    
    return secret_dict
