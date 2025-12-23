'use client';

import './gallery.css';

import { AlbumHierarchyView } from './AlbumHierarchyView';
import { AlbumItemHierarchy, ImageItemContent, AlbumSettings } from './AlbumHierarchyProps';
import { ImageView } from './ImageView';
import { useEffect, useState } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';
import { apiFetch } from '@/app/utils/apiFetch';
import { useAuth } from '@/app/contexts/AuthContext';

export function AlbumPage(): JSX.Element {
  const { user, loading: authLoading } = useAuth();
  const [album, setAlbum] = useState<AlbumItemHierarchy | null>(null);
  const [loading, setLoading] = useState(true);
  const [isFullscreen, setIsFullscreen] = useState(false);
  const [lastViewedImage, setLastViewedImage] = useState<number | null>(null);
  const router = useRouter();
  const searchParams = useSearchParams();
  const albumNameParam = searchParams.get('name') || '';
  const imageIdParam = searchParams.get('image') ? parseInt(searchParams.get('image') || '', 10) : null;
  const [currentSettings, setCurrentSettings] = useState<AlbumSettings>(album ? album.settings : {} as AlbumSettings);
  
  
    // Sync currentSettings with album.settings when album changes
  useEffect(() => {
    if (album?.settings) setCurrentSettings(album.settings);
  }, [album?.settings]);


  const viewMode = imageIdParam ? 'image' : 'gallery';
  const selectedImage = album?.images.find(item => item.id === imageIdParam) || null;

  const fetchAlbum = async (albumNameParam: string = '') => {
    setLoading(true);
    try {
      const encodedAlbumName = albumNameParam ? encodeURIComponent(albumNameParam) : '';
      const apiBase = process.env.NEXT_PUBLIC_API_BASE || '';
      const url = apiBase ? `${apiBase}/api/v1/albums/${encodedAlbumName}` : `/api/v1/albums/${encodedAlbumName}`;
      console.log('Fetching album:', { raw: albumNameParam, encoded: encodedAlbumName, url });
      const res = await apiFetch(url);
      if (!res.ok) {
        console.error('Failed to fetch album', res.status);
        setAlbum(null);
      } else {
        const data = await res.json();  // We need to convert plain json data object to AlbumHierarchy class instances
        const convertToAlbum = (obj: any): AlbumItemHierarchy => {
          const album = Object.assign(new AlbumItemHierarchy(), obj);
          if (album.content) { 
            album.content = album.content.map(convertToAlbum);  //Recursively convert nested content
          }
          return album;
        };
        setAlbum(convertToAlbum(data));
      }
    } catch (e) {
      console.error('Error fetching album', e);
      setAlbum(null);
    } finally {
      setLoading(false);
    }
  };

  const postSearchAlbum = async (expression: string) => {
    setLoading(true);
    try {
      const apiBase = process.env.NEXT_PUBLIC_API_BASE || '';
      const url = apiBase ? `${apiBase}/api/v1/albums/search` : `/api/v1/albums/search`;
      console.log('Searching albums:', { expression, url });
      const res = await apiFetch(url, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ expression })
      });
      if (!res.ok) {
        console.error('Search failed', res.status);
        setAlbum(null);
      } else {
        const data = await res.json();
        const convertToAlbum = (obj: any): AlbumItemHierarchy => {
          const album = Object.assign(new AlbumItemHierarchy(), obj);
          if (album.content) {
            album.content = album.content.map(convertToAlbum);
          }
          return album;
        };
        setAlbum(convertToAlbum(data));
        // Do not change route; show results directly
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
      const apiBase = process.env.NEXT_PUBLIC_API_BASE || '';
      const url = apiBase ? `${apiBase}/api/v1/albums/${apiUrl}` : `/api/v1/albums/${apiUrl}`;
      console.log('Fetching apiUrl:', { raw: apiUrl, url });
      const res = await apiFetch(url);
      if (!res.ok) {
        console.error('Failed to fetch api url', res.status);
        setAlbum(null);
      } else {
        const data = await res.json();  // We need to convert plain json data object to AlbumHierarchy class instances
        const convertToAlbum = (obj: any): AlbumItemHierarchy => {
          const album = Object.assign(new AlbumItemHierarchy(), obj);
          if (album.content) { 
            album.content = album.content.map(convertToAlbum);  //Recursively convert nested content
          }
          return album;
        };
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
    // Wait for auth to resolve; if no user, redirect and skip fetch
    if (authLoading) return;
    if (!user) {
      router.push('/login');
      return;
    }

    fetchAlbum(albumNameParam);
  }, [albumNameParam, authLoading, user]);

  
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
    
    // Check initial state
    handleFullscreenChange();
    
    return () => {
      console.log('Removing fullscreen listener');
      document.removeEventListener('fullscreenchange', handleFullscreenChange);
      document.removeEventListener('webkitfullscreenchange', handleFullscreenChange);
      document.removeEventListener('mozfullscreenchange', handleFullscreenChange);
      document.removeEventListener('MSFullscreenChange', handleFullscreenChange);
    };
  }, []);

  const handleAlbumClick = (newAlbumName: string) => {
    // Build URL with properly encoded params
    const params = new URLSearchParams({
      albumSort: currentSettings.album_sort,
      imageSort: currentSettings.image_sort
    });
    if (newAlbumName) {params.set('name', newAlbumName);}
    const targetUrl = `/album?${params.toString()}`;
    const currentUrl = `${window.location.pathname}${window.location.search}`;
    if (currentUrl === targetUrl) {
      // If URL is unchanged (e.g., navigating to root when already at root), force a refresh/fetch
      fetchAlbum(newAlbumName || '');
      router.refresh();
    } else {
      router.push(targetUrl);
    }
  };

  const handleImageClick = (image: ImageItemContent) => {
    const currentParams = new URLSearchParams(window.location.search);
    currentParams.set('image', image.id.toString());
    // Persist current sort preferences to URL on navigation
    currentParams.set('albumSort', currentSettings.album_sort);
    currentParams.set('imageSort', currentSettings.image_sort);
    router.push(`/album?${currentParams.toString()}`);
  };

  const handleCloseImage = () => {
    // Save the current image name before closing
    if (imageIdParam) {
      setLastViewedImage(imageIdParam);
    }
    const currentParams = new URLSearchParams(window.location.search);
    currentParams.delete('image');
    router.push(`/album?${currentParams.toString()}`);
  };

  return (
    <main>
      {loading ? (
        <p>Loading...</p>
      ) : album ? (
        <>
          {viewMode === 'gallery' && (
            <AlbumHierarchyView 
              key={albumNameParam} 
              album={album} 
              onAlbumClick={handleAlbumClick} 
              onImageClick={handleImageClick} 
              lastViewedImage={lastViewedImage}
              settings={currentSettings}
              router={router}
              onSortChange={(settings) => {
                // Update local state immediately without routing
                setCurrentSettings(settings);
              }}
              onSearchSubmit={(expr) => postSearchAlbum(expr)}
              onGetApiUrl={(apiUrl) => getApiUrl(apiUrl)} 
              clearLastViewedImage={() => setLastViewedImage(null)}
            />
          )}
          {viewMode === 'image' && selectedImage && (
            <ImageView 
              image={selectedImage} 
              album={album} 
              onAlbumClick={handleAlbumClick} 
              onClose={handleCloseImage} 
              router={router}
              isFullscreen={isFullscreen}
              setIsFullscreen={setIsFullscreen}
              path="/album"
              useOriginalImage={true}
            />
          )}
        </>
      ) : (
        <p>No album found.</p>
      )}
    </main>
  );
}