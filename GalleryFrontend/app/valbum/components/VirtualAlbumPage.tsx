import React from 'react';
import '../../album/components/gallery.css';
import { VirtualAlbumHierarchyView } from './VirtualAlbumHierarchyView';
import { BaseAlbumPage, BaseAlbumConfig, BaseAlbumPageProps } from '../../shared/BaseAlbumPage';

export interface VirtualAlbumPageProps {
  initialView?: 'random' | 'recent';
}

export function VirtualAlbumPage({ initialView }: VirtualAlbumPageProps = {}): JSX.Element {
  const config: BaseAlbumConfig = {
    apiBaseUrl: '/api/v1/valbums',
    basePath: '/valbum',
    requireAuth: false,
    useOriginalImage: false,
    initialView,
    renderHierarchyView: (props: BaseAlbumPageProps) => (
      <VirtualAlbumHierarchyView
        key={props.router.query?.album}
        album={props.album}
        onAlbumClick={props.handleAlbumClick}
        onImageClick={props.handleImageClick}
        lastViewedImage={props.lastViewedImage}
        settings={props.currentSettings}
        router={props.router}
        onSortChange={props.onSortChange}
        onSearchSubmit={props.onSearchSubmit!}
        onGetApiUrl={props.onGetApiUrl}
        clearLastViewedImage={() => props.setLastViewedImage(null)}
        onFaceSearch={props.onFaceSearch}
        onFaceDelete={props.onFaceDelete}
        onPersonDelete={props.onPersonDelete}
        onSearchByName={props.onSearchByName}
        onSearchByPersonId={props.onSearchByPersonId}
        onSearchByClusterId={props.onSearchByClusterId}
        onSearchByClusterName={props.onSearchByClusterName}
        onSortedImagesChange={props.onSortedImagesChange}
        searchEditor={props.searchEditor}
        showAlbumManager={props.showAlbumManager}
        setShowAlbumManager={props.setShowAlbumManager}
        galleryPicker={props.galleryPicker!}
      />
    )
  };

  return <BaseAlbumPage config={config} />;
}