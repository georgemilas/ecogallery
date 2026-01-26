import React, { useEffect, useState } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';
import { AlbumItemHierarchy, ImageItemContent, AlbumSettings } from '../album/components/AlbumHierarchyProps';
import { ImageView } from '../album/components/ImageView';
import { apiFetch } from '@/app/utils/apiFetch';
import { useAuth } from '@/app/contexts/AuthContext';

export interface BaseAlbumConfig {
  apiBaseUrl: string; // '/api/v1/albums' or '/api/v1/valbums'
  basePath: string; // '/album' or '/valbum'
  requireAuth: boolean;
  useOriginalImage: boolean;
  renderHierarchyView: (props: BaseAlbumPageProps) => React.ReactNode;
  onSearchSubmit?: (expression: string, offset: number) => void;
}

export interface BaseAlbumPageProps {
  album: AlbumItemHierarchy;
  loading: boolean;
  isFullscreen: boolean;
  lastViewedImage: number | null;
  router: any;
  currentSettings: AlbumSettings;
  handleAlbumClick: (newAlbumId: number | null) => void;
  handleImageClick: (image: ImageItemContent) => void;
  handleCloseImage: () => void;
  setCurrentSettings: (settings: AlbumSettings) => void;
  setLastViewedImage: (id: number | null) => void;
  onGetApiUrl: (apiUrl: string) => void;
  onSortChange: (settings: AlbumSettings) => void;
  onSearchSubmit?: (expression: string, offset: number) => void;
  config: BaseAlbumConfig;
}

export function BaseAlbumPage({ config }: { config: BaseAlbumConfig }): JSX.Element {
  const { user, loading: authLoading } = useAuth();
  const [album, setAlbum] = useState<AlbumItemHierarchy | null>(null);
  const [loading, setLoading] = useState(true);
  const [isFullscreen, setIsFullscreen] = useState(false);
  const [lastViewedImage, setLastViewedImage] = useState<number | null>(null);
  const router = useRouter();
  const searchParams = useSearchParams();
  const albumIdParam = searchParams.get('id') ? parseInt(searchParams.get('id') || '', 10) : null;
  const imageIdParam = searchParams.get('image') ? parseInt(searchParams.get('image') || '', 10) : null;
  const [currentSettings, setCurrentSettings] = useState<AlbumSettings>(album ? album.settings : {} as AlbumSettings);

  // Sync currentSettings with album.settings when album changes
  useEffect(() => {
    if (album?.settings) setCurrentSettings(album.settings);
  }, [album?.settings]);

    // Helper to update sort in URL
    const updateSortInUrl = (newSettings: AlbumSettings) => {
      const currentParams = new URLSearchParams(window.location.search);
      if (newSettings.album_sort) currentParams.set('albumSort', newSettings.album_sort);
      if (newSettings.image_sort) currentParams.set('imageSort', newSettings.image_sort);
      router.push(`${config.basePath}?${currentParams.toString()}`);
    }
  const viewMode = imageIdParam ? 'image' : 'gallery';
  const selectedImage = album?.images.find(item => item.id === imageIdParam) || null;

  const convertToAlbum = (obj: any): AlbumItemHierarchy => {
    const album = Object.assign(new AlbumItemHierarchy(), obj);
    if (album.content) {
      album.content = album.content.map(convertToAlbum);
    }
    return album;
  };

  const fetchAlbum = async (albumId: number | null) => {
    setLoading(true);
    try {
      const albumPath = albumId !== null ? `/${albumId}` : '';
      const url = `${config.apiBaseUrl}${albumPath}`;
      console.log('Fetching album:', { albumId, url });
      const res = await apiFetch(url);
      if (!res.ok) {
        console.error('Failed to fetch album', res.status);
        setAlbum(null);
      } else {
        const data = await res.json();
        setAlbum(convertToAlbum(data));
      }
    } catch (e) {
      console.error('Error fetching album', e);
      setAlbum(null);
    } finally {
      setLoading(false);
    }
  };

  const postSearchAlbum = async (expression: string, offset: number) => {
    setLoading(true);
    try {
      const url = `${config.apiBaseUrl}/search`;
      console.log('Searching albums:', { expression, url });
      const DefaultLimit = 2000;
      var searchInfo = album != null && album.search_info ? { ...album.search_info, expression: expression, offset: offset } : { expression: expression, limit: DefaultLimit, offset: offset, count: 0, group_by_p_hash: true };
      console.log('Using search info:', searchInfo);
      const res = await apiFetch(url, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(searchInfo)
      });
      if (!res.ok) {
        console.error('Search failed', res.status);
        setAlbum(null);
      } else {
        const data = await res.json();
        setAlbum(convertToAlbum(data));
      }
    } catch (e) {
      console.error('Error searching albums', e);
      setAlbum(null);
    } finally {
      setLoading(false);
    }
  };

  const getApiUrl = async (apiUrl: string) => {
    setLoading(true);
    try {
      const url = `${config.apiBaseUrl}/${apiUrl}`;
      console.log('Fetching apiUrl:', { raw: apiUrl, url });
      const res = await apiFetch(url);
      if (!res.ok) {
        console.error('Failed to fetch api url', res.status);
        setAlbum(null);
      } else {
        const data = await res.json();
        setAlbum(convertToAlbum(data));
      }
    } catch (e) {
      console.error('Error fetching api url', e);
      setAlbum(null);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    // Handle auth requirements
    if (config.requireAuth) {
      if (authLoading) return;
      if (!user) {
        router.push('/login');
        return;
      }
    }
    fetchAlbum(albumIdParam);
  }, [albumIdParam, authLoading, user, config.requireAuth]);

  // Fullscreen handling
  useEffect(() => {
    console.log('Setting up fullscreen listener');
    
    const handleFullscreenChange = () => {
      const isFullscreenNow = !!(
        document.fullscreenElement ||
        (document as any).webkitFullscreenElement ||
        (document as any).mozFullScreenElement ||
        (document as any).msFullscreenElement
      );
      console.log('Fullscreen changed:', isFullscreenNow);
      setIsFullscreen(isFullscreenNow);
    };

    document.addEventListener('fullscreenchange', handleFullscreenChange);
    document.addEventListener('webkitfullscreenchange', handleFullscreenChange);
    document.addEventListener('mozfullscreenchange', handleFullscreenChange);
    document.addEventListener('MSFullscreenChange', handleFullscreenChange);
    
    handleFullscreenChange();
    
    return () => {
      console.log('Removing fullscreen listener');
      document.removeEventListener('fullscreenchange', handleFullscreenChange);
      document.removeEventListener('webkitfullscreenchange', handleFullscreenChange);
      document.removeEventListener('mozfullscreenchange', handleFullscreenChange);
      document.removeEventListener('MSFullscreenChange', handleFullscreenChange);
    };
  }, []);

  const handleAlbumClick = (newAlbumId: number | null) => {
    const params = new URLSearchParams({
      albumSort: currentSettings.album_sort,
      imageSort: currentSettings.image_sort
    });
    if (newAlbumId) {params.set('id', newAlbumId.toString());}
    const targetUrl = `${config.basePath}?${params.toString()}`;
    const currentUrl = `${window.location.pathname}${window.location.search}`;
    if (currentUrl === targetUrl) {
      fetchAlbum(newAlbumId);
      router.refresh();
    } else {
      router.push(targetUrl);
    }
  };

  const handleImageClick = (image: ImageItemContent) => {
    const currentParams = new URLSearchParams(window.location.search);
    currentParams.set('image', image.id.toString());
    currentParams.set('albumSort', currentSettings.album_sort);
    currentParams.set('imageSort', currentSettings.image_sort);
    router.push(`${config.basePath}?${currentParams.toString()}`);
  };

  const handleCloseImage = () => {
    if (imageIdParam) {
      setLastViewedImage(imageIdParam);
    }
    const currentParams = new URLSearchParams(window.location.search);
    currentParams.delete('image');
    router.push(`${config.basePath}?${currentParams.toString()}`);
  };

  const baseProps: BaseAlbumPageProps = {
    album: album!,
    loading,
    isFullscreen,
    lastViewedImage,
    router,
    currentSettings,
    handleAlbumClick,
    handleImageClick,
    handleCloseImage,
    setCurrentSettings,
    setLastViewedImage,
    onSortChange: (settings: AlbumSettings) => {
      setCurrentSettings(settings);
      updateSortInUrl(settings);
    },
    onGetApiUrl: getApiUrl,
    onSearchSubmit: config.onSearchSubmit || postSearchAlbum,
    config
  };

  return (
    <main>
      {loading ? (
        <p>Loading...</p>
      ) : album ? (
        <>
          {viewMode === 'gallery' && config.renderHierarchyView(baseProps)}
          {viewMode === 'image' && selectedImage && (
            <ImageView 
              image={selectedImage} 
              album={album} 
              onAlbumClick={handleAlbumClick} 
              onClose={handleCloseImage} 
              router={router}
              isFullscreen={isFullscreen}
              setIsFullscreen={setIsFullscreen}
              path={config.basePath}
              useOriginalImage={config.useOriginalImage}
            />
          )}
          {viewMode === 'image' && !selectedImage && (
            <div>
              <p>Image not found in current album.</p>
              <button onClick={handleCloseImage}>Back to Gallery</button>
            </div>
          )}
        </>
      ) : (
        <p>No album found.</p>
      )}
    </main>
  );
}