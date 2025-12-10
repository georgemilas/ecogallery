import React, { useEffect, useCallback, useMemo } from 'react';
import './gallery.css';
import { justifyGallery, debounce } from './gallery';
import { SortPanel } from './Sort';

export interface ImageExif {
  id: number;
  album_image_id: number;
  camera: string | null;
  lens: string | null;
  focal_length: string | null;
  aperture: string | null;
  exposure_time: string | null;
  iso: number | null;
  date_taken: string | null;
  rating: number | null;
  date_modified: string | null;
  flash: string | null;
  metering_mode: string | null;
  exposure_program: string | null;
  exposure_bias: string | null;
  exposure_mode: string | null;
  white_balance: string | null;
  color_space: string | null;
  scene_capture_type: string | null;
  circle_of_confusion: number | null;
  field_of_view: number | null;
  depth_of_field: number | null;
  hyperfocal_distance: number | null;
  normalized_light_value: number | null;
  software: string | null;
  serial_number: string | null;
  lens_serial_number: string | null;
  file_name: string;
  file_path: string;
  file_size_bytes: number | null;
  image_width: number | null;
  image_height: number | null;
  last_updated_utc: string;
}

export class AlbumItemHierarchy {
  id: number = 0;
  name: string = '';
  is_album: boolean = false;
  is_movie: boolean = false;
  navigation_path_segments: Array<string> = [];
  thumbnail_path: string = '';
  image_hd_path: string = '';
  image_uhd_path: string = '';
  image_original_path: string = '';
  last_updated_utc: Date = new Date();
  item_timestamp_utc: Date = new Date();
  content: AlbumItemHierarchy[] = [];
  image_exif: ImageExif | null = null;

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

interface AlbumHierarchyProps {
  album: AlbumItemHierarchy;
  onAlbumClick: (albumName: string) => void;
  onImageClick: (image: AlbumItemHierarchy) => void;
  lastViewedImage?: string | null;
}

export function AlbumHierarchyComponent({ album, onAlbumClick, onImageClick, lastViewedImage }: AlbumHierarchyProps) {
  const [sortedAlbums, setSortedAlbums] = React.useState<AlbumItemHierarchy[]>([]);
  const [sortedImages, setSortedImages] = React.useState<AlbumItemHierarchy[]>([]);

  const handleSortedAlbumsChange = useCallback((sorted: AlbumItemHierarchy[]) => {
    setSortedAlbums(sorted);
    setTimeout(() => {justifyGallery('.gallery', 300);}, 100);  
  }, []);

  const handleSortedImagesChange = useCallback((sorted: AlbumItemHierarchy[]) => {
    setSortedImages(sorted);
    setTimeout(() => {justifyGallery('.gallery', 300);}, 100);  
  }, []);

  const getAlbumName = useCallback((item: AlbumItemHierarchy) => {
    return album.get_name(item.name);
  }, [album]);

  const albumsToSort = useMemo(() => album.content.filter(a => a.is_album), [album.content]);
  const imagesToSort = useMemo(() => album.content.filter(a => !a.is_album), [album.content]);

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
        <SortPanel 
          albums={albumsToSort} 
          images={imagesToSort}
          getAlbumName={getAlbumName} 
          onSortedAlbumsChange={handleSortedAlbumsChange} 
          onSortedImagesChange={handleSortedImagesChange}
        />
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
      {album.content.some(a => a.is_album) && (
      <div className='albums'>
        <ul className='albums-container'>
        {(sortedAlbums.length > 0 ? sortedAlbums : album.content.filter(a => a.is_album)).map(r => (
          <li className='albums-item' key={r.id}>
            <a onClick={(e) => {e.preventDefault(); onAlbumClick(r.name);}}>
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


      <ul className="gallery">
        {(sortedImages.length > 0 ? sortedImages : album.content.filter(a => !a.is_album)).map(r => (
        <li className="gallery-item" key={r.id} data-image-name={r.name}> 
            <a href={`#${r.id.toString()}`} onClick={(e) => {e.preventDefault(); onImageClick(r);}}>
                <img src={r.thumbnail_path} alt={r.name} />
                <span className="gallery-item-label">{r.name}</span>
            </a>
        </li>
        ))}
      </ul>
    </>
  );
}

