# ecogallery

**Privacy-focused, self-hosted photo & video gallery for managing massive media collections locally.**

EcoGallery is a self-hosted gallery application designed for photographers and families with large photo/video collections who want full control over their media without relying on cloud services.

**Key Features:**
- **Privacy-First** - No cloud dependency, no third-party AI training on your data
- **Folder-Based Albums** - Automatic album organization from your directory structure
- **Powerful Search** - Boolean expression queries with many features including face recognition searh, geolocation search, date, tags etc.  
- **Virtual Albums** - Dynamic collections based on search expressions and more
- **Handles Scale** - Proven with 50K+ images (500GB non raw files) 
- **8K Display Support** - Full-resolution viewing 
- **Auto-Sync** - Continuous synchronization 

## Quick Overview of how it works 
* ### What
    * each folder is an album so the way you organize the pictures on your hard drive becomes your raw albums data 
        * each album (a folder) may contains sub albums (sub folders) and all images and movies on disk represents its content
        * quicly browse and search trough huge folders with thousands of files 
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
---

<table>
<tr>
<td width="50%">

### Gallery View
![Gallery](https://raw.githubusercontent.com/georgemilas/ecogallery/refs/heads/master/data/gallery.png)
Browse albums and sub-albums, create/edit virtual albums, sort, search, keyboard shortcuts etc.

</td>
<td width="50%">

### Image Viewer
![Viewer](https://raw.githubusercontent.com/georgemilas/ecogallery/refs/heads/master/data/gallery-image.png)
Full-screen, slideshow, 1:1 zoom, metadata display, keyboard navigation etc.

</td>
</tr>
</table>

---

# ecogallery Installation Guide

## Prerequisites

- **Docker Desktop** installed ([Download](https://www.docker.com/products/docker-desktop/))
  - Windows: Docker Desktop for Windows
  - Mac: Docker Desktop for Mac
  - Linux: Docker Engine + Docker Compose

### 1. Download the Application

  * Windows: Download and extract this zip file [ecogallery_windows.zip](https://raw.githubusercontent.com/georgemilas/ecogallery/refs/heads/master/deploy/docker/prod/ecogallery_windows.zip)
  * Mac/Linux: Download and extract this zip file [ecogallery_mac_linux.zip](https://raw.githubusercontent.com/georgemilas/ecogallery/refs/heads/master/deploy/docker/prod/ecogallery_mac_linux.zip)

  * Or if you want you can also get the source code
    ```bash
    git clone --recursive https://github.com/georgemilas/ecogallery.git
    cd ecogallery/deploy/docker/prod
    ```

### 2. Setup & Configuration

Run the setup script provided in the download to initialize the database and to start building the gallery.
Once the database is initialized and the synchronization process begins, feel free to stop the process at any time before it finishes (which may take a long time for large folders)  if you want to login into the app and start seeing what it is about while the gallery is building in the backreound.
To do that, stop the sync process (Ctrl+C) and start the app by using the start script provided. 

```bash
# Windows (Command Prompt)
setup.bat

# Linux/Mac
chmod +x setup.sh
setup.sh
```

### 3. Start the gallery
```bash
# Windows (Command Prompt)
start.bat

# Linux/Mac
chmod +x setup.sh
start.sh
```

# Manual Configuration

**Manual Changes in `.env` file:**
If you didn't run the setup script and don't yet have a .env file, first create one by copying .env.example
```bash
cp .env.exampl .env
```

The setup script above will help you set the admin password and pictures folder but you can also manualy change them before starting!**

```env
ADMIN_PASSWORD=CHANGE_ME_TO_SECURE_ADMIN_PASSWORD    #this is the initial admin user password for the gallery
                                                     #you set this only for the initial login after which you can change it in the app 

PICTURES_PATH=/path/to/your/pictures                 # Windows: Use forward slashes e.g., C:/Users/YourName/Pictures
                                                     # Linux/Mac: Use absolute path e.g., /home/yourname/pictures

```

## Email Configuration (this settings are needed for user management to work)

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


## Manually controll the application components

```bash
docker compose run sync      #run to manually sync the pictures folder into the database. Runs in the foreground to see the progress (may take a while for large folders)
                             #sync is incremental so after initial full sync any new/updated/renamed/deleted/moved files are also synced
docker compose run valbum    #run to create (or update) virtual albums (see below)
docker compose run face      #run to manually discover peoples faces. Runs in the foreground to see the progress (may take a while for large folders)
docker compose run geo       #run to manually generate geolocation clusters  

docker compose up -d         #run all components (the web app along with continuous sync, face recognition and geolocation clustering in the background)
                             #this is equivalent to what the start script does 

docker compose logs sync --tail=10  #see how much is left for sync to finish when running in the backround
docker compose logs face --tail=10  #see how much is left for face detection process to finish when running in the backround
```


# Virtual Albums 

Virtual albums represent public albums (no login required) generated based on a search expression or a designated folder.

Virtual albums can be pre-defined in a YAML file for a quick setup (e.g. `virtual_albums.yml`)
Additionaly you will also be able to use the gallery itself to manage (create/edit) virtual albums.   

Virtual albums in the YML files have the following properties:
- yml root keys are the actual album names
- album yml sub keys:
    - **expression**: A query expression to find pictures from within your folders structure (see examples bellow) 
    - **folder**: relative path to an album folder
    - one of either 'folder' or 'expression' is required but you only specify one, so either an expression or a folder, not both 
    - **description**: Album description (optional)
    - **feature**: Featured (album cover) image path (optional)
    - **parent**: Parent album name for hierarchical organization (optional)


Example `virtual_albums.yml`:
```yaml
Pictures Gallery:
  description: Welcome to my gallery
  folder: /public       #designate the /public folder and all images it cotains (excluding subfolders) as a public virtual album  
  feature: /public/IMG_8337.jpg

Barcelona:
  parent: Pictures Gallery
  description: From the 2023 trip to Barcelona
  #hand picking images based on their name and folder path 
  expression: barcelona and (7939 8024 8004 981 939 883 818 787 781)   
  feature: /2023/Barcelona/_IMG_7939.jpg

2024:
  parent: Pictures Gallery
  description: Selections from 2024 trips
  #all pictures from 2024 and all subfolders except colorado, eclipse and a few hand picked images to exclude 
  expression: 2024/ and not (colorado eclipse _8940 _8881)
  feature: /2024/best/_MG_2981.jpg

Colorado:
  parent: 2024   #Colorado will show up as a sub gallery inside 2024
  description: Selections from Colorado trip
  #all pictures from colorado from 2024 (for example 2024/colorado or 2024/select/best/colorado etc.)
  expression: 2024/ and colorado
  feature: /2024/Colorado/_MG_2981-Pano.jpg

Us:
  parent: Pictures Gallery
  description: the two of us 
  #pictures with both George and Maria since 2010 
  #allow face detection process to find some faces then login as admin and name a few clusters of similar faces
  #this expression say to find pictures that include both george and maria but not joe or any other unnamed face  
  expression: {ai:george} and {ai:maria} and not ({ai:box} {ai:joe}) and {d>=:Jan 2010}
  feature: /2022/Us/_MG_2983.jpg
```

**Expression syntax:**
- `AND`, `OR`, `NOT` - Logical operators
- searches any part of the full image path or use {regex} {ai:person name} {d>:date} {l:named location} etc.
- space is equivalent to using a logical OR so the expression 'colorado barcelona' is equivalent to 'colorado or barcelona' 
- Use parentheses for grouping
- Use quotes to include spaces for example "img 23.jpg" or "img 23"

**Set the path in `.env`:**
```env
VALBUM_YAML=./virtual_albums.yml
```

**Load virtual albums from configured YML file:**
```bash
docker-compose run valbum
```

