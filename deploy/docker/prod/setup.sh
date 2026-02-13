#!/bin/bash
set -e

echo "======================================"
echo "  EcoGallery - First Time Setup"
echo "======================================"
echo ""
echo "This setup uses pre-built Docker images."
echo "No source code or build tools required."
echo ""

cd "$(dirname "$0")"

if [ ! -f docker-compose.yml ]; then
    echo "ERROR: docker-compose.yml not found!"
    echo "Download it from the EcoGallery releases page."
    exit 1
fi

# ---- .env file setup ----
if [ ! -f .env ]; then
    if [ ! -f .env.example ]; then
        echo "ERROR: .env.example not found!"
        echo "Download it from the EcoGallery releases page."
        exit 1
    fi
    echo "No .env file found. Creating from .env.example..."
    cp .env.example .env
fi

# Check if API_KEY needs to be generated
EXISTING_API_KEY=$(grep '^API_KEY=' .env 2>/dev/null | cut -d'=' -f2)
if [ "${EXISTING_API_KEY}" = "CHANGE_ME_TO_A_SECURE_RANDOM_STRING" ] || [ -z "${EXISTING_API_KEY}" ]; then
    API_KEY=$(cat /proc/sys/kernel/random/uuid | tr -d '-')$(cat /proc/sys/kernel/random/uuid | tr -d '-')
    if grep -q '^API_KEY=' .env; then
        sed -i "s|API_KEY=.*|API_KEY=${API_KEY}|" .env
    else
        echo "API_KEY=${API_KEY}" >> .env
    fi
    echo "Generated API_KEY automatically."
else
    echo "Using existing API_KEY from .env"
fi

# Check if POSTGRES_PASSWORD needs to be generated
EXISTING_DB_PASS=$(grep '^POSTGRES_PASSWORD=' .env 2>/dev/null | cut -d'=' -f2)
if [ "${EXISTING_DB_PASS}" = "CHANGE_ME_TO_SECURE_DB_PASSWORD" ] || [ -z "${EXISTING_DB_PASS}" ]; then
    DB_PASS=$(cat /proc/sys/kernel/random/uuid | tr -d '-')
    if grep -q '^POSTGRES_PASSWORD=' .env; then
        sed -i "s|POSTGRES_PASSWORD=.*|POSTGRES_PASSWORD=${DB_PASS}|" .env
    else
        echo "POSTGRES_PASSWORD=${DB_PASS}" >> .env
    fi
    echo "Generated POSTGRES_PASSWORD automatically."
else
    echo "Using existing POSTGRES_PASSWORD from .env"
fi

# Check if ADMIN_PASSWORD needs to be prompted
EXISTING_ADMIN_PASS=$(grep '^ADMIN_PASSWORD=' .env 2>/dev/null | cut -d'=' -f2)
if [ "${EXISTING_ADMIN_PASS}" = "CHANGE_ME_TO_SECURE_ADMIN_PASSWORD" ] || [ -z "${EXISTING_ADMIN_PASS}" ]; then
    while true; do
        read -sp "Enter admin password: " ADMIN_PASS
        echo ""
        read -sp "Confirm admin password: " ADMIN_PASS_CONFIRM
        echo ""
        if [ "${ADMIN_PASS}" = "${ADMIN_PASS_CONFIRM}" ] && [ -n "${ADMIN_PASS}" ]; then
            break
        fi
        echo "Passwords do not match or are empty. Try again."
    done
    if grep -q '^ADMIN_PASSWORD=' .env; then
        sed -i "s|ADMIN_PASSWORD=.*|ADMIN_PASSWORD=${ADMIN_PASS}|" .env
    else
        echo "ADMIN_PASSWORD=${ADMIN_PASS}" >> .env
    fi
else
    echo "Using existing ADMIN_PASSWORD from .env"
fi

# Check if PICTURES_PATH needs to be prompted
EXISTING_PICS_PATH=$(grep '^PICTURES_PATH=' .env 2>/dev/null | cut -d'=' -f2)
if [ "${EXISTING_PICS_PATH}" = "/path/to/your/pictures" ] || [ -z "${EXISTING_PICS_PATH}" ]; then
    while true; do
        read -p "Enter absolute path to your pictures directory: " PICS_PATH
        if [ -d "${PICS_PATH}" ]; then
            break
        fi
        echo "Directory '${PICS_PATH}' does not exist. Try again."
    done
    if grep -q '^PICTURES_PATH=' .env; then
        sed -i "s|PICTURES_PATH=.*|PICTURES_PATH=${PICS_PATH}|" .env
    else
        echo "PICTURES_PATH=${PICS_PATH}" >> .env
    fi
else
    echo "Using existing PICTURES_PATH from .env"
fi
echo ""
echo "Configuration saved to .env"
echo "You can edit .env later to customize other settings (email, filtering, etc.)"
echo ""

# Load .env
echo "Loading environment from .env..."
set -a
source .env
set +a

# ---- Validate required variables ----
HAS_ERROR=0

if [ "${API_KEY}" = "CHANGE_ME_TO_A_SECURE_RANDOM_STRING" ] || [ -z "${API_KEY}" ]; then
    echo "ERROR: API_KEY is not set or still using placeholder value!"
    HAS_ERROR=1
fi

if [ "${POSTGRES_PASSWORD}" = "CHANGE_ME_TO_SECURE_DB_PASSWORD" ] || [ -z "${POSTGRES_PASSWORD}" ]; then
    echo "ERROR: POSTGRES_PASSWORD is not set or still using placeholder value!"
    HAS_ERROR=1
fi

if [ "${ADMIN_PASSWORD}" = "CHANGE_ME_TO_SECURE_ADMIN_PASSWORD" ] || [ -z "${ADMIN_PASSWORD}" ]; then
    echo "ERROR: ADMIN_PASSWORD is not set or still using placeholder value!"
    HAS_ERROR=1
fi

if [ "${PICTURES_PATH}" = "/path/to/your/pictures" ] || [ -z "${PICTURES_PATH}" ]; then
    echo "ERROR: PICTURES_PATH is not set or still using placeholder value!"
    HAS_ERROR=1
fi

if [ $HAS_ERROR -ne 0 ]; then
    echo ""
    echo "Please fix the errors above in .env and run this script again."
    exit 1
fi

echo "Configuration validated."
echo ""

# ---- Pull pre-built images ----
echo "======================================"
echo "  Pulling Docker images..."
echo "======================================"
docker compose pull
echo ""
echo "Images pulled successfully."
echo ""

# ---- Initialize database ----
echo "======================================"
echo "  Initializing database..."
echo "======================================"
echo ""
echo "Starting PostgreSQL..."
docker compose up -d postgres
echo "Waiting for PostgreSQL to be healthy..."
until docker compose exec postgres pg_isready -U "${POSTGRES_USER:-postgres}" > /dev/null 2>&1; do
    sleep 2
done
echo "PostgreSQL is ready."
echo ""

echo "Creating database schema..."
docker compose run --rm service create-db -pw "${ADMIN_PASSWORD}"
echo ""
echo "Database initialized."
echo ""

# ---- Run initial sync (foreground) ----
echo "======================================"
echo "  Starting initial sync..."
echo "======================================"
echo "This will scan your pictures directory and populate the database."
echo "Depending on the size of your collection, this may take a while."
echo "Press Ctrl+C to stop (you can resume later with: docker compose run --rm service sync -f /pictures)"
echo ""
docker compose run --rm service sync -f /pictures

echo ""
echo "======================================"
echo "  Setup Complete!"
echo "======================================"
echo ""
echo "Admin credentials:"
echo "  Username: admin"
echo "  Password: (the password you entered above)"
echo ""
echo "To start the application run:"
echo "  ./start.sh"
echo "======================================"