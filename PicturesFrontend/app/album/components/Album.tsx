import React, { useEffect, useCallback } from 'react';
import './gallery.css';
import { justifyGallery, debounce } from './gallery';
import { SortControl } from './Sort';
import { AlbumHierarchyProps, ImageItemContent } from './AlbumHierarchyProps';

export function AlbumHierarchyComponent({ album, onAlbumClick, onImageClick, lastViewedImage, albumSort, imageSort, onSortChange }: AlbumHierarchyProps) {
  const [, forceUpdate] = React.useReducer(x => x + 1, 0);
  
  const handleSortedAlbumsChange = useCallback(() => {
    console.log('AlbumHierarchyComponent: sorted albums changed', album.albums[0]?.name);
    forceUpdate(); // Force re-render after in-place sort
    setTimeout(() => {justifyGallery('.gallery', 300);}, 100);  
  }, []);

  const handleSortedImagesChange = useCallback(() => {
    forceUpdate(); // Force re-render after in-place sort
    setTimeout(() => {
      justifyGallery('.gallery', 300);
    }, 100);  
  }, []);


  // Scroll to last viewed image when returning from image view
  useEffect(() => {
    if (lastViewedImage) {
      // Small delay to ensure DOM is ready
      setTimeout(() => {
        const imageElement = document.querySelector(`[data-image-name="${lastViewedImage}"]`);
        if (imageElement) {
          imageElement.scrollIntoView({ behavior: 'smooth', block: 'center' });
        }
      }, 100);
    }
  }, [lastViewedImage]);

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
        <img src={album.image_hd_path} alt={album.album_name()} />
        <div className="gallery-banner-menu">
          <nav className="menu">
            <button onClick={() => onAlbumClick('')} className="back-button" title="Raw Album Data">Raw Album Data</button>
            <button onClick={() => onAlbumClick('')} className="back-button" title="Virtual Albums">Virtual Albums</button>
            <button onClick={() => onAlbumClick('')} className="back-button" title="Adhoc Query">Adhoc Query</button>
          </nav>
        </div>
        <div className="gallery-banner-label">
        <nav className="breadcrumbs">
          <a href="#"onClick={(e) => {e.preventDefault(); onAlbumClick('');}}>
            <svg width="16" height="16" viewBox="0 0 16 16" fill="currentColor" style={{verticalAlign: 'middle', marginTop: '-10px', marginLeft: '4px', marginBottom: '4px', marginRight: '4px'}}>
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
      {album.albums.length > 0 && (
      <div className='albums'>
        <SortControl type="albums" album={album} onSortChange={handleSortedAlbumsChange} initialSort={albumSort} 
          onSortUpdate={(sort) => onSortChange?.(sort, imageSort || 'timestamp-desc')}
        />
        <ul className='albums-container'>
        {(album.albums).map(r => (
          <li className='albums-item' key={r.id}>
            <a href="#" onClick={(e) => {e.preventDefault(); onAlbumClick(r.name);}}>
              <img src={r.thumbnail_path} alt={album.get_name(r.name)} />
              <span className="albums-item-label">{album.get_name(r.name)}</span>
              <svg className="albums-item-icon" width="24" height="24" viewBox="0 0 24 24" fill="none">
                <path d="M3 6C3 4.9 3.9 4 5 4H9L11 6H19C20.1 6 21 6.9 21 8V17C21 18.1 20.1 19 19 19H5C3.9 19 3 18.1 3 17V6Z"  fill="black" stroke='white'/>                
              </svg>
            </a>
          </li>
        ))} 
        </ul>        
      </div>
      )}



      <div className="gallery-container">
        <SortControl 
          type="images"
          album={album} 
          onSortChange={handleSortedImagesChange}
          initialSort={imageSort}
          onSortUpdate={(sort) => onSortChange?.(albumSort || 'timestamp-desc', sort)}
        />
        <ul className="gallery">
        {(album.images).map(r => (
        <li className="gallery-item" key={r.id} data-image-name={r.name}> 
            <a href={`#${r.id.toString()}`} onClick={(e) => {e.preventDefault(); onImageClick(r as ImageItemContent);}}>
                <img src={r.thumbnail_path} alt={r.name} />
                {r.is_movie && (
                  <svg className="gallery-item-video-icon" viewBox="0 0 24 24" fill="none">
                    <path d="M8 5v14l11-7L8 5z" fill="currentColor"/>
                  </svg>
                )}
                <span className="gallery-item-label">{r.name}</span>
            </a>
        </li>
        ))}
      </ul>
      </div>
    </>
  );
}

