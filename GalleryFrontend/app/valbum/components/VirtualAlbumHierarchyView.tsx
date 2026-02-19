import React from 'react';
import { AlbumItemHierarchy, ImageItemContent, AlbumSettings } from '../../album/components/AlbumHierarchyProps';
import { GalleryPickerState } from '../../album/components/GalleryPicker';
import { BaseHierarchyView, BaseHierarchyConfig, BaseHierarchyProps } from '../../album/components/BaseHierarchyView';
import { AppRouterInstance } from 'next/dist/shared/lib/app-router-context.shared-runtime';
import { useAuth } from '@/app/contexts/AuthContext';
import { ViewMenu } from '../../album/components/ViewMenu';

export interface VirtualAlbumHierarchyProps {
  album: AlbumItemHierarchy;
  onAlbumClick: (albumId: number | null) => void;
  onImageClick: (image: ImageItemContent) => void;
  onSearchSubmit: (expression: string, offset: number) => void;
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
  searchEditor?: {
    isOpen: boolean;
    setIsOpen: (open: boolean) => void;
    text: string;
    setText: (text: string) => void;
    error: string | null;
    clearError: () => void;
    panelWidth: number;
    setPanelWidth: (width: number) => void;
  };
  showAlbumManager?: boolean;
  setShowAlbumManager?: (show: boolean) => void;
  galleryPicker?: GalleryPickerState;
}

export function VirtualAlbumHierarchyView(props: VirtualAlbumHierarchyProps): JSX.Element {
  const { user } = useAuth();

  const config: BaseHierarchyConfig = {
    settingsApiEndpoint: '/api/v1/valbums/settings',
    showSearch: user?.roles?.includes('album_admin') ?? false,
    getImageLabel: (album, imageName) => imageName,
    renderNavMenu: (baseProps) => {
      const navigateOrRefresh = (path: string, apiEndpoint: string) => {
        if (window.location.pathname === path) {
          baseProps.onGetApiUrl(apiEndpoint);
        } else {
          baseProps.router.push(path);
        }
      };
      const goHome = () => {
        baseProps.onAlbumClick(null);
      };
      return (
        <nav className="menu">
          <button onClick={goHome} className="page-button" title="Home">
            <svg width="16" height="16" viewBox="0 0 16 16" fill="currentColor" style={{verticalAlign: 'middle', marginTop: '0px', marginLeft: '2px', marginBottom: '4px', marginRight: '2px'}}>
              <path d="M8 2L2 7v7h4v-4h4v4h4V7L8 2z"/>
            </svg>Home
          </button>
          {user?.roles?.includes('private') ? (
            <button
              onClick={() => baseProps.router.push('/album')}
              className="page-button"
              title="Go to private albums"
            >
              Private
            </button>
          ) : !user ? (
            <button
              onClick={() => baseProps.router.push('/login')}
              className="page-button"
              title="Login to access private albums"
            >
              Login
            </button>
          ) : null}
          <ViewMenu
            onPersonClick={(personName) => baseProps.onSearchByName?.(personName)}
            onRandomClick={() => navigateOrRefresh('/valbum/random', 'random')}
            onRecentClick={() => navigateOrRefresh('/valbum/recent', 'recent')}
            showPeopleMenu={user?.roles?.includes('private') ?? false}
            showLocationsMenu={user?.roles?.includes('private') ?? false}
            onSearchByClusterId={(id) => baseProps.onSearchByClusterId?.(id)}
            onSearchByClusterName={(name) => baseProps.onSearchByClusterName?.(name)}
          />
        </nav>
      );
    }
  };

  // Convert VirtualAlbumHierarchyProps to BaseHierarchyProps
  const baseProps: BaseHierarchyProps = {
    ...props,
    config
  };

  return <BaseHierarchyView {...baseProps} />;
}

