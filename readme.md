## What it is
Gallery app for pictures sitting in a root folder on your hard drive or your local NAS.
* ### Why
    * I have close to 50,000  pictures, almost 2TB worth of data
    * To much to upload and manage externaly 
    * I still want a website to quicly show stuff or just for me to enjoy from wherever
    * I like privacy. I don't want google/apple/facebook etc. to use my pictures for who knows what (enhace ai, know private data about me, friends, family)
    * I what the full size pictures for myself when navigating and viewing if necesary  
* ### How
    Point the application at the folder containing pictures        
    * creates thumbnails and smaller size images for fast access
    * extracts metadata from images and movies
    * adds data to a local PostgreSQL database powering the gallery
    * keeps data on your hard drive syncronized with the gallery  (for example: move pictures around, add new ones, rename etc.)
    * simple configurations for files/folders to skip from processing
* ### What
    * each folder is an album 
        * each album contains sub albums and images / movies
        * configure a feature image for the album by naming one of the files with a configurable prefix or suffix like "feature_", or just let the gallery select one
        * slideshow for the album
        * view image metadata
        * zoom, keyboard navigation, sorting etc. 
    * virtual albums, adhoc albums, flatten albums and powerful search
    * Todo: 
        * user namagement and clear separation of public / private catalogs
        * local AI integration for hardware that supports it mainly to enhance the search and the virtual albums  
* ### Limitaions
    * Its local so you need to run a server and make it available on the internet 
    * Contact me for help

        

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


## How it works
The solution folder consists of 5 projects :
1) PictureLib - This is a library containing the main logic for the solution. 
    * model folder: data models 
    * service folder: main business logic
    * database: all database scripts, tables, stored procs, indeces etc.
    * repository: database abstractions 

2) PicturesConsole - This project contains the heart of the syncronization logic
    Actually it's just a thin layer on top of the library above.
	```powershell
	cd gmpictures
    dotnet run --project PicturesConsole -- exif -f E:\TotulAici\TutuLaptop\pictures
    dotnet run --project PicturesConsole -- thumbnails -h 400, 1440 -f E:\TotulAici\TutuLaptop\pictures
    ```
3) PicturesApi - Api powering the gallery. Expose the data at "/api/v1/pictures" and also to run as a docker container
    ```powershell
	cd gmpictures
    dotnet run --project PicturesApi
    # Swagger UI: http://localhost:5001/swagger
    # Endpoint:   http://localhost:5001/api/v1/pictures
    ```
4) PicturesFrontend - The main gallery app (a React/Type Script/NextJS) app
    ```powershell
	cd gmpictures\PicturesFrontend
    npm run dev
    # Endpoint:   http://localhost:3000
    ``` 
5) PictureLib.Tests - tests to ensure correctness in the business services      
    ```powershell
    cd gmpictures
	dotnet test
    ```

## Additional information
The solution above is targeting .NET 10 and if you aren't sure you have it or if there are issues building the projects, you can use the following command on a Windows machine in a powershell connsole. Alternatively download and install .NET from [Microsoft official .NET site](https://dotnet.microsoft.com/download/dotnet/10.0) 

```powershell
winget install Microsoft.DotNet.SDK.10
````
