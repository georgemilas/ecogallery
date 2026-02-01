'use client';

import React from 'react';
import './gallery.css';
import { AlbumHierarchyView } from './AlbumHierarchyView';
import { BaseAlbumPage, BaseAlbumConfig, BaseAlbumPageProps } from '../../shared/BaseAlbumPage';

export function AlbumPage(): JSX.Element {
  const postSearchAlbum = async (expression: string, offset: number) => {
    // This will be called by the base component
  };

  const config: BaseAlbumConfig = {
    apiBaseUrl: '/api/v1/albums',
    basePath: '/album',
    requireAuth: true,
    useOriginalImage: true,
    renderHierarchyView: (props: BaseAlbumPageProps) => (
      <AlbumHierarchyView 
        key={props.router.query?.id || 'root'} 
        album={props.album} 
        onAlbumClick={props.handleAlbumClick} 
        onImageClick={props.handleImageClick} 
        lastViewedImage={props.lastViewedImage}
        settings={props.currentSettings}
        router={props.router}
        onSortChange={(settings) => {
          props.setCurrentSettings(settings);
        }}
        onSearchSubmit={props.onSearchSubmit!}
        onGetApiUrl={props.onGetApiUrl}
        clearLastViewedImage={() => props.setLastViewedImage(null)}
        onFaceSearch={props.onFaceSearch}
      />
    )
  };

  return <BaseAlbumPage config={config} />;
}