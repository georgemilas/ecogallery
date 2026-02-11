
### Todo
   
   * theme for an album (background, border, roundings)
   * going back to album on large albums (wait for gallery to render before setting to last image - show loading - pagination?)
   * movie hash ?
   * {s:} fuzzy search, {loc:}
   * TODO: AlbumProcessor UpdateImageHash what if the thumbnail does not exist yet?
      

### Changes
   * scrolling is instant instead of smooth
   * add the breadcrumbs to the image viewer
   * 108.250.182.25 / 192.168.1.254
   * way to navigate up to folder selection (or keep the breadcrumbs menu visible at the top after you scroll down) 
   * pressing next with same image name -> use path or id instead of name? 
   * get rid of duplicates in search result like the same image in all these 3 folders album\select\best (image hash?)
   * sorting is now global, make it by album    
   * extract movie metadata
   * random 100, recent 100
   * not doing: convert .mov to .mp4 during thumbnail processor for reliable viewing (converted as a separate process)
   * {d=:} {d!=:} {d>:} {d>=:} {d<:} {d<=:} date, {ai:} {face:}

### ffmpeg
To convert an .mts (or .api or .mov etc) file to .mp4 using FFmpeg, use:
```powershell
ffmpeg -i input.mts -c:v libx264 -c:a aac output.mp4
```
 * input.mts: your source file
 * output.mp4: your desired output file
 * -c:v libx264: encodes video to H.264 (widely supported)
 * -c:a aac: encodes audio to AAC (widely supported)

```powershell
#powershell
Get-ChildItem *.mts | ForEach-Object {
    ffmpeg -i $_.FullName -c:v libx264 -c:a aac ($_.BaseName + '.mp4')
}
```

```powershell
#command prompt (cmd)
for %f in (*.mts) do ffmpeg -i "%f" -c:v libx264 -c:a aac "%~nf.mp4"
```

You can use the /R flag to recurively process all files in all subfolders:
```powershell
#command prompt (cmd)
for /R %f in (*.mts) do ffmpeg -i "%f" -c:v libx264 -c:a aac "%~dpnf.mp4"
```
 * /R makes the loop recursive.
 * %f is each file found.
 * %~dpnf expands to the full path and filename (without extension), so the .mp4 is created next to the source.



# commands
```bash
#windows
./init-db.bat                #initialize database and the admin user for the gallery
#mac/linux
chmod +x init-db.sh         #on linux make the script executable
./init-db.sh                #and then execute (or sudo ./init-db.sh to run as root)

#windows, mac or linux 
docker-compose run sync      #run once to populate and sync gallery database with picures folder (may take a while depending on the size of you pictures folder)
docker-compose run valbum    #run to create or update virtual albums (public albums based on "search" expression)
docker-compose run face      #Optional: run once to pre-discover faces and people  (may take a while depending on the size of you pictures folder)
docker-compose run geo       #Optional: run once to pre-generate geolocation clusters based on photos latitude/longitude metadata 
docker-compose up -d         #run the gallery web app (along with continuous sync, face recognition and geolocation clustering) in the background
                             #sync is incremental so any updated/renamed/deleted/moved files etc. are synced


docker-compose run cleanup   #optional utility to clean orphaned db records and thumbnails (for example you delete pictures and the "sync" service is not running)  
docker-compose run service cleanup -f /pictures -h 400 800 1440 -pl yes --log  #run cleanup in plan mode to see what it would do before commiting
docker-compose run service sync -f /pictures -h 400 800 1440 --reprocess       #run sync manualy to reprocess metadata 
docker-compose down          #stop gallery and associated processes

chmod +x init-db.sh         #on linux make the script executable
./init-db.sh                #and then execute (or sudo ./init-db.sh to run as root)



docker-compose logs api --tail=30
docker-compose exec api printenv | Select-String -Pattern "API"

docker-compose down nginx;
docker-compose build nginx;
docker-compose up -d --force-recreate --no-deps nginx
docker-compose up -d nginx;
docker-compose logs nginx --tail=30
docker-compose exec nginx cat /etc/nginx/nginx.conf
docker-compose exec nginx ls -l /pictures/2024/botanical\ garden/_MG_3507.jpg
docker-compose exec nginx ls -l '/pictures/2024/botanical garden/_MG_3507.jpg'
docker-compose exec nginx tail -n 30 /var/log/nginx/error.log

docker-compose exec postgres psql -U ecogallery -d ecogallery5 -c "SELECT image_path FROM album_image WHERE image_path LIKE '%2021%' LIMIT 5;"
docker-compose exec postgres psql -U ecogallery -d ecogallery5 -c "\d virtual_album"
docker-compose exec postgres psql -U ecogallery -d ecogallery5 -c "SELECT id, album_name, album_expression, album_type FROM virtual_album WHERE id IN (12, 13);"

docker-compose build --no-cache frontend; docker-compose up -d
docker-compose exec frontend printenv | Select-String -Pattern "API"

```
