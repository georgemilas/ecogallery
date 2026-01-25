#!/bin/bash
set -e

echo "=================================="
echo "EcoGallery Database Initialization"
echo "=================================="

# Load .env file if it exists
if [ -f .env ]; then
    echo "Loading environment from .env file..."
    set -a
    source .env
    set +a
    echo ""
else
    echo "❌ ERROR: .env file not found!"
    echo "Please copy .env.example to .env and configure it"
    exit 1
fi

# Validate required environment variables
if [ "${API_KEY}" = "CHANGE_ME_TO_A_SECURE_RANDOM_STRING" ] || [ -z "${API_KEY}" ]; then
    echo "❌ ERROR: API_KEY is not set or still using placeholder value!"
    echo "Please edit .env file and set a secure API_KEY"
    exit 1
fi

if [ "${POSTGRES_PASSWORD}" = "CHANGE_ME_TO_SECURE_DB_PASSWORD" ] || [ -z "${POSTGRES_PASSWORD}" ]; then
    echo "❌ ERROR: POSTGRES_PASSWORD is not set or still using placeholder value!"
    echo "Please edit .env file and set a secure POSTGRES_PASSWORD"
    exit 1
fi

if [ "${ADMIN_PASSWORD}" = "CHANGE_ME_TO_SECURE_ADMIN_PASSWORD" ] || [ -z "${ADMIN_PASSWORD}" ]; then
    echo "❌ ERROR: ADMIN_PASSWORD is not set or still using placeholder value!"
    echo "Please edit .env file and set a secure ADMIN_PASSWORD"
    exit 1
fi

if [ "${PICTURES_PATH}" = "/path/to/your/pictures" ] || [ -z "${PICTURES_PATH}" ]; then
    echo "❌ ERROR: PICTURES_PATH is not set or still using placeholder value!"
    echo "Please edit .env file and set the path to your pictures directory"
    exit 1
fi

echo "✅ Configuration validated"
echo ""

# Wait for PostgreSQL to be ready (already handled by healthcheck, but double-check)
echo "Waiting for PostgreSQL to be ready..."
sleep 2

# Create database schema with custom admin password
echo "Creating database schema..."
echo "Using admin password from ADMIN_PASSWORD environment variable"
docker-compose run --rm service create-db -pw "${ADMIN_PASSWORD}"
if [ $? -ne 0 ]; then
    echo ""
    echo "❌ Database initialization failed!"
    exit 1
fi

echo ""
echo "✅ Database schema created successfully!"
echo ""
echo "=================================="
echo "Admin User Created:"
echo "=================================="
echo "Username: admin"
echo "Password: (your custom ADMIN_PASSWORD)"
echo ""
echo "=================================="
echo "Next Steps:"
echo "=================================="
echo "1. Sync your pictures:"
echo "   docker-compose run --rm service sync /pictures"
echo ""
echo "2. Access the application at http://localhost"
echo "=================================="
