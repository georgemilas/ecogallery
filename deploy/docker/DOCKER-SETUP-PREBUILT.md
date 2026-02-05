# EcoGallery - Pre-Built Docker Image Deployment

**No code download required!** Just use pre-built Docker images.

## Prerequisites

- **Docker Desktop** installed ([Download](https://www.docker.com/products/docker-desktop/))

## Quick Start (3 minutes)

### 1. Download Configuration Files

Download these 2 files from the [releases page](YOUR_RELEASE_URL):
- `docker-compose.prod.yml`
- `.env.example`

Or create a folder and download them directly:

```bash
# Create project folder
mkdir ecogallery
cd ecogallery

# Download files (update URL to your actual release)
curl -O https://github.com/YOUR_USERNAME/ecogallery/releases/latest/download/docker-compose.prod.yml
curl -O https://github.com/YOUR_USERNAME/ecogallery/releases/latest/download/.env.example
```

### 2. Configure Environment

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
PICTURES_PATH=C:/Users/YourName/Pictures
```

**Optional: Email Configuration (for password reset)**

To enable password reset emails, add these to your `.env`:

```env
# Auth type: Basic, OAuth2, or SendGrid
# Basic Authentication (most common - use with Gmail App Password)
SMTP_AUTH_TYPE=Basic
SMTP_HOST=smtp.gmail.com
SMTP_PORT=587
SMTP_USER=your-email@gmail.com
SMTP_PASS=your-app-password
SMTP_FROM=your-email@gmail.com
```

**Gmail Setup:** Enable 2-Step Verification, then create App Password at https://myaccount.google.com/apppasswords

**SendGrid (no SMTP required):**
```env
SMTP_AUTH_TYPE=SendGrid
SENDGRID_API_KEY=your-sendgrid-api-key
SENDGRID_FROM=your-verified-from@domain.com
```

### 3. Start the Application

```bash
docker-compose -f docker-compose.prod.yml up -d
```

Docker will:
- ✅ Download pre-built images (one-time download)
- ✅ Start all services
- ✅ Configure networking

**Wait 1-2 minutes for images to download and services to start.**

### 4. Initialize Database

```bash
docker-compose -f docker-compose.prod.yml run --rm service create-db
```

The init script will validate that you've set secure passwords in `.env` and refuse to run if you haven't changed the placeholder values.

### 5. Sync Your Pictures

```bash
docker-compose -f docker-compose.prod.yml run --rm service sync /pictures
```

### 6. Access the Application

Open your browser: **http://localhost**

Login:
- **Username:** `admin`
- **Password:** Your `ADMIN_PASSWORD` from `.env`

## Management Commands

All commands use `-f docker-compose.prod.yml`:

```bash
# View running containers
docker-compose -f docker-compose.prod.yml ps

# Stop the application
docker-compose -f docker-compose.prod.yml down

# View logs
docker-compose -f docker-compose.prod.yml logs -f

# Enable continuous sync (every 2 minutes)
docker-compose -f docker-compose.prod.yml --profile sync up -d sync

# Update to latest version
docker-compose -f docker-compose.prod.yml pull
docker-compose -f docker-compose.prod.yml up -d
```

## Benefits of Pre-Built Images

✅ **No source code needed** - Just configuration files
✅ **Faster deployment** - No building from source
✅ **Smaller download** - Only images you need
✅ **Easy updates** - `docker-compose pull` to update
✅ **Consistent environment** - Same images for everyone

## Custom Image Registry

If images are hosted on a custom registry, add to `.env`:

```env
DOCKER_IMAGE_PREFIX=ghcr.io/yourusername
IMAGE_TAG=v1.0.0
```

Then use specific version:
```bash
docker-compose -f docker-compose.prod.yml up -d
```

## Troubleshooting

### Images Not Found

If you see "image not found" errors:
1. Check that images are published to Docker Hub or your registry
2. Verify `DOCKER_IMAGE_PREFIX` in `.env` matches the registry
3. Ensure you have internet connection to pull images

### Update .env Configuration

After changing `.env`, restart services:
```bash
docker-compose -f docker-compose.prod.yml down
docker-compose -f docker-compose.prod.yml up -d
```

## Full Documentation

For complete documentation, see [DOCKER-SETUP.md](DOCKER-SETUP.md) - all commands work the same, just add `-f docker-compose.prod.yml`.
