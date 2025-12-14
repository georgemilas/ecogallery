## What it is
Gallery app for pictures sitting in a root folder on you hard drive or your local NAS.
   * ### Why
       * I have close to 50,000  pictures, almost 2TB worth of data
       * To much to upload and manage externaly 
       * I still want a website to quicly show stuff or just for me to enjoy from wherever
       * I like privacy and don't whant google/apple/facebook/smugmug etc. to invade it or use my pictures for who  knows what?
       * I what accees to full size pictures for myself   
   * ### How
       Point the application at the folder containing pictures        
       * creates thumbnails for fast access
        * extracts metadata from images and movies
        * adds data to local PostgreSQL database
        * keeps data on your hard drive syncronized with the gallery  (for example: move pictures around, add new ones, rename etc.)
        * simple configurations for files/folders to skip from processing
        * each folder is an album 
            * each album (folder) contains sub albums and images / movies
            * configure a feature image for the album by naming one of the files with the prefix "feature_" (this is configurable)
            * slideshow for the album
            * view image metadata
            * zoom, keyboard navigation, sorting etc. 
        

## What does it looks like

<table>
<tr>
<td width="auto" height="300px">

### Main Gallery Page
<img src="gallery.png" width="100%" alt="main gallery">

</td>
<td width="auto" height="300px">

### Main Image/Video Viewer
<img src="gallery-image.png" width="100%" alt="main picture viewer">

</td>
</tr>
</table>


## How it works
The solution folder consists of 4 projects :
1) PictureLib - This is a library containing the main business logic for the solution. 
    * model folder: data models 
    * service folder: main business logic
    * database: all database scripts, tables, stored procs, indeces etc.
    * repository: database abstractions 

2) PicturesConsole - This projects contains the heart of the syncronization logic
	```powershell
	cd gmpictures
    dotnet run --project PicturesConsole -- exif -f E:\TotulAici\TutuLaptop\pictures
    dotnet run --project PicturesConsole -- thumbnails -h 400, 1440 -f E:\TotulAici\TutuLaptop\pictures
    ```
3) PicturesApi - A versioned Web API (v1) to expose the data at "/api/v1/pictures" and also to run as a docker container
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
The solution above is targeting .NET 8 or above and if you aren't sure you have it or if there are issues building the projects, you can use the following command on a Windows machine in a powershell connsole or download and install from [Microsoft official .NET site](https://dotnet.microsoft.com/download/dotnet/8.0) 

```powershell
winget install Microsoft.DotNet.SDK.8
````
