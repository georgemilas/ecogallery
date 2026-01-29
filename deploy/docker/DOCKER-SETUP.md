# EcoGallery Docker Deployment Guide

Quick and easy deployment of EcoGallery using Docker. Perfect for local self-hosting!

## Deployment Options

**Option 1: Build from Source** (this guide)
- Download full source code
- Build and customize

## Prerequisites

- **Docker Desktop** installed ([Download](https://www.docker.com/products/docker-desktop/))
  - Windows: Docker Desktop for Windows
  - Mac: Docker Desktop for Mac
  - Linux: Docker Engine + Docker Compose

## Quick Start - Build from Source

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

# Linux/Mac
cp .env.example .env
nano .env
```

**Required Changes in `.env`:**

**⚠️ You MUST change these values before starting!**

```env
# REQUIRED: Generate a secure random string https://www.guidgenerator.com/
API_KEY=CHANGE_ME_TO_A_SECURE_RANDOM_STRING

#this is a passsword for the database where the gallery will be built
POSTGRES_PASSWORD=CHANGE_ME_TO_SECURE_DB_PASSWORD

#this is the initial admin user for the gallery 
#login user: admin, password: this value
ADMIN_PASSWORD=CHANGE_ME_TO_SECURE_ADMIN_PASSWORD

# Set the path to your pictures folder
# Windows: Use forward slashes e.g., C:/Users/YourName/Pictures
# Linux/Mac: Use absolute path e.g., /home/yourname/pictures
# Use quotes if the path contains spaces "C:/Users/George Milas/Pictures"
PICTURES_PATH=C:/Users/YourName/Pictures
```

### 3. Build the Application

```bash
docker-compose build 
```

This will:
- ✅ Download everything needed
- ✅ Compile the code and create executables

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

### 6. Sync Your Pictures

**One-time sync (for initial setup):**
```bash
docker-compose run sync
```

The sync command runs continuously until you stop it (Ctrl+C). For initial setup, this may take a while depending on the volume of pictures to process.

This will:
- ✅ Scan all images and videos in your pictures directory
- ✅ Generate thumbnails (400px and HD)
- ✅ Extract metadata (EXIF, GPS, camera info)
- ✅ Organize into albums by folders
- ✅ Once done it will re-scan automatically every 2 minutes unless you stop it

### Virtual Albums (Dynamic Albums from YAML)

Create dynamic albums based on expressions or folders defined in a YAML file:


**1. Create your virtual albums (public albums) YAML file** (e.g., `virtual_albums.yml`):

Virtual albums use album names as keys with the following properties:
- **expression**: Query expression to match pictures (supports AND, OR, NOT, regex)
- **folder**: Specific folder path to include (you would use either a folder or and expression not both)
- **description**: Album description (optional)
- **feature**: Featured image path (optional)
- **parent**: Parent album name for hierarchical organization (optional)


Example `virtual_albums.yml`:
```yaml
Pictures Gallery:
  description: Welcome to my gallery
  folder: /public
  feature: /public/IMG_8337.jpg

Barcelona:
  parent: Pictures Gallery
  description: From the 2023 trip to Barcelona
  expression: barcelona and (7939 8024 8004 981 939 883 818 787 781)
  feature: \2023\Barcelona\_IMG_7939.jpg

2024:
  parent: Pictures Gallery
  description: Selections from 2024 trips
  expression: 2024/ and not (colorado eclipse _8940 _8881)
  feature: \2024\best\_MG_2981.jpg

Colorado:
  parent: 2024
  description: Selections from Colorado trip
  expression: 2024/ and colorado
  feature: \2024\Colorado\_MG_2981-Pano.jpg
```

**Expression syntax:**
- `AND`, `OR`, `NOT` - Logical operators
-  space is equivalent to using OR so 'colorado barcelona' is equivalent to 'colorado or barcelona' 
- Parentheses for grouping
- Text matching (parts of filenames or paths)
- Use quotes for "" for files that contains spaces for example "img 23.jpg" or simply "img 23"

**2. Set the path in `.env`:**
```env
VALBUM_YAML=./virtual_albums.yml
```

**3. Load virtual albums:**
```bash
docker-compose run valbum
```


### 7. Access the Application

**Start the gallery app:**
```bash
docker-compose up -d
```

Open your browser and go to:
**http://localhost**


## Management Commands
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
docker-compose logs -f sync

# Stop continuous sync
docker-compose stop sync
```




### Cleanup Service 

The cleanup service removes orphaned thumbnails and database entries for deleted pictures. 
This can happen if you delete pictures while the sync process is no running:

```bash
# Start continuous cleanup service (runs every 2 minutes)
docker-compose up cleanup -d

# View cleanup logs
docker-compose logs -f cleanup

# Stop cleanup service
docker-compose stop cleanup
```




## Troubleshooting

### Port Already in Use

If port 80 is already in use, change it in `.env`:

```env
HTTP_PORT=8080
```

Then access the app at `http://localhost:8080`

### Pictures Not Showing - Check the path in `.env`:**
  - Must be an absolute path
  - Windows: Use forward slashesand put it in quotes if it contain spaces ("C:/Users/...")


### Clear Everything and Start Fresh

```bash
# Stop and remove all containers, volumes, and images
docker-compose down -v
docker-compose up -d

# Re-initialize database (uses your ADMIN_PASSWORD from .env)
docker-compose run --rm service create-db
docker-compose run --rm service sync /pictures
```

## Updating the Application

```bash
# Pull latest changes
git pull

# Rebuild containers
docker-compose up -d --build
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
docker-compose down --rmi all

# Your pictures directory is NOT touched
# Your .env configuration is preserved
```
