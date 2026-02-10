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
            onSearchByClusterId={(id) => baseProps.onSearchByClusterId?.(id)}
            onSearchByClusterName={(name) => baseProps.onSearchByClusterName?.(name)}
            showPeopleMenu={user?.roles?.includes('private') ?? false}
            showLocationsMenu={user?.roles?.includes('private') ?? false}
          />
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

