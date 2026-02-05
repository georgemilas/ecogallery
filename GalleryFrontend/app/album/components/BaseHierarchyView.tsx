import React, { useEffect, useCallback, useState } from 'react';
import './gallery.css';
import { debounce } from './gallery';
import { SortControl } from './Sort';
import { AlbumItemHierarchy, ImageItemContent, AlbumSettings, AlbumItemContent } from './AlbumHierarchyProps';
import { DraggableBanner } from './DraggableBanner';
import { CancellableImage } from './CancellableImage';
import { VirtualizedGallery } from './VirtualizedGallery';
import { AppRouterInstance } from 'next/dist/shared/lib/app-router-context.shared-runtime';
import { useAuth } from '@/app/contexts/AuthContext';
import { apiFetch } from '@/app/utils/apiFetch';
import { useGallerySettings } from '@/app/contexts/GallerySettingsContext';

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
  onFaceSearch?: (personId: number, personName: string | null) => void;
  onFaceDelete?: (faceId: number) => void;
  onPersonDelete?: (personId: number) => void;
  onSearchByName?: (name: string) => void;
  onSearchByPersonId?: (personId: number) => void;
  onSortedImagesChange?: (images: ImageItemContent[]) => void;
  config: BaseHierarchyConfig;
}

export function BaseHierarchyView(props: BaseHierarchyProps): JSX.Element {
  const [, forceUpdate] = React.useReducer(x => x + 1, 0);
  const [searchText, setSearchText] = React.useState('');
  const [bannerEditMode, setBannerEditMode] = React.useState(false);
  const [showSettingsModal, setShowSettingsModal] = useState(false);
  const { user, logout } = useAuth();
  const { settings: gallerySettings, setShowFaceBoxes, setSearchPageSize, setPeopleMenuLimit } = useGallerySettings();
  const { settings, onSortChange, config } = props;

  // Keyboard shortcuts
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      // Don't trigger if user is typing in an input field
      if (e.target instanceof HTMLInputElement || e.target instanceof HTMLTextAreaElement) {
        return;
      }

      // 's' or ',' (comma) to toggle settings modal
      if (e.key === 's' || e.key === 'S' || e.key === ',') {
        setShowSettingsModal(prev => !prev);
        return;
      }

      // 'Escape' to close settings modal
      if (e.key === 'Escape' && showSettingsModal) {
        setShowSettingsModal(false);
        return;
      }

      // 'f' to toggle face boxes (only for authenticated users)
      if (user && (e.key === 'f' || e.key === 'F')) {
        setShowFaceBoxes(!gallerySettings.showFaceBoxes);
      }
    };

    document.addEventListener('keydown', handleKeyDown);
    return () => document.removeEventListener('keydown', handleKeyDown);
  }, [user, gallerySettings.showFaceBoxes, setShowFaceBoxes, showSettingsModal]);

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
    
  // Track responsive target height for gallery
  const [targetHeight, setTargetHeight] = useState(getResponsiveHeight());

  // Update target height on window resize
  useEffect(() => {
    const handleResize = debounce(() => {
      setTargetHeight(getResponsiveHeight());
    }, 150);
    window.addEventListener('resize', handleResize);
    return () => window.removeEventListener('resize', handleResize);
  }, []);

  const handleSortedItemsChange = useCallback((sortedItems: ImageItemContent[] | AlbumItemContent[]) => {
    props.clearLastViewedImage?.();
    if (Array.isArray(sortedItems) && sortedItems.length > 0) {
      // Type guard: ImageItemContent has 'is_movie' property, AlbumItemContent doesn't
      if ('is_movie' in sortedItems[0]) {
        setLocalImages(sortedItems as ImageItemContent[]);
      } else {
        setLocalAlbums(sortedItems as AlbumItemContent[]);
      }
    }
    forceUpdate();
  }, [props.clearLastViewedImage]);

  // Handle sort value update: save to API and propagate AlbumSettings to parent
  const handleSortUpdate = useCallback(async (sortValue: string, target: 'album' | 'image') => {
    const newSettings = { ...props.settings };
    if (target === 'album') newSettings.album_sort = sortValue;
    if (target === 'image') newSettings.image_sort = sortValue;
    await saveSettings(newSettings);
    props.onSortChange?.(newSettings);
  }, [props.settings, props.onSortChange]);

  const [localImages, setLocalImages] = useState<ImageItemContent[]>(props.album.images);
  const [localAlbums, setLocalAlbums] = useState<AlbumItemContent[]>(props.album.albums);

  // Sync localImages and localAlbums when album data changes (before sorting kicks in)
  useEffect(() => {
    setLocalImages(props.album.images);
    setLocalAlbums(props.album.albums);
  }, [props.album.images, props.album.albums]);

  // Report sorted images to parent for ImageView navigation
  useEffect(() => {
    props.onSortedImagesChange?.(localImages);
  }, [localImages]);

  // Get image label helper for VirtualizedGallery
  const getImageLabelForGallery = useCallback((imageName: string) => {
    return config.getImageLabel(props.album, imageName);
  }, [config, props.album]);

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

  const renderSettingsButton = () => (
    <button
      className="settings-button"
      onClick={() => setShowSettingsModal(true)}
      title="Gallery Settings"
      style={{
        background: 'none',
        border: 'none',
        cursor: 'pointer',
        padding: '4px 8px',
        display: 'flex',
        alignItems: 'center',
      }}
    >
      <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="#e8f09e" strokeWidth="1.5">
        <path d="M12 15a3 3 0 1 0 0-6 3 3 0 0 0 0 6z"/>
        <path d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 0 1-2.83 2.83l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-4 0v-.09a1.65 1.65 0 0 0-1.08-1.51 1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 0 1-2.83-2.83l.06-.06a1.65 1.65 0 0 0 .33-1.82 1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1 0-4h.09a1.65 1.65 0 0 0 1.51-1.08 1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 0 1 2.83-2.83l.06.06a1.65 1.65 0 0 0 1.82.33h.08a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 4 0v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 0 1 2.83 2.83l-.06.06a1.65 1.65 0 0 0-.33 1.82v.08a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 0 4h-.09a1.65 1.65 0 0 0-1.51 1z"/>
      </svg>
    </button>
  );

  const renderSettingsModal = () => {
    if (!showSettingsModal) return null;

    return (
      <div
        className="settings-modal-overlay"
        onClick={() => setShowSettingsModal(false)}
        style={{
          position: 'fixed',
          top: 0,
          left: 0,
          right: 0,
          bottom: 0,
          backgroundColor: 'rgba(0, 0, 0, 0.7)',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          zIndex: 2000,
        }}
      >
        <div
          className="settings-modal"
          onClick={(e) => e.stopPropagation()}
          style={{
            backgroundColor: '#2a2a2a',
            borderRadius: '8px',
            padding: '24px',
            minWidth: '300px',
            maxWidth: '400px',
            border: '1px solid #e8f09e',
          }}
        >
          <h3 style={{ margin: '0 0 20px 0', color: '#e8f09e' }}>Gallery Settings</h3>

          <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '16px' }}>
            <label htmlFor="showFaces" style={{ color: '#ddd' }}>Show Face Boxes</label>
            <label className="toggle-switch" style={{ position: 'relative', display: 'inline-block', width: '50px', height: '26px' }}>
              <input
                type="checkbox"
                id="showFaces"
                checked={gallerySettings.showFaceBoxes}
                onChange={(e) => setShowFaceBoxes(e.target.checked)}
                style={{ opacity: 0, width: 0, height: 0 }}
              />
              <span
                style={{
                  position: 'absolute',
                  cursor: 'pointer',
                  top: 0,
                  left: 0,
                  right: 0,
                  bottom: 0,
                  backgroundColor: gallerySettings.showFaceBoxes ? '#4CAF50' : '#555',
                  transition: '0.3s',
                  borderRadius: '26px',
                }}
              >
                <span style={{
                  position: 'absolute',
                  content: '""',
                  height: '20px',
                  width: '20px',
                  left: gallerySettings.showFaceBoxes ? '26px' : '3px',
                  bottom: '3px',
                  backgroundColor: 'white',
                  transition: '0.3s',
                  borderRadius: '50%',
                }}/>
              </span>
            </label>
          </div>

          <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '16px' }}>
            <label htmlFor="searchPageSize" style={{ color: '#ddd' }}>Search Page Size</label>
            <input
              type="number"
              id="searchPageSize"
              value={gallerySettings.searchPageSize}
              onChange={(e) => setSearchPageSize(Math.max(1, parseInt(e.target.value) || 2000))}
              min="1"
              max="10000"
              style={{
                width: '80px',
                padding: '6px 10px',
                borderRadius: '4px',
                border: '1px solid #555',
                backgroundColor: '#333',
                color: '#ddd',
                textAlign: 'right',
              }}
            />
          </div>

          <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '16px' }}>
            <label htmlFor="peopleMenuLimit" style={{ color: '#ddd' }}>People Menu Limit</label>
            <input
              type="number"
              id="peopleMenuLimit"
              value={gallerySettings.peopleMenuLimit}
              onChange={(e) => setPeopleMenuLimit(Math.max(1, Math.min(100, parseInt(e.target.value) || 20)))}
              min="1"
              max="100"
              style={{
                width: '80px',
                padding: '6px 10px',
                borderRadius: '4px',
                border: '1px solid #555',
                backgroundColor: '#333',
                color: '#ddd',
                textAlign: 'right',
              }}
            />
          </div>

          <div style={{ display: 'flex', justifyContent: 'space-between', marginTop: '20px' }}>
            <button
              onClick={() => {
                setShowSettingsModal(false);
                logout();
              }}
              style={{
                padding: '8px 16px',
                backgroundColor: '#dc3545',
                color: 'white',
                border: 'none',
                borderRadius: '4px',
                cursor: 'pointer',
                fontWeight: 'bold',
              }}
            >
              Logout
            </button>
            <button
              onClick={() => setShowSettingsModal(false)}
              style={{
                padding: '8px 16px',
                backgroundColor: '#e8f09e',
                color: '#333',
                border: 'none',
                borderRadius: '4px',
                cursor: 'pointer',
                fontWeight: 'bold',
              }}
            >
              Close
            </button>
          </div>
        </div>
      </div>
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
        <div style={{ display: 'flex', alignItems: 'center', gap: '0' }}>
          {user && renderSettingsButton()}
          {renderBreadcrumbs()}
        </div>
        {config.renderNavMenu(props)}
        {renderSearchBar()}
      </div>
      {renderSettingsModal()}

      {localAlbums.length > 0 && (
        <div className='albums'>
          <SortControl
            type="albums"
            album={props.album}
            onSortChange={handleSortedItemsChange}
            initialSort={settings.album_sort}
            onSortUpdate={(sort) => handleSortUpdate(sort, 'album')}
          />
          <ul className='albums-container'>
            {localAlbums.map(r => (
              <li className='albums-item' key={r.id}>
                <a href="#" onClick={(e) => {e.preventDefault(); props.onAlbumClick(r.id);}}>
                  <CancellableImage 
                    src={r.thumbnail_path} 
                    alt={config.getImageLabel(props.album, r.name)} 
                    loading="lazy" 
                    enableCancellation={true}
                    showLoadingPlaceholder={false}
                  />
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
          onSortChange={handleSortedItemsChange}
          initialSort={settings.image_sort}
          onSortUpdate={(sort) => handleSortUpdate(sort, 'image')}
        />
        <VirtualizedGallery
          images={localImages}
          targetHeight={targetHeight}
          gap={8}
          overscan={2}
          onImageClick={props.onImageClick}
          getImageLabel={getImageLabelForGallery}
          lastViewedImageId={props.lastViewedImage}
          showFaceBoxes={gallerySettings.showFaceBoxes}
          onFaceSearch={props.onFaceSearch}
          onFaceDelete={props.onFaceDelete}
          onPersonDelete={props.onPersonDelete}
          onSearchByName={props.onSearchByName}
          onSearchByPersonId={props.onSearchByPersonId}
        />
      </div>
    </>
  );
}