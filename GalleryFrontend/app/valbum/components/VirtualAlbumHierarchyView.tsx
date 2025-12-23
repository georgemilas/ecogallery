import React, { useEffect, useCallback } from 'react';
import '../../album/components/gallery.css';
import { justifyGallery, debounce } from '../../album/components/gallery';
import { SortControl } from '../../album/components/Sort';
import { AlbumItemHierarchy, ImageItemContent, AlbumSettings } from '../../album/components/AlbumHierarchyProps';
import { DraggableBanner } from '../../album/components/DraggableBanner';
import { AppRouterInstance } from 'next/dist/shared/lib/app-router-context.shared-runtime';
import { useAuth } from '@/app/contexts/AuthContext';
import { apiFetch } from '@/app/utils/apiFetch';



export interface VirtualAlbumHierarchyProps {
  album: AlbumItemHierarchy;
  onAlbumClick: (albumId: number | null) => void;
  onImageClick: (image: ImageItemContent) => void;
  onSearchSubmit: (expression: string) => void;
  onGetApiUrl: (apiUrl: string) => void;
  lastViewedImage?: number | null;
  settings: AlbumSettings;
  router: AppRouterInstance;
  onSortChange?: (settings: AlbumSettings) => void;
  clearLastViewedImage?: () => void;
}




export function VirtualAlbumHierarchyView(props: VirtualAlbumHierarchyProps): JSX.Element {
  const { user } = useAuth();
  const [, forceUpdate] = React.useReducer(x => x + 1, 0);
  const [isLayouting, setIsLayouting] = React.useState(false);
  const [bannerEditMode, setBannerEditMode] = React.useState(false);
  const { settings, onSortChange } = props;

  const saveSettings = async (settings: any) => {
    //console.log(`saveSettings for user?: ${user ? user.id : 'null'}`);
    if (user) {
      //console.log(`saveSettings settings: ${JSON.stringify(settings)}`);
      settings.user_id = user.id;
      settings.album_id = props.album.id;

      try {
        const response = await apiFetch(`/api/v1/valbums/settings`, {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
          },
          body: JSON.stringify(settings),
        });
        const data = await response.json();  

        if (!response.ok) throw new Error('Failed to save settings');
      } catch (e) {
        console.error('Error saving settings:', e);
      }
    }
  };

  const handleBannerPositionSave = async (objectPositionY: number) => {
    const newSettings = { ...settings, banner_position_y: Math.floor(objectPositionY) };
    onSortChange?.(newSettings);
    console.log(`handleBannerPositionSave newSettings: ${JSON.stringify(newSettings)}`);
    await saveSettings(newSettings);
  };

  const handleSortedAlbumsChange = useCallback(() => {
    forceUpdate(); // Force re-render after in-place sort
    setIsLayouting(true);
    setTimeout(() => {
      justifyGallery('.gallery', getResponsiveHeight(), () => {
        setIsLayouting(false);
      });
    }, 100);
  }, [user, settings]);

  const handleSortedImagesChange = useCallback(() => {
    props.clearLastViewedImage?.();
    forceUpdate(); // Force re-render after in-place sort
    setIsLayouting(true);
    setTimeout(() => {
      justifyGallery('.gallery', getResponsiveHeight(), () => {
        setIsLayouting(false);
      });
    }, 100);
  }, [props.clearLastViewedImage, user, settings]);
  
  const getResponsiveHeight = () => {
    const width = window.innerWidth;
    if (width < 768) return 150;  // Mobile
    if (width < 1024) return 200; // Tablet
    return 300; // Desktop
  };
  
  const scrollToLastViewedImage = (imageId: number) => {
    const imageElement = document.querySelector(`[data-image-id="${imageId}"]`);
    if (imageElement) {
      console.log('Scrolling to last viewed image:', imageId);
      imageElement.scrollIntoView({ behavior: 'instant', block: 'center' });
    } 
    else {
      console.log('Image element not found for id:', imageId);
    }
  };


  useEffect(() => {
      // Wait for images to load before calculating layout
      const gallery = document.querySelector('.gallery');
      if (!gallery) return;
  
      //////////////////////////////////////////////////////////////
      //image load handling
      const images = Array.from(gallery.querySelectorAll('img'));
      let loadedCount = 0;
      const totalImages = images.length;
  
      if (totalImages === 0) return;
  
      const onImageLoad = () => {
        loadedCount++;
        if (loadedCount === totalImages) {
          setIsLayouting(true);
          justifyGallery('.gallery', getResponsiveHeight(), () => {
            setIsLayouting(false);
            if (props.lastViewedImage) {
              scrollToLastViewedImage(props.lastViewedImage);
            }
          });
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
        setIsLayouting(true);
        justifyGallery('.gallery', getResponsiveHeight(), () => {
          setIsLayouting(false);
          if (props.lastViewedImage) {
            scrollToLastViewedImage(props.lastViewedImage);
          }
        });
      }, 1000);
  
      //////////////////////////////////////////////////////////////
      //page  resize (scroll etc.) handling
      const handleResize = debounce(() => {
        setIsLayouting(true);
        justifyGallery('.gallery', getResponsiveHeight(), () => {
          setIsLayouting(false);
          // if (props.lastViewedImage) {
          //   scrollToLastViewedImage(props.lastViewedImage);
          // }
        });
      }, 150);    
      window.addEventListener('resize', handleResize);    
      
      return () => {
        clearTimeout(fallbackTimer);
        window.removeEventListener('resize', handleResize);
        images.forEach(img => {
          img.removeEventListener('load', onImageLoad);
          img.removeEventListener('error', onImageLoad);
        });
      };
  }, [props.album]); //, props.lastViewedImage]); 


  return (
    <>
      <DraggableBanner 
        album={props.album}
        isEditMode={bannerEditMode}
        onEditModeChange={setBannerEditMode}
        onPositionSave={handleBannerPositionSave}
        objectPositionY={settings.banner_position_y}
        label={
          <>
            <h1>{props.album.album_name()}</h1>
            {props.album.description && <h4>{props.album.description}</h4>}
          </>
        }
      />
      <div className="gallery-banner-menubar">                  
            <nav className="menu">
              <button onClick={() => props.onAlbumClick(null)} className="page-button" title="Albums">
                <svg width="16" height="16" viewBox="0 0 16 16" fill="currentColor" style={{verticalAlign: 'middle', marginTop: '0', marginLeft: '4px', marginBottom: '4px', marginRight: '4px'}}>
                  <path d="M8 2L2 7v7h4v-4h4v4h4V7L8 2z"/>
                </svg>Albums
              </button>

              <button
                onClick={() => props.router.push(user ? '/album' : '/login')}
                className="page-button"
                title={user ? 'Go to private albums' : 'Login to access private albums'}
              >
                {user ? 'Private' : 'Login'}
              </button>
              <button onClick={() => props.onGetApiUrl('random')} className="page-button" title="Random Images">Random</button>
              <button onClick={() => props.onGetApiUrl('recent')} className="page-button" title="Recent Images">Recent</button>

              <button onClick={() => window.scrollTo({ top: 0, behavior: 'instant' })} className="page-button" title="Scroll to Top">
                <svg width="16" height="16" viewBox="0 0 16 16" fill="currentColor" style={{verticalAlign: 'middle', marginTop: '0', marginLeft: '4px', marginBottom: '4px', marginRight: '4px'}}>
                  <path d="M8 2L4 6h3v8h2V6h3L8 2z"/>
                </svg>
                Top
              </button>
            </nav>                      
          </div>


      {props.album.albums.length > 0 && (
      <div className='albums'>
        <SortControl 
          type="albums" 
          album={props.album} 
          onSortChange={handleSortedAlbumsChange} 
          initialSort={settings.album_sort}
          onSortUpdate={async (sort) => {
            const newSettings = { ...settings, album_sort: sort };
            onSortChange?.(newSettings);
            await saveSettings(newSettings);
          }}
        />
        <ul className='albums-container'>
        {(props.album.albums).map(r => (
          <li className='albums-item' key={r.id}>
            <a href="#" onClick={(e) => {e.preventDefault(); props.onAlbumClick(r.id);}}>
              <img src={r.thumbnail_path} alt={props.album.get_name(r.name)} />
              <span className="albums-item-label">{props.album.get_name(r.name)}</span>
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
          album={props.album} 
          onSortChange={handleSortedImagesChange}
          initialSort={settings.image_sort}
          onSortUpdate={async (sort) => {
            const newSettings = { ...settings, image_sort: sort };
            onSortChange?.(newSettings);
            await saveSettings(newSettings);
          }}
        />
        {isLayouting && props.album.images.length > 100 && (
          <div className="gallery-loading">
            <span>Laying out {props.album.images.length} images...</span>
          </div>
        )}
        <ul className="gallery">
        {(props.album.images).map(r => (
        <li className="gallery-item" key={r.id} data-image-id={r.id} data-image-name={r.name}> 
            <a href={`#${r.id.toString()}`} onClick={(e) => {e.preventDefault(); props.onImageClick(r as ImageItemContent);}}>
                <img src={r.thumbnail_path} alt={r.name} />
                {r.is_movie && (
                  <svg className="gallery-item-video-icon" viewBox="0 0 24 24" fill="none">
                    <path d="M8 5v14l11-7L8 5z" fill="currentColor"/>
                  </svg>
                )}
                <span className="gallery-item-label">{r.name}</span>
                {r.description && <span className="gallery-item-label">{r.description}</span>}
            </a>
        </li>
        ))}
      </ul>
      </div>
    </>
  );
}

