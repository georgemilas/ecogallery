# EcoGallery

**Privacy-focused, self-hosted photo & video gallery for managing massive media collections locally.**

EcoGallery is a self-hosted gallery application designed for photographers and families with large photo/video collections who want full control over their media without relying on cloud services.

**Key Features:**
- 🔒 **Privacy-First** - No cloud dependency, no third-party AI training on your data
- 📁 **Folder-Based Albums** - Automatic album organization from your directory structure
- 🔍 **Powerful Search** - Boolean expression queries with many features including face recognition searh, geolocation search, date, tags etc.  
- 🎨 **Virtual Albums** - Dynamic collections based on search expressions powered by the search mechanism
- ⚡ **Handles Scale** - Proven with 50K+ images (500GB files) 
- 🖼️ **8K Display Support** - Full-resolution viewing 
- 🔄 **Auto-Sync** - Continuous synchronization 

## Quick Overview of how it works 
* ### What
    * each folder is an album so the way you organize the pictures on your harddrive becomes you raw albums data 
        * each album (folder) contains sub albums (sub folders) and images and movies that are directly stored in the folder as its content
        * quicly browse huge folders with thousands of files 
        * view detailed information like metadata, geolocation, people etc.
        * keyboard navigation shortcuts 
    * use the search feature to build virtual albums or just to browse and dicover pictures you forgot you have  
    * user management, public vs private vs client designated catalogs
    * local face recognition and geolocation integration to enhance the search capability and virtual albums  
* ### How
    Point the application at the folder containing pictures        
    * let it run once to creates thumbnails, discover faces etc. and build its local database
    * start the sync background process to continuosly sync the database with changes in the hard drive content (new files, moved files, renamed, deleted etc.)  
* ### Limitaions
    * Its local so you need to run a server to make it available on the internet 
    * Contact me for help    
        
---

## Screenshots

<table>
<tr>
<td width="50%">

### Gallery View
![Gallery](data/gallery.png)
Browse albums and sub-albums, create/edit virtual albums, sort, search, keyboard shortcuts etc.

</td>
<td width="50%">

### Image Viewer
![Viewer](data/gallery-image.png)
Full-screen slideshow, zoom, metadata display, keyboard navigation etc.

</td>
</tr>
</table>

---

# EcoGallery Docker Deployment Guide

## Prerequisites

- **Docker Desktop** installed ([Download](https://www.docker.com/products/docker-desktop/))
  - Windows: Docker Desktop for Windows
  - Mac: Docker Desktop for Mac
  - Linux: Docker Engine + Docker Compose

### 1. Download the Application

```bash
git clone --recursive https://github.com/georgemilas/ecogallery.git
cd ecogallery/deploy/docker
```

### 2. Configuration

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
ADMIN_PASSWORD=CHANGE_ME_TO_SECURE_ADMIN_PASSWORD    #this is the initial admin user for the gallery 
PICTURES_PATH=/path/to/your/pictures                 # Use forward slash to separate folders and quotes if the path contains spaces "C:/Path With Spaces/Pictures"
                                                     # Windows: Use forward slashes e.g., C:/Users/YourName/Pictures
                                                     # Linux/Mac: Use absolute path e.g., /home/yourname/pictures

```

**Optional: Email Configuration (for user management)**

To manage and invite additional users configure email settings:

```env
# Basic Authentication (most common)
SMTP_AUTH_TYPE=Basic
SMTP_HOST=smtp.gmail.com          
SMTP_PORT=587
SMTP_USER=your-email@gmail.com
SMTP_PASS=your-app-password       
SMTP_FROM=your-email@gmail.com
```

**Gmail Setup:**
1. Go to https://myaccount.google.com/security
2. Enable 2-Step Verification
3. Go to https://myaccount.google.com/apppasswords
4. Create App Password for "Mail"
5. Use the generated 16-character password as `SMTP_PASS`

**Additional Info:**
- **Outlook/Office365**: `SMTP_HOST=smtp.office365.com`
- **Yahoo**: `SMTP_HOST=smtp.mail.yahoo.com`
- See more [here](/deploy/docker/DOCKER-SETUP.md)


### 3. Build and run the application

```bash
./init-db.bat               #Windows Only: initialize database and the admin user for the gallery
chmod +x init-db.sh         #Mac/Linux Only: make the script executable
./init-db.sh                #                and then execute 

docker-compose run sync      #run once to sync the database with the pictures folder (may take a while for large folders)
docker-compose run valbum    #run to create (or update) virtual albums (see below)
docker-compose run face      #Optional: run once to pre-discover faces and people (may take a while for large folders)
docker-compose run geo       #Optional: run once to pre-generate geolocation clusters  
docker-compose up -d         #run web app along with continuous sync, face recognition and geolocation clustering in the background
                             #sync is incremental so any new/updated/renamed/deleted/moved files etc. are synced
```


### Virtual Albums 

Virtual albums represent public albums (no login required) generated based on a search expression or a designated folder to be visible publicly.

Virtual albums can be pre-defined in a YAML file and loaded for a quick setup (e.g. `virtual_albums.yml`)
Additionaly you will also be able to use the gallery itself to manage (create/edit) virtual albums.   

Virtual albums have the following properties:
- uses album names as keys
- album keys:
    - **expression**: Query expression to match pictures 
    - **folder**: relative path to an album folder
    - **description**: Album description (optional)
    - **feature**: Featured (album cover) image path (optional)
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
  feature: /2023/Barcelona/_IMG_7939.jpg

2024:
  parent: Pictures Gallery
  description: Selections from 2024 trips
  expression: 2024/ and not (colorado eclipse _8940 _8881)
  feature: /2024/best/_MG_2981.jpg

Colorado:
  parent: 2024
  description: Selections from Colorado trip
  expression: 2024/ and colorado
  feature: /2024/Colorado/_MG_2981-Pano.jpg
```

**Expression syntax:**
- `AND`, `OR`, `NOT` - Logical operators
- searches any part of the full image path or use {regex} {ai:person name} {d>:date} {l:geo location} 
- space is equivalent to using a logical OR so the expression 'colorado barcelona' is equivalent to 'colorado or barcelona' 
- Use parentheses for grouping
- Use quotes to include spaces for example "img 23.jpg" or "img 23"

**Set the path in `.env`:**
```env
VALBUM_YAML=./virtual_albums.yml
```

**Load virtual albums:**
```bash
docker-compose run valbum
```

