## What it is
Gallery app for pictures sitting in a root folder on your hard drive or your local NAS.
* ### Why
    * I have close to 45,000 pictures and videos, almost 400GB (80K/2TB including raw files) worth of data 
    * To much to upload and manage externaly 
    * I still want a website to quicly show stuff or just for me to enjoy from wherever
    * Privacy matters, I don't want google/apple/facebook etc. to use my pictures for who knows what (enhace ai, know private data about me, friends and family)
    * I what the full size pictures if necesary when viewing on large or 8K displays 
* ### What
    * each folder is an album 
        * each album contains sub albums and images / movies
        * slideshow for the album
        * view image or movie detailed information
        * zoom, keyboard navigation, sorting etc. 
        * configure a feature image for the album by naming one of the files with a configurable prefix or suffix like "feature_", or just let the gallery select one
    * virtual albums, adhoc albums, flatten albums and powerful search
    * Todo: 
        * user management and clear separation of public / private catalogs
        * local AI integration for hardware that supports it mainly to enhance the search and the virtual albums  
* ### How
    Point the application at the folder containing pictures        
    * creates thumbnails and smaller size images for fast access
    * extracts metadata from images and movies
    * adds data to a local PostgreSQL database powering the gallery
    * keeps data on your hard drive syncronized with the gallery  (for example: move pictures around, add new ones, rename etc.)
    * simple configurations for files/folders to skip from processing and additional rules        
* ### Limitaions
    * Its local so you need to run a server and make it available on the internet 
    * Contact me for help
    * No raw files support yet

        

## What does it looks like

<table>
<tr>
<td width="auto" height="300px">

### Main Gallery Page
<img src="data/gallery.png" width="100%" alt="main gallery">
<div>
Navigate albums, sub albums, virtual albums. <br/>
Sort images and albums. <br/>
Search for quick adhoc albums on the fly 
</div>

</td>
<td width="auto" height="300px">

### Main Image/Video Viewer
<img src="data/gallery-image.png" width="100%" alt="main picture viewer">
<div>
    Slideshow (adjust speed and animations), zoom, full screen <br/>  
    View metadata <br />
    Keyboard navigation, everything you would expect. 
</div>
</td>
</tr>
</table>


## Project structure and some details
The solution folder consists of several projects:
1) GalleryLib - This is a library containing core logic for gallery sync. 
    
2) GalleryService - This project contains the heart of the syncronization logic as a thin layer on top of the library above.
    * Syncronize pictures folder with the database 
        ```powershell
        cd ecogallery
        dotnet run --project GalleryService -- db -f E:\TotulAici\TutuLaptop\pictures    
        ```
    * Syncronize pictures folder with the thumbnails folder  
        ```powershell
        cd ecogallery
        dotnet run --project GalleryService -- thumbnails -h 400, 1440 -f E:\TotulAici\TutuLaptop\pictures
        ```
    * Build virtual albums from a yml file 
        ```powershell
        cd ecogallery
        dotnet run --project GalleryService -- valbum -f E:\TotulAici\TutuLaptop\pictures -y "virtual albums.yml"
        ```
        
3) GalleryApi - Api powering the gallery. Exposes data at "/api/v1/..." 
    ```powershell
	cd ecogallery
    dotnet run --project GalleryApi
    # Swagger UI: http://localhost:5001/swagger
    # Endpoints:   http://localhost:5001/api/v1/albums, /valbums, /auth
    ```
4) GalleryFrontend - The main gallery app (a React/Type Script/NextJS) app
    * start a dev server for accessing the app locally from your network
    ```powershell    
	cd ecogallery
    npm run dev -prefix GalleryFrontend
    # Local Endpoint:   http://localhost:3000
    ```

   * start a production server to access the app from the internet 
   ```powershell
    cd ecogallery    
    npm run build -prefix GalleryFrontend   #rebuild if necesary
    npm run start -prefix GalleryFrontend -- --hostname 0.0.0.0 --port 3000
    # Internet Endpoint:   http://your.public.ip
    # Additional 1: Configure your router/firewall to port forward 80 from your public IP to your local server 
    # Additional 2: Setup a reverse proxy from port 80 to 3000 and 5001 (see nginx.config)
    ``` 

5) [ExpParser](https://github.com/georgemilas/ExpParser) - a library powering the search and virtual albums       
    

## Additional information
The solution above is targeting .NET 10 and if you aren't sure you have it or if there are issues building the projects, you can use the following command on a Windows machine in a powershell connsole. Alternatively download and install .NET from [Microsoft official .NET site](https://dotnet.microsoft.com/download/dotnet/10.0) 

```powershell
winget install Microsoft.DotNet.SDK.10
````
