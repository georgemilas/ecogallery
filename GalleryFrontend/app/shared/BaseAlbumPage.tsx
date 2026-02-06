import React, { useEffect, useRef, useState } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';
import { AlbumItemHierarchy, ImageItemContent, AlbumSettings } from '../album/components/AlbumHierarchyProps';
import { ImageView } from '../album/components/ImageView';
import { apiFetch } from '@/app/utils/apiFetch';
import { useAuth } from '@/app/contexts/AuthContext';
import { useGallerySettings } from '@/app/contexts/GallerySettingsContext';

export interface SearchEditorState {
  isOpen: boolean;
  setIsOpen: (open: boolean) => void;
  text: string;
  setText: (text: string) => void;
  error: string | null;
  clearError: () => void;
}

export interface BaseAlbumConfig {
  apiBaseUrl: string; // '/api/v1/albums' or '/api/v1/valbums'
  basePath: string; // '/album' or '/valbum'
  requireAuth: boolean;
  useOriginalImage: boolean;
  renderHierarchyView: (props: BaseAlbumPageProps) => React.ReactNode;
  onSearchSubmit?: (expression: string, offset: number) => void;
  initialView?: 'random' | 'recent'; // For /album/random and /album/recent routes
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
  onFaceSearch?: (personId: number, personName: string | null) => void;
  onFaceDelete?: (faceId: number) => void;
  onPersonDelete?: (personId: number) => void;
  onSearchByName?: (name: string) => void;
  onSearchByPersonId?: (personId: number) => void;
  onSortedImagesChange?: (images: ImageItemContent[]) => void;
  searchEditor: SearchEditorState;
  config: BaseAlbumConfig;
}

export function BaseAlbumPage({ config }: { config: BaseAlbumConfig }): JSX.Element {
  const { user, loading: authLoading } = useAuth();
  const { settings: gallerySettings } = useGallerySettings();
  const [album, setAlbum] = useState<AlbumItemHierarchy | null>(null);
  const [loading, setLoading] = useState(true);
  const [isFullscreen, setIsFullscreen] = useState(false);
  const [lastViewedImage, setLastViewedImage] = useState<number | null>(null);
  const router = useRouter();
  const searchParams = useSearchParams();
  const albumIdParam = searchParams.get('id') ? parseInt(searchParams.get('id') || '', 10) : null;
  const imageIdParam = searchParams.get('image') ? parseInt(searchParams.get('image') || '', 10) : null;
  const faceSearchParam = searchParams.get('faceSearch');
  const viewParam = searchParams.get('view'); // 'random' or 'recent'
  const [currentSettings, setCurrentSettings] = useState<AlbumSettings>(album ? album.settings : {} as AlbumSettings);
  const [sortedImages, setSortedImages] = useState<ImageItemContent[]>([]);
  const [searchEditorOpen, setSearchEditorOpen] = useState(false);
  const [searchEditorText, setSearchEditorText] = useState('');
  const [searchError, setSearchError] = useState<string | null>(null);
  const fetchedRef = useRef(false);

  const searchEditor: SearchEditorState = {
    isOpen: searchEditorOpen,
    setIsOpen: setSearchEditorOpen,
    text: searchEditorText,
    setText: setSearchEditorText,
    error: searchError,
    clearError: () => setSearchError(null),
  };

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
  // Use sorted images for lookup/navigation so ImageView matches gallery order
  const effectiveImages = sortedImages.length > 0 ? sortedImages : (album?.images || []);
  const selectedImage = effectiveImages.find(item => item.id === imageIdParam) || null;

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
    setSearchError(null); // Clear previous error
    try {
      const url = `${config.apiBaseUrl}/search`;
      console.log('Searching albums:', { expression, url });
      var searchInfo = album != null && album.search_info ? { ...album.search_info, expression: expression, offset: offset, limit: gallerySettings.searchPageSize } : { expression: expression, limit: gallerySettings.searchPageSize, offset: offset, count: 0, group_by_p_hash: true };
      console.log('Using search info:', searchInfo);
      const res = await apiFetch(url, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(searchInfo)
      });
      if (!res.ok) {
        // Try to get error message from response
        let errorMessage = `Search failed (${res.status})`;
        try {
          const errorData = await res.json();
          if (errorData.message || errorData.error || errorData.detail) {
            errorMessage = errorData.message || errorData.error || errorData.detail;
          }
        } catch {
          // If response isn't JSON, try text
          try {
            const errorText = await res.text();
            if (errorText) errorMessage = errorText;
          } catch {}
        }
        console.error('Search failed:', errorMessage);
        setSearchError(errorMessage);
        // Keep current album visible, don't set to null
      } else {
        const data = await res.json();
        setAlbum(convertToAlbum(data));
      }
    } catch (e) {
      const errorMessage = e instanceof Error ? e.message : 'Search failed';
      console.error('Error searching albums', e);
      setSearchError(errorMessage);
      // Keep current album visible, don't set to null
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

  // Internal function to fetch face search results
  const doFaceSearch = async (searchParam: string) => {
    setLoading(true);
    try {
      // Parse the search param: "person:123" or "name:John"
      const [type, value] = searchParam.split(':');
      const endpoint = type === 'name'
        ? `/api/v1/faces/search/name/${encodeURIComponent(value)}`
        : `/api/v1/faces/search/person/${value}`;
      console.log('Searching faces:', { searchParam, endpoint });
      const res = await apiFetch(endpoint);
      if (!res.ok) {
        console.error('Face search failed', res.status);
        setAlbum(null);
      } else {
        const data = await res.json();
        setAlbum(convertToAlbum(data));
      }
    } catch (e) {
      console.error('Error searching faces', e);
      setAlbum(null);
    } finally {
      setLoading(false);
    }
  };

  // Navigate to face search via URL (for browser back button support)
  const navigateToFaceSearch = (personId: number, personName: string | null) => {
    const searchParam = personName
      ? `name:${personName}`
      : `person:${personId}`;
    router.push(`${config.basePath}?faceSearch=${encodeURIComponent(searchParam)}`);
  };

  // Navigate to search by name only
  const navigateToSearchByName = (name: string) => {
    router.push(`${config.basePath}?faceSearch=${encodeURIComponent(`name:${name}`)}`);
  };

  // Navigate to search by person ID only
  const navigateToSearchByPersonId = (personId: number) => {
    router.push(`${config.basePath}?faceSearch=${encodeURIComponent(`person:${personId}`)}`);
  };

  // Handle face deletion - refresh to update the view
  const handleFaceDelete = (faceId: number) => {
    console.log('Face deleted:', faceId);
    // Refresh the current album to update the UI
    if (faceSearchParam) {
      doFaceSearch(faceSearchParam);
    } else {
      fetchAlbum(albumIdParam);
    }
  };

  // Handle person deletion - refresh to update the view
  const handlePersonDelete = (personId: number) => {
    console.log('Person deleted:', personId);
    // Refresh the current album to update the UI
    if (faceSearchParam) {
      doFaceSearch(faceSearchParam);
    } else {
      fetchAlbum(albumIdParam);
    }
  };

  // Reset fetch guard when the actual query parameters change
  useEffect(() => {
    fetchedRef.current = false;
  }, [albumIdParam, faceSearchParam, viewParam]);

  useEffect(() => {
    // Wait for auth to settle
    if (authLoading) return;

    // Handle auth requirements
    if (config.requireAuth && !user) {
      router.push('/login');
      return;
    }

    // Prevent duplicate fetches when auth state settles in multiple steps
    if (fetchedRef.current) return;
    fetchedRef.current = true;

    // Handle different view modes - route-based initialView takes priority
    const effectiveView = config.initialView || viewParam;
    if (faceSearchParam) {
      doFaceSearch(faceSearchParam);
    } else if (effectiveView === 'random' || effectiveView === 'recent') {
      getApiUrl(effectiveView);
    } else {
      fetchAlbum(albumIdParam);
    }
  }, [albumIdParam, faceSearchParam, viewParam, authLoading, user, config.requireAuth]);

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
    // Use current pathname to preserve routes like /album/random, /album/recent, search results
    router.push(`${window.location.pathname}?${currentParams.toString()}`);
  };

  const handleCloseImage = () => {
    if (imageIdParam) {
      setLastViewedImage(imageIdParam);
    }
    const currentParams = new URLSearchParams(window.location.search);
    currentParams.delete('image');
    // Use current pathname to preserve routes like /album/random, /album/recent, search results
    router.push(`${window.location.pathname}?${currentParams.toString()}`);
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
    onFaceSearch: navigateToFaceSearch,
    onFaceDelete: handleFaceDelete,
    onPersonDelete: handlePersonDelete,
    onSearchByName: navigateToSearchByName,
    onSearchByPersonId: navigateToSearchByPersonId,
    onSortedImagesChange: setSortedImages,
    searchEditor,
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
              album={Object.assign(Object.create(Object.getPrototypeOf(album)), album, { images: effectiveImages })}
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
        <>
        <p>No album found.</p>
        <a href="#" className="home-link" onClick={e => { e.preventDefault(); router.push('/album'); }}
            style={{ color: '#667eea', textDecoration: 'underline', fontSize: '15px' }}>Home
          </a>
        </>
      )}
    </main>
  );
}