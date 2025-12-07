import React, { useEffect } from 'react';
import './gallery.css';
import { justifyGallery, debounce } from './gallery';

export type AlbumHierarchy = {
  id: number;
  name: string;
  is_album: boolean;
  navigation_path_segments: Array<string>;
  image_path: string;
  last_updated_utc: Date;
};    

interface AlbumHierarchyServiceProps {
  albums: AlbumHierarchy[];
  onAlbumClick: (albumName: string) => void;
}

export function AlbumHierarchyService({ albums, onAlbumClick }: AlbumHierarchyServiceProps) {
  useEffect(() => {
    // Wait for images to load before calculating layout
    const gallery = document.querySelector('.gallery');
    if (!gallery) return;

    const images = Array.from(gallery.querySelectorAll('img'));
    let loadedCount = 0;
    const totalImages = images.length;

    const onImageLoad = () => {
      loadedCount++;
      if (loadedCount === totalImages) {
        justifyGallery('.gallery', 300);
      }
    };

    // Add load listeners to all images
    images.forEach(img => {
      if (img.complete) {
        onImageLoad(); // Already loaded
      } else {
        img.addEventListener('load', onImageLoad);
        img.addEventListener('error', onImageLoad); // Count errors too
      }
    });

    // Fallback: call after a delay if images don't load
    const fallbackTimer = setTimeout(() => {
      justifyGallery('.gallery', 300);
    }, 1000);

    const handleResize = debounce(() => {justifyGallery('.gallery', 300);}, 150);    
    window.addEventListener('resize', handleResize);    
    
    return () => {
      clearTimeout(fallbackTimer);
      window.removeEventListener('resize', handleResize);
      images.forEach(img => {
        img.removeEventListener('load', onImageLoad);
        img.removeEventListener('error', onImageLoad);
      });
    };
  }, [albums]); // Re-run when albums change

  if (!albums?.length) {
    return <p>No data.</p>;
  }
  return (
    <>
      <div className="gallery-banner">
        <img src="https://photos.smugmug.com/photos/i-RkqTzwS/0/Lg4wsf7Bq8bZMN82tRdTpKGfM8krDgcTj9hSChfCw/X4/i-RkqTzwS-X4.jpg" alt="Gallery banner" />
        <h1 className="gallery-banner-label">Bahamas</h1>     
      </div>	
      <div className='albums'>
        <h1 className='albums-label'>sub-albums</h1>
        <ul className='albums-container'>
        {albums.filter(a => a.is_album).map(r => (
          <li className='albums-item' key={r.id}>
            <a onClick={(e) => {e.preventDefault(); onAlbumClick(r.name);}}>
              <img src={r.image_path} alt={r.name} />
              <span className="albums-item-label">{r.name}</span>
            </a>
          </li>
        ))} 
        </ul>        
      </div>

      <ul className="gallery">
        {albums.filter(a => !a.is_album).map(r => (
        <li className="gallery-item" key={r.id}>
            <a href={r.image_path} target="_blank">
                <img src={r.image_path} alt={r.name} />
                <span className="gallery-item-label">{r.name}</span>
            </a>
        </li>
        ))}
      </ul>
    </>
  );
}

