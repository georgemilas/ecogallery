
### Todo
   
   * theme for an album (background, border, roundings)
   * going back to album on large albums (wait for gallery to render before setting to last image - show loading - pagination?)
   * movie hash ?
   * {r:} regex, {s:} fuzzy search, {d=:} {d!=:} {d>:} {d>=:} {d<:} {d<=:} date 
      

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
