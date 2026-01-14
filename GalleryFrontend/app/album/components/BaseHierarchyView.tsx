import React, { useEffect, useCallback } from 'react';
import './gallery.css';
import { justifyGallery, debounce } from './gallery';
import { SortControl } from './Sort';
import { AlbumItemHierarchy, ImageItemContent, AlbumSettings } from './AlbumHierarchyProps';
import { DraggableBanner } from './DraggableBanner';
import { AppRouterInstance } from 'next/dist/shared/lib/app-router-context.shared-runtime';
import { useAuth } from '@/app/contexts/AuthContext';
import { apiFetch } from '@/app/utils/apiFetch';

export interface BaseHierarchyConfig {
  settingsApiEndpoint: string; // '/api/v1/albums/settings' or '/api/v1/valbums/settings'
  showSearch: boolean;
  renderNavMenu: (props: BaseHierarchyProps) => React.ReactNode;
  getImageLabel: (album: AlbumItemHierarchy, imageName: string) => string;
}

export interface BaseHierarchyProps {
  album: AlbumItemHierarchy;
  onAlbumClick: (albumId: number | null) => void;
  onImageClick: (image: ImageItemContent) => void;
  onSearchSubmit?: (expression: string, offset: number) => void;
  onGetApiUrl: (apiUrl: string) => void;
  lastViewedImage?: number | null;
  settings: AlbumSettings;
  router: AppRouterInstance;
  onSortChange?: (settings: AlbumSettings) => void;
  clearLastViewedImage?: () => void;
  config: BaseHierarchyConfig;
}

export function BaseHierarchyView(props: BaseHierarchyProps): JSX.Element {
  const [, forceUpdate] = React.useReducer(x => x + 1, 0);
  const [searchText, setSearchText] = React.useState('');
  const [isLayouting, setIsLayouting] = React.useState(false);
  const [bannerEditMode, setBannerEditMode] = React.useState(false);
  const { user } = useAuth();
  const { settings, onSortChange, config } = props;
  
  const saveSettings = async (settings: any) => {
    if (user) {
      settings.user_id = user.id;
      settings.album_id = props.album.id;

      try {
        const response = await apiFetch(config.settingsApiEndpoint, {
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
  
  const getResponsiveHeight = () => {
    const width = window.innerWidth;
    if (width < 768) return 150;  // Mobile
    if (width < 1024) return 200; // Tablet
    return 300; // Desktop
  };

  const handleSearchSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (searchText.trim().length === 0) return;
    props.onSearchSubmit?.(searchText.trim(), 0);
  };
    
  const scrollToLastViewedImage = (imageId: number) => {
    const imageElement = document.querySelector(`[data-image-id="${imageId}"]`);
    if (imageElement) {
      console.log('Scrolling to last viewed image:', imageId);
      imageElement.scrollIntoView({ behavior: 'instant', block: 'center' });
    } else {
      console.log('Image element not found for id:', imageId);
    }
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

  // Justify Gallery on mount and when album changes
  useEffect(() => {
    const gallery = document.querySelector('.gallery');
    if (!gallery) return;

    setIsLayouting(true);
    justifyGallery('.gallery', getResponsiveHeight(), () => {
      setIsLayouting(false);
      if (props.lastViewedImage) {
        scrollToLastViewedImage(props.lastViewedImage);
      }
    });

    // Handle page resize
    const handleResize = debounce(() => {
      setIsLayouting(true);
      justifyGallery('.gallery', getResponsiveHeight(), () => {
        setIsLayouting(false);        
      });
    }, 150);    
    window.addEventListener('resize', handleResize);    
    
    return () => {
      window.removeEventListener('resize', handleResize);
    };
  }, [props.album]);

  const renderBreadcrumbs = () => (
    <nav className="breadcrumbs">
      {props.album.navigation_path_segments.length > 1 && (
        <a href="#" onClick={(e) => {e.preventDefault(); props.onAlbumClick(null);}}>
          <svg width="16" height="16" viewBox="0 0 16 16" fill="currentColor" style={{verticalAlign: 'middle', marginTop: '0px', marginLeft: '2px', marginBottom: '4px', marginRight: '2px'}}>
            <path d="M8 2L2 7v7h4v-4h4v4h4V7L8 2z"/>
          </svg>
        </a>
      )}
      {props.album.navigation_path_segments.slice(1).map((segment, index) => (
        <span key={index}>
          {' > '} <a href="#" onClick={(e) => {e.preventDefault(); props.onAlbumClick(segment.id);}}>{segment.name}</a>
        </span>
      ))}  
      {config.showSearch && props.album.search_info && props.album.search_info.count > props.album.search_info.limit && (
        <div className="search-info">
          {props.album.search_info.offset > 0 && (
            <button onClick={() => props.onSearchSubmit?.(props.album.search_info!.expression, props.album.search_info!.offset - props.album.search_info!.limit)} className="page-button" title="Prev">Previous</button>
          )}
          {props.album.search_info.offset + props.album.search_info.limit <= props.album.search_info.count && (
            <button onClick={() => props.onSearchSubmit?.(props.album.search_info!.expression, props.album.search_info!.offset + props.album.search_info!.limit)} className="page-button" title="Next">Next</button>
          )}
        </div>
      )}            
    </nav>
  );

  const renderSearchBar = () => {
    if (!config.showSearch) return (<div></div>);
    
    return (
      <form className="searchbar" onSubmit={handleSearchSubmit}>
        <input type="text" placeholder="Search expression..." value={searchText} onChange={(e) => setSearchText(e.target.value)}/>
        <button type="submit" className="search-button" title="Search">
          <svg viewBox="0 0 24 24" fill="none">
            <circle cx="10" cy="10" r="6" stroke="white" strokeWidth="2"/>
            <line x1="16.5" y1="16.5" x2="21" y2="21" stroke="white" strokeWidth="2"/>
          </svg>
        </button>
      </form>
    );
  };

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
        {renderBreadcrumbs()}
        {config.renderNavMenu(props)}
        {renderSearchBar()}
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
              props.onSortChange?.(newSettings);
              await saveSettings(newSettings);
            }}
          />
          <ul className='albums-container'>
            {props.album.albums.map(r => (
              <li className='albums-item' key={r.id}>
                <a href="#" onClick={(e) => {e.preventDefault(); props.onAlbumClick(r.id);}}>
                  <img src={r.thumbnail_path} alt={config.getImageLabel(props.album, r.name)} />
                  <span className="albums-item-label">{config.getImageLabel(props.album, r.name)}</span>
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
            props.onSortChange?.(newSettings);
            await saveSettings(newSettings);
          }}
        />
        {isLayouting && props.album.images.length > 100 && (
          <div className="gallery-loading">
            <span>
              Loading and laying out {props.album.images.length} images
              <span className="loader-spinner" aria-label="Loading"></span>
            </span>
          </div>
        )}
        <ul className="gallery">
          {props.album.images.map(r => {
            const width = r.is_movie ? r.video_metadata?.video_width ?? null : r.image_metadata?.image_width ?? null;
            const height = r.is_movie ? r.video_metadata?.video_height ?? null : r.image_metadata?.image_height ?? null;
            return (
              <li className="gallery-item" key={r.id} data-image-id={r.id} data-image-name={r.name} data-image-width={width ?? 0} data-image-height={height ?? 0}> 
                <a href={`#${r.id.toString()}`} onClick={(e) => {e.preventDefault(); props.onImageClick(r as ImageItemContent);}}>
                  <img src={r.thumbnail_path} alt={r.name} />
                  {r.is_movie && (
                    <svg className="gallery-item-video-icon" viewBox="0 0 24 24" fill="none">
                      <path d="M8 5v14l11-7L8 5z" fill="currentColor"/>
                    </svg>
                  )}
                  <span className="gallery-item-label">{config.getImageLabel(props.album, r.name)}</span>                
                  {r.description && <span className="gallery-item-label">{r.description}</span>}
                </a>
              </li>
            );
          })}
        </ul>
      </div>
    </>
  );
}