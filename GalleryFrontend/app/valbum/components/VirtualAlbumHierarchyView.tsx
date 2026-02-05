import React from 'react';
import { AlbumItemHierarchy, ImageItemContent, AlbumSettings } from '../../album/components/AlbumHierarchyProps';
import { BaseHierarchyView, BaseHierarchyConfig, BaseHierarchyProps } from '../../album/components/BaseHierarchyView';
import { AppRouterInstance } from 'next/dist/shared/lib/app-router-context.shared-runtime';
import { useAuth } from '@/app/contexts/AuthContext';
import { ViewMenu } from '../../album/components/ViewMenu';

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
  onFaceSearch?: (personId: number, personName: string | null) => void;
  onFaceDelete?: (faceId: number) => void;
  onPersonDelete?: (personId: number) => void;
  onSearchByName?: (name: string) => void;
  onSearchByPersonId?: (personId: number) => void;
  onSortedImagesChange?: (images: ImageItemContent[]) => void;
}

export function VirtualAlbumHierarchyView(props: VirtualAlbumHierarchyProps): JSX.Element {
  const { user } = useAuth();

  const config: BaseHierarchyConfig = {
    settingsApiEndpoint: '/api/v1/valbums/settings',
    showSearch: false,
    getImageLabel: (album, imageName) => imageName,
    renderNavMenu: (baseProps) => {
      const navigateOrRefresh = (path: string, apiEndpoint: string) => {
        if (window.location.pathname === path) {
          baseProps.onGetApiUrl(apiEndpoint);
        } else {
          baseProps.router.push(path);
        }
      };
      return (
        <nav className="menu">
          <button onClick={() => baseProps.router.push('/valbum')} className="page-button" title="Home">
            <svg width="16" height="16" viewBox="0 0 16 16" fill="currentColor" style={{verticalAlign: 'middle', marginTop: '0px', marginLeft: '2px', marginBottom: '4px', marginRight: '2px'}}>
              <path d="M8 2L2 7v7h4v-4h4v4h4V7L8 2z"/>
            </svg>Home
          </button>
          <button
            onClick={() => baseProps.router.push(user ? '/album' : '/login')}
            className="page-button"
            title={user ? 'Go to private albums' : 'Login to access private albums'}
          >
            {user ? 'Private' : 'Login'}
          </button>
          <ViewMenu
            onRandomClick={() => navigateOrRefresh('/valbum/random', 'random')}
            onRecentClick={() => navigateOrRefresh('/valbum/recent', 'recent')}
            showPeopleMenu={false}
          />
          <button onClick={() => window.scrollTo({ top: 0, behavior: 'instant' })} className="page-button" title="Scroll to Top">
            <svg width="16" height="16" viewBox="0 0 16 16" fill="currentColor" style={{verticalAlign: 'middle', marginTop: '0', marginLeft: '4px', marginBottom: '4px', marginRight: '4px'}}>
              <path d="M8 2L4 6h3v8h2V6h3L8 2z"/>
            </svg>
            Top
          </button>
        </nav>
      );
    }
  };

  // Convert VirtualAlbumHierarchyProps to BaseHierarchyProps
  const baseProps: BaseHierarchyProps = {
    ...props,
    onSearchSubmit: (expression: string, offset: number) => {
      // VirtualAlbumHierarchyView doesn't use offset, so we ignore it
      props.onSearchSubmit(expression);
    },
    config
  };

  return <BaseHierarchyView {...baseProps} />;
}

