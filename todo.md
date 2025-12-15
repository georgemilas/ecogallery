
### Todo
   * extract movie metadata
   * convert .mov to .mp4 during thumbnail processor for reliable viewing
   * way to navigate up to folder selection (or keep the breadcrumbs menu visible at the top after you scroll down) 
   * theme for an album (background, border, roundings)
   * sorting is now global, make it by album 
   * 108.250.182.25 / 192.168.1.254
   * add the breadcrumbs to the image viewer


### Changes
   * scrolling is instant instead of smooth


Set up nginx to:
    Listen on port 80
    Reverse proxy /api/* → http://localhost:5001/api/*
    Reverse proxy /pictures/* → http://localhost:5001/pictures/*
    Serve everything else → http://localhost:3000/*
