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
import { SearchEditor, SearchEditorState } from './SearchEditor';
import { SettingsModal } from './SettingsModal';
import { MenuPanel } from './MenuPanel';

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
  onSearchByClusterId?: (clusterId: number) => void;
  onSearchByClusterName?: (name: string) => void;
  onSortedImagesChange?: (images: ImageItemContent[]) => void;
  searchEditor?: SearchEditorState;
  config: BaseHierarchyConfig;
}

export function BaseHierarchyView(props: BaseHierarchyProps): JSX.Element {
  const [, forceUpdate] = React.useReducer(x => x + 1, 0);
  const [bannerEditMode, setBannerEditMode] = React.useState(false);
  const [showSettingsModal, setShowSettingsModal] = useState(false);
  const [showMenuPanel, setShowMenuPanel] = useState(false);
    const { user } = useAuth();
  const hasPrivateRole = user?.roles?.includes('private') ?? false;
  const { settings: gallerySettings, setShowFaceBoxes } = useGallerySettings();
  const { settings, onSortChange, config } = props;

  // Keyboard shortcuts
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      // Don't trigger if user is typing in an input field
      if (e.target instanceof HTMLInputElement || e.target instanceof HTMLTextAreaElement) {
        return;
      }

      // 's' or ',' (comma) to toggle settings modal (private role only)
      if (hasPrivateRole && (e.key === 's' || e.key === 'S' || e.key === ',')) {
        setShowSettingsModal(prev => !prev);
        return;
      }

      // 'm' to toggle menu panel (private role only)
      if (hasPrivateRole && (e.key === 'm' || e.key === 'M')) {
        setShowMenuPanel(prev => !prev);
        return;
      }

      // 'Escape' to close menu panel or settings modal
      if (e.key === 'Escape') {
        if (showSettingsModal) {
          setShowSettingsModal(false);
          return;
        }
        if (showMenuPanel) {
          setShowMenuPanel(false);
          return;
        }
      }

      // 'f' to toggle face boxes (private role only)
      if (hasPrivateRole && (e.key === 'f' || e.key === 'F')) {
        setShowFaceBoxes(!gallerySettings.showFaceBoxes);
      }
    };

    document.addEventListener('keydown', handleKeyDown);
    return () => document.removeEventListener('keydown', handleKeyDown);
  }, [hasPrivateRole, gallerySettings.showFaceBoxes, setShowFaceBoxes, showSettingsModal, showMenuPanel]);

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
    return gallerySettings.desktopThumbnailHeight; // Desktop
  };

  // Track responsive target height for gallery
  const [targetHeight, setTargetHeight] = useState(getResponsiveHeight());

  // Update target height on window resize or settings change
  useEffect(() => {
    setTargetHeight(getResponsiveHeight());
    const handleResize = debounce(() => {
      setTargetHeight(getResponsiveHeight());
    }, 150);
    window.addEventListener('resize', handleResize);
    return () => window.removeEventListener('resize', handleResize);
  }, [gallerySettings.desktopThumbnailHeight]);

  // Track sorted images with their data_id to detect stale data
  // TODO: Backend will provide data_id for unique identification
  const [sortedImagesState, setSortedImagesState] = useState<{
    dataId: string | number | undefined,
    images: ImageItemContent[]
  } | null>(null);

  // Track sorted albums with their data_id
  const [sortedAlbumsState, setSortedAlbumsState] = useState<{
    dataId: string | number | undefined,
    albums: AlbumItemContent[]
  } | null>(null);

  // Get the current data_id from album settings (search_id for searches, id for albums)
  const currentDataId = props.album.settings?.unique_data_id ?? props.album.id;

  // Derive localImages and localAlbums: use sorted if data_id matches, else use props
  const localImages = (sortedImagesState !== null && sortedImagesState.dataId === currentDataId)
    ? sortedImagesState.images
    : props.album.images;
  const localAlbums = (sortedAlbumsState !== null && sortedAlbumsState.dataId === currentDataId)
    ? sortedAlbumsState.albums
    : props.album.albums;

  const handleSortedItemsChange = useCallback((sortedItems: ImageItemContent[] | AlbumItemContent[]) => {
    props.clearLastViewedImage?.();
    const dataId = props.album.settings?.unique_data_id ?? props.album.id;
    if (Array.isArray(sortedItems) && sortedItems.length > 0) {
      // Type guard: ImageItemContent has 'is_movie' property, AlbumItemContent doesn't
      if ('is_movie' in sortedItems[0]) {
        const sorted = sortedItems as ImageItemContent[];
        setSortedImagesState({ dataId, images: sorted });
        // Report sorted images to parent immediately after sorting
        props.onSortedImagesChange?.(sorted);
      } else {
        setSortedAlbumsState({ dataId, albums: sortedItems as AlbumItemContent[] });
      }
    } else if (sortedItems.length === 0) {
      // Handle empty case
      setSortedImagesState({ dataId, images: [] });
      props.onSortedImagesChange?.([]);
    }
    forceUpdate();
  }, [props.clearLastViewedImage, props.onSortedImagesChange, props.album.settings?.unique_data_id, props.album.id]);

  // Handle sort value update: save to API and propagate AlbumSettings to parent
  const handleSortUpdate = useCallback(async (sortValue: string, target: 'album' | 'image') => {
    const newSettings = { ...props.settings };
    if (target === 'album') newSettings.album_sort = sortValue;
    if (target === 'image') newSettings.image_sort = sortValue;
    await saveSettings(newSettings);
    props.onSortChange?.(newSettings);
  }, [props.settings, props.onSortChange]);

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

  const renderMenuButton = () => (
    <button
      className="settings-button"
      onClick={() => setShowMenuPanel(true)}
      title="Menu"
      style={{
        background: 'none',
        border: 'none',
        cursor: 'pointer',
        padding: '4px 8px',
        display: 'flex',
        alignItems: 'center',
      }}
    >
      <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="#e8f09e" strokeWidth="2" strokeLinecap="round">
        <line x1="3" y1="6" x2="21" y2="6"/>
        <line x1="3" y1="12" x2="21" y2="12"/>
        <line x1="3" y1="18" x2="21" y2="18"/>
      </svg>
    </button>
  );

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
          {renderMenuButton()}
          {renderBreadcrumbs()}
        </div>
        {config.renderNavMenu(props)}
        {props.searchEditor && (
          <SearchEditor
            searchEditor={props.searchEditor}
            onSearchSubmit={(expr, offset) => props.onSearchSubmit?.(expr, offset)}
            showSearch={config.showSearch}
          />
        )}
      </div>
      
        <>
          <MenuPanel
            isOpen={showMenuPanel}
            onClose={() => setShowMenuPanel(false)}
            onSettingsClick={hasPrivateRole ? () => setShowSettingsModal(true) : ()=>{}}
            router={props.router}
          />
          {hasPrivateRole && (
          <SettingsModal isOpen={showSettingsModal} onClose={() => setShowSettingsModal(false)} />
          )}
        </>
      

      {localAlbums.length > 0 && (
        <div className='albums'>
          <SortControl
            type="albums"
            album={props.album}
            onSortChange={handleSortedItemsChange}
            initialSort={settings.album_sort}
            onSortUpdate={(sort) => handleSortUpdate(sort, 'album')}
          />
          <ul className='albums-container' style={{ display: 'grid', gap: `8px` }}>
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
          gap={gallerySettings.galleryGap}
          overscan={2}
          onImageClick={props.onImageClick}
          getImageLabel={getImageLabelForGallery}
          lastViewedImageId={props.lastViewedImage}
          showFaceBoxes={hasPrivateRole && gallerySettings.showFaceBoxes}
          onFaceSearch={props.onFaceSearch}
          onFaceDelete={props.onFaceDelete}
          onPersonDelete={props.onPersonDelete}
          onSearchByName={props.onSearchByName}
          onSearchByPersonId={props.onSearchByPersonId}
          onSearchByClusterId={hasPrivateRole ? props.onSearchByClusterId : undefined}
          onSearchByClusterName={hasPrivateRole ? props.onSearchByClusterName : undefined}
        />
      </div>
      <button
        className="scroll-to-top-button"
        onClick={() => window.scrollTo({ top: 0, behavior: 'instant' })}
        title="Scroll to Top"
        style={{
          position: 'fixed',
          bottom: '20px',
          right: '20px',
          zIndex: 1000,
          width: '40px',
          height: '40px',
          borderRadius: '50%',
          border: '1px solid #e8f09e',
          backgroundColor: 'rgba(0, 0, 0, 0.7)',
          color: '#e8f09e',
          cursor: 'pointer',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          boxShadow: '0 2px 8px rgba(0, 0, 0, 0.4)',
        }}
      >
        <svg width="18" height="18" viewBox="0 0 16 16" fill="currentColor">
          <path d="M8 2L4 6h3v8h2V6h3L8 2z"/>
        </svg>
      </button>
    </>
  );
}