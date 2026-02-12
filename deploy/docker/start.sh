#!/bin/bash
set -e

echo "======================================"
echo "  EcoGallery - Start Application"
echo "======================================"
echo ""

cd "$(dirname "$0")"

if [ ! -f .env ]; then
    echo "ERROR: .env file not found!"
    echo "Run ./setup.sh first to initialize the application."
    exit 1
fi

# Generate a new API_KEY for this session
NEW_API_KEY=$(cat /proc/sys/kernel/random/uuid | tr -d '-')$(cat /proc/sys/kernel/random/uuid | tr -d '-')
sed -i "s|^API_KEY=.*|API_KEY=${NEW_API_KEY}|" .env

echo "Generated new API_KEY for this session."
echo ""

# Start the application in detached mode
echo "Starting EcoGallery services..."
docker compose up -d
echo ""

# Show status
docker compose ps
echo ""
echo "======================================"
echo "  EcoGallery is running!"
echo "======================================"
echo ""
if [ -z "${HTTP_PORT}" ] || [ "${HTTP_PORT}" = "80" ]; then
    echo "Access the application at: http://localhost"
else
    echo "Access the application at: http://localhost:${HTTP_PORT}"
fi
echo ""
echo "Useful commands:"
echo "  docker compose logs -f          # View logs"
echo "  docker compose down             # Stop ecogallery"
echo "  docker compose ps               # Check status"
echo "======================================"