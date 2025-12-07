import React, { useEffect } from 'react';
import './gallery.css';
import { justifyGallery, debounce } from './gallery';

export class AlbumHierarchy {
  id: number = 0;
  name: string = '';
  is_album: boolean = false;
  navigation_path_segments: Array<string> = [];
  image_path: string = '';
  last_updated_utc: Date = new Date();
  item_timestamp_utc: Date = new Date();
  content: AlbumHierarchy[] = [];

  get_name(path: string): string {
    var  name = path.split('\\');
    if (name.length === 0) {
      name = path.split('/');
    }
    return name.pop() || 'Pictures Gallery';
  }

  album_name(): string {
    return this.get_name(this.name);
  }

}  

interface AlbumHierarchyServiceProps {
  album: AlbumHierarchy;
  onAlbumClick: (albumName: string) => void;
}

export function AlbumHierarchyService({ album, onAlbumClick }: AlbumHierarchyServiceProps) {
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
  }, [album]); // Re-run when album change

  
  return (
    <>
      <div className="gallery-banner">
        <img src={album.image_path} alt={album.album_name()} />
        
        <div className="gallery-banner-label">
        <nav className="breadcrumbs">
          <a href="#"onClick={(e) => {e.preventDefault(); onAlbumClick('');}}>
            <svg width="16" height="16" viewBox="0 0 16 16" fill="currentColor" style={{verticalAlign: 'middle', marginBottom: '4px', marginRight: '4px'}}>
              <path d="M8 2L2 7v7h4v-4h4v4h4V7L8 2z"/>
            </svg>
          </a>
          {album.navigation_path_segments.map((segment, index) => {
            const pathToSegment = '\\' + album.navigation_path_segments.slice(0, index + 1).join('\\');
            return (
              <span key={index}>
                {' > '} <a href="#" onClick={(e) => {e.preventDefault(); onAlbumClick(pathToSegment);}}>{segment}</a>
              </span>
            );
          })}
        </nav>
        
        <h1>{album.album_name()}</h1>     
        </div>
      </div>	
      {album.content.some(a => a.is_album) && (
      <div className='albums'>
        <h1 className='albums-label'>sub-albums</h1>
        <ul className='albums-container'>
        {album.content
          .filter(a => a.is_album)
          .toSorted((a, b) => new Date(b.last_updated_utc).getTime() - new Date(a.last_updated_utc).getTime())
          .map(r => (
          <li className='albums-item' key={r.id}>
            <a onClick={(e) => {e.preventDefault(); onAlbumClick(r.name);}}>
              <img src={r.image_path} alt={album.get_name(r.name)} />
              <span className="albums-item-label">{album.get_name(r.name)}</span>
            </a>
          </li>
        ))} 
        </ul>        
      </div>
      )}

      <ul className="gallery">
        {album.content
          .filter(a => !a.is_album)
          .toSorted((a, b) => new Date(b.item_timestamp_utc).getTime() - new Date(a.item_timestamp_utc).getTime())
          .map(r => (
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

