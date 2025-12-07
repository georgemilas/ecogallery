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

export function AlbumHierarchyService({ albums }: { albums: AlbumHierarchy[] }) {
  useEffect(() => {
    // Run gallery layout on page load
    justifyGallery('.gallery', 300);
    
    // Setup resize handler
    const handleResize = debounce(() => {
      justifyGallery('.gallery', 300);
    }, 150);
    
    window.addEventListener('resize', handleResize);
    
    // Cleanup
    return () => {
      window.removeEventListener('resize', handleResize);
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

      <ul className="gallery">
        {albums.map(r => (
        <li className="gallery-item">
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

