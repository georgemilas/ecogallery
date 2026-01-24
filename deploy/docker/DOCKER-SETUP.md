# EcoGallery Docker Deployment Guide

Quick and easy deployment of EcoGallery using Docker. Perfect for local self-hosting!

## Deployment Options

**Option 1: Build from Source** (this guide)
- Download full source code
- Build Docker images locally
- Full control and customization

**Option 2: Pre-Built Images** ([See DOCKER-SETUP-PREBUILT.md](DOCKER-SETUP-PREBUILT.md))
- No source code needed
- Just download 2 config files
- Faster deployment (3 minutes)

## Prerequisites

- **Docker Desktop** installed ([Download](https://www.docker.com/products/docker-desktop/))
  - Windows: Docker Desktop for Windows
  - Mac: Docker Desktop for Mac
  - Linux: Docker Engine + Docker Compose

## Quick Start (5 minutes) - Build from Source

### 1. Download the Application

```bash
git clone --recursive https://github.com/georgemilas/ecogallery.git
cd ecogallery/deploy/docker
```

### 2. Configure Environment

Copy the example environment file and edit it:

```bash
# Windows (Command Prompt)
copy .env.example .env
notepad .env

# Windows (PowerShell)
Copy-Item .env.example .env
notepad .env

# Linux/Mac
cp .env.example .env
nano .env
```

**Required Changes in `.env`:**

**⚠️ CRITICAL: You MUST change these three values before starting!**

```env
# REQUIRED: Change this to a secure random string (minimum 32 characters)
# The init script will refuse to run if you don't change this!
API_KEY=CHANGE_ME_TO_A_SECURE_RANDOM_STRING

# REQUIRED: Set a strong database password
# The init script will refuse to run if you don't change this!
POSTGRES_PASSWORD=CHANGE_ME_TO_SECURE_DB_PASSWORD

# REQUIRED: Set a secure admin user password
# The init script will refuse to run if you don't change this!
ADMIN_PASSWORD=CHANGE_ME_TO_SECURE_ADMIN_PASSWORD

# IMPORTANT: Set the path to your pictures folder
# Windows: Use forward slashes, e.g., C:/Users/YourName/Pictures
# Linux/Mac: Use absolute path, e.g., /home/yourname/pictures
PICTURES_PATH=C:/Users/YourName/Pictures

# OPTIONAL: Set where to store PostgreSQL database files
# If not set, Docker manages storage automatically in its default location
# If set, database files are stored at this path (easier backup/access)
# POSTGRES_DATA_PATH=C:/Users/YourName/ecogallery-data/postgres
```

### 3. Start the Application

```bash
docker-compose up -d
```

This will:
- ✅ Download required images
- ✅ Build your application
- ✅ Start PostgreSQL database
- ✅ Start API and frontend servers
- ✅ Configure nginx reverse proxy

**Wait 1-2 minutes for all services to start.**

### 4. Initialize the Database

**Windows:**
```cmd
init-db.bat
```

**Linux/Mac:**
```bash
chmod +x init-db.sh
./init-db.sh
```

The init script will validate that you've set secure passwords in `.env` and refuse to run if you haven't changed the placeholder values.

**Or manually:**
```bash
docker-compose run --rm service create-db -pw YourSecurePassword
```

### 5. Login with Admin Account

The `create-db` command creates an admin user with the password you set in `ADMIN_PASSWORD`:
- **Username:** `admin`
- **Password:** Your `ADMIN_PASSWORD` from `.env`

### 6. Sync Your Pictures

**One-time sync (for initial setup):**
```bash
docker-compose run --rm service sync /pictures
```

**Note:** `/pictures` is the path INSIDE the container. It automatically maps to your `PICTURES_PATH` from `.env`.

The sync command runs continuously and automatically scans every 2 minutes until you stop it (Ctrl+C). For initial setup, you can let it run one scan cycle then stop it.

This will:
- ✅ Scan all images in your pictures directory
- ✅ Generate thumbnails (400px and HD)
- ✅ Extract metadata (EXIF, GPS, camera info)
- ✅ Organize into albums by date/folder
- ✅ Re-scan automatically every 2 minutes

**OR - Enable continuous sync as a background service (recommended):**

To run sync continuously in the background:
```bash
docker-compose --profile sync up -d sync
```

This runs as a Docker service that:
- Automatically syncs every 2 minutes
- Restarts automatically if it crashes
- Runs in the background

View logs:
```bash
docker-compose logs -f sync
```

Stop continuous sync:
```bash
docker-compose stop sync
```

### 7. Access the Application

Open your browser and go to:

**http://localhost**

Login with the admin credentials:
- **Username:** `admin`
- **Password:** Your `ADMIN_PASSWORD` from `.env`

## Management Commands

### View Running Containers

```bash
docker-compose ps
```

### Stop the Application

```bash
docker-compose down
```

### Restart the Application

```bash
docker-compose up -d
```

### View Logs

```bash
# All services
docker-compose logs -f

# Specific service
docker-compose logs -f api
docker-compose logs -f frontend
docker-compose logs -f nginx
```

### Re-sync Pictures (after adding new photos)

**Option 1: Manual one-time sync**
```bash
docker-compose run --rm service sync /pictures
```
Note: This runs continuously (scans every 2 minutes) until you press Ctrl+C.

**Option 2: Enable background sync service (recommended)**
```bash
# Start continuous sync (syncs every 2 minutes in background)
docker-compose --profile sync up -d sync

# View sync logs 
docker-compose logs -f sync

# Stop continuous sync
docker-compose stop sync
```

### Database Backup

**Method 1: SQL Dump (portable backup)**
```bash
# Export database
docker-compose exec postgres pg_dump -U ecogallery ecogallery > backup.sql

# Import database
docker-compose exec -T postgres psql -U ecogallery ecogallery < backup.sql
```

**Method 2: File System Backup (if using POSTGRES_DATA_PATH)**

If you set `POSTGRES_DATA_PATH` in `.env`, you can backup the entire database by copying that folder:

```bash
# Stop the database first
docker-compose stop postgres

# Copy the folder (Windows PowerShell example)
Copy-Item -Path "C:\Users\YourName\ecogallery-data\postgres" -Destination "C:\Backups\postgres-backup-2026-01-23" -Recurse

# Restart database
docker-compose start postgres
```

This gives you complete control over your database storage location.

### Create Additional Users

Use the web interface to create additional users:
1. Login as admin
2. Go to user management
3. Create new users with appropriate permissions

Alternatively, insert users directly into the database (password must be Base64(SHA256(password))).

### Continuous Picture Syncing

The sync command automatically scans your pictures folder every 2 minutes. You can run it in two ways:

**Background service (recommended for always-on syncing):**

```bash
# Start continuous sync service
docker-compose --profile sync up -d sync

# Check if it's running
docker-compose ps sync

# View sync logs in real-time
docker-compose logs -f sync

# Stop continuous sync
docker-compose stop sync

# Restart continuous sync
docker-compose restart sync
```

The sync service will:
- ✅ Run continuously, scanning every 2 minutes
- ✅ Detect new photos in your pictures folder
- ✅ Generate thumbnails for new images
- ✅ Update the database with new albums
- ✅ Restart automatically if it crashes (Docker restart policy)
- ✅ Log all activity with timestamps

### Cleanup Service (Bidirectional Sync)

The cleanup service removes orphaned thumbnails and database entries for deleted pictures:

```bash
# Start continuous cleanup service (runs every 2 minutes)
docker-compose --profile cleanup up -d cleanup

# View cleanup logs
docker-compose logs -f cleanup

# Stop cleanup service
docker-compose stop cleanup
```

**Or run cleanup manually:**
```bash
docker-compose run --rm service cleanup -f /pictures -h 400 1440 -pl no
```

### Virtual Albums (Dynamic Albums from YAML)

Create dynamic albums based on expressions or folders defined in a YAML file:

**1. Create your virtual albums YAML file** (e.g., `virtual_albums.yml`):

Virtual albums use album names as keys with the following properties:
- **expression**: Query expression to match pictures (supports AND, OR, NOT, wildcards, regex)
- **folder**: Specific folder path to include
- **description**: Album description (optional)
- **feature**: Featured image path (optional)
- **parent**: Parent album name for hierarchical organization (optional)

Example `virtual_albums.yml`:
Example `virtual_albums.yml`:
```yaml
Pictures Gallery:
  description: Welcome to my gallery
  folder: /public
  feature: /public/IMG_8337.jpg

Barcelona:
  parent: Pictures Gallery
  description: From the 2023 trip to Barcelona and Montserrat
  expression: barcelona and (8024 8004 981 939 883 818 787 781)
  feature: \2023\Barcelona\feature__MG_7939.jpg

2024:
  parent: Pictures Gallery
  description: Selections from 2024 trips
  expression: 2024/ and not (eclipse _8940 _8881)
  feature: \2024\Colorado\_MG_2981-Pano-Edit.jpg

Colorado:
  parent: 2024
  description: Selections from Colorado trip
  expression: 2024/ and colorado
  feature: \2024\Colorado\_MG_2981-Pano-Edit.jpg
```

**Expression syntax:**
- `AND`, `OR`, `NOT` - Logical operators
- Parentheses for grouping
- Text matching (filenames, paths, dates)
- Numbers for specific image IDs
- Wildcards and patterns

**2. Set the path in `.env`:**
```env
VALBUM_YAML=./virtual_albums.yml
```

**3. Load virtual albums:**
```bash
docker-compose run --rm valbum valbum -f /pictures -y /config/virtual_albums.yml
```

### All Available Commands

Run any GalleryService command using the `service` container:

```bash
# Sync (thumbnails + database)
docker-compose run --rm service sync /pictures

# Cleanup (remove orphaned files)
docker-compose run --rm service cleanup -f /pictures -h 400 1440 -pl no

# Database sync only
docker-compose run --rm service db -f /pictures

# Thumbnails only
docker-compose run --rm service thumbnails -f /pictures -h 400 1440

# Virtual albums from YAML
docker-compose run --rm service valbum -f /pictures -y /path/to/yaml

# Create database schema
docker-compose run --rm service create-db

# Get help
docker-compose run --rm service --help
```

## Troubleshooting

### Port Already in Use

If port 80 is already in use, change it in `.env`:

```env
HTTP_PORT=8080
```

Then access the app at `http://localhost:8080`

### Pictures Not Showing

1. **Check the path in `.env`:**
   - Must be absolute path
   - Windows: Use forward slashes (C:/Users/...)
   - Verify the path exists and contains images

2. **Check permissions:**
   ```bash
   # Make sure Docker can access the directory
   # Windows: Share the drive in Docker Desktop settings
   # Linux: Ensure proper file permissions
   ```

3. **Re-sync pictures:**
   ```bash
   docker-compose run --rm service sync /pictures
   ```

4. **Run cleanup if pictures were deleted:**
   ```bash
   docker-compose run --rm service cleanup -f /pictures -h 400 1440 -pl no
   ```

### Database Connection Errors

```bash
# Check if PostgreSQL is running
docker-compose ps postgres

# Check PostgreSQL logs
docker-compose logs postgres

# Restart PostgreSQL
docker-compose restart postgres
```

**If using POSTGRES_DATA_PATH:**
- Verify the path exists and Docker has permission to access it
- On Windows, ensure the drive is shared in Docker Desktop settings
- Check folder permissions (must be writable by Docker)
docker-compose restart postgres
```

### Can't Login / Authentication Errors

1. **Verify API_KEY matches in `.env`**
2. **Restart all services:**
   ```bash
   docker-compose down
   docker-compose up -d
   ```

### Clear Everything and Start Fresh

```bash
# Stop and remove all containers, volumes, and images
docker-compose down -v
docker-compose up -d

# Re-initialize database (uses your ADMIN_PASSWORD from .env)
docker-compose run --rm service create-db
docker-compose run --rm service sync /pictures
```

## Architecture

```
┌─────────────────────────────────────────────┐
│  Browser (http://localhost)                 │
└─────────────┬───────────────────────────────┘
              │
              ▼
┌─────────────────────────────────────────────┐
│  Nginx Reverse Proxy (Port 80)              │
│  - Routes API requests to backend           │
│  - Routes app requests to frontend          │
│  - Serves pictures with X-Accel-Redirect    │
└─────┬──────────────────────┬────────────────┘
      │                      │
      ▼                      ▼
┌──────────────┐      ┌──────────────────────┐
│  Frontend    │      │  Backend API         │
│  (Next.js)   │      │  (.NET)              │
│  Port 3000   │      │  Port 5001           │
└──────────────┘      └──────┬───────────────┘
                             │
                             ▼
                      ┌──────────────────────┐
                      │  PostgreSQL          │
                      │  Port 5432           │
                      └──────────────────────┘
```

## Security Notes

- **API Key:** Used to authenticate requests between frontend and backend
- **Database:** Only accessible from within Docker network
- **Pictures:** Served with authentication through nginx
- **Thumbnails:** 400px thumbnails are served directly for performance
- **Full Images:** Protected with X-API-Key authentication
- **Large Files (>10MB):** Use session cookie authentication as fallback

## Updating the Application

```bash
# Pull latest changes
git pull

# Rebuild containers
docker-compose up -d --build

# Database migrations (if any)
docker-compose run --rm service migrate
```

## Resource Usage

**Typical resource consumption:**
- PostgreSQL: ~50-100MB RAM
- Backend API: ~100-200MB RAM
- Frontend: ~100-150MB RAM
- Nginx: ~10-20MB RAM

**Total: ~300-500MB RAM**

## Advanced Configuration

### Custom Nginx Configuration

Edit `nginx.config` and restart:

```bash
docker-compose restart nginx
```

### Custom Database Configuration

Add to `docker-compose.yml` under `postgres` service:

```yaml
command: 
  - "postgres"
  - "-c"
  - "max_connections=200"
  - "-c"
  - "shared_buffers=256MB"
```

### Enable HTTPS

Mount SSL certificates:

```yaml
nginx:
  volumes:
    - ../../nginx.config:/etc/nginx/nginx.conf:ro
    - ./ssl/cert.pem:/etc/ssl/cert.pem:ro
    - ./ssl/key.pem:/etc/ssl/key.pem:ro
  ports:
    - "443:443"
```

Update `nginx.config` to listen on port 443 with SSL.

## Getting Help

1. **Check logs:** `docker-compose logs -f [service-name]`
2. **Verify configuration:** Ensure `.env` file has correct values
3. **Check Docker:** Ensure Docker Desktop is running
4. **Restart services:** `docker-compose restart`

## Uninstall

```bash
# Stop and remove everything
docker-compose down -v

# Remove Docker images (optional)
docker-compose down --rmi all

# Your pictures directory is NOT deleted
# Your .env configuration is preserved
```
