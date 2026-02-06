import React from 'react';
import { AlbumHierarchyProps } from './AlbumHierarchyProps';
import { BaseHierarchyView, BaseHierarchyConfig, BaseHierarchyProps } from './BaseHierarchyView';
import { useAuth } from '@/app/contexts/AuthContext';
import { ViewMenu } from './ViewMenu';

export function AlbumHierarchyView(props: AlbumHierarchyProps): JSX.Element {
  const { user } = useAuth();

  const config: BaseHierarchyConfig = {
    settingsApiEndpoint: '/api/v1/albums/settings',
    showSearch: true,
    getImageLabel: (album, imageName) => album.get_name(imageName),
    renderNavMenu: (baseProps) => {
      const navigateOrRefresh = (path: string, apiEndpoint: string) => {
        if (window.location.pathname === path) {
          baseProps.onGetApiUrl(apiEndpoint);
        } else {
          baseProps.router.push(path);
        }
      };
      const goHome = () => {
        // Always reload the root album (handles case when viewing search results)
        baseProps.onAlbumClick(null);
      };
      return (
        <nav className="menu">
          <button onClick={goHome} className="page-button" title="Home">
            <svg width="16" height="16" viewBox="0 0 16 16" fill="currentColor" style={{verticalAlign: 'middle', marginTop: '0px', marginLeft: '2px', marginBottom: '4px', marginRight: '2px'}}>
              <path d="M8 2L2 7v7h4v-4h4v4h4V7L8 2z"/>
            </svg>Home
          </button>
          <button onClick={() => baseProps.router.push('/valbum')} className="page-button" title="Public Albums">Public</button>
          <ViewMenu
            onPersonClick={(personName) => baseProps.onSearchByName?.(personName)}
            onRandomClick={() => navigateOrRefresh('/album/random', 'random')}
            onRecentClick={() => navigateOrRefresh('/album/recent', 'recent')}
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

  // Convert AlbumHierarchyProps to BaseHierarchyProps
  const baseProps: BaseHierarchyProps = {
    ...props,
    config
  };

  return <BaseHierarchyView {...baseProps} />;
}

