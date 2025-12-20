import '../../album/components/gallery.css';

import { VirtualAlbumHierarchyView } from './VirtualAlbumHierarchyView';
import { AlbumItemHierarchy, ImageItemContent } from '../../album/components/AlbumHierarchyProps';
import { ImageView } from '../../album/components/ImageView';
import { useEffect, useState } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';

export function VirtualAlbumPage(): JSX.Element {
  const [album, setAlbum] = useState<AlbumItemHierarchy | null>(null);
  const [loading, setLoading] = useState(true);
  const [isFullscreen, setIsFullscreen] = useState(false);
  const [lastViewedImage, setLastViewedImage] = useState<number | null>(null);
  const router = useRouter();
  const searchParams = useSearchParams();
  const albumIdParam = searchParams.get('album') ? parseInt(searchParams.get('album') || '', 10) : null;
  const imageIdParam = searchParams.get('image') ? parseInt(searchParams.get('image') || '', 10) : null;
  
  // Local sort state - updated immediately without routing
  const [currentAlbumSort, setCurrentAlbumSort] = useState(searchParams.get('albumSort') || 'timestamp-desc');
  const [currentImageSort, setCurrentImageSort] = useState(searchParams.get('imageSort') || 'timestamp-desc');
  
  const viewMode = imageIdParam ? 'image' : 'gallery';
  const selectedImage = album?.images.find(item => item.id === imageIdParam) || null;

  const fetchAlbum = async (albumIdParam: number | null = null) => {
    setLoading(true);
    try {
      const apiBase = process.env.NEXT_PUBLIC_API_BASE || '';
      const albumIdPath = albumIdParam !== null ? `/${albumIdParam}` : '';
      const url = apiBase ? `${apiBase}/api/v1/valbums${albumIdPath}` : `/api/v1/valbums${albumIdPath}`;
      console.log('Fetching virtual album:', {albumIdParam, albumIdPath, url });
      const res = await fetch(url);
      if (!res.ok) {
        console.error('Failed to fetch virtual album', res.status);
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
      console.error('Error fetching virtual album', e);
      setAlbum(null);
    } finally {
      setLoading(false);
    }
  };

  
  useEffect(() => {
    fetchAlbum(albumIdParam);
  }, [albumIdParam]);

  
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

  const handleAlbumClick = (newAlbumId: number | null) => {
    // Build URL with properly encoded params
    const params = new URLSearchParams({
      albumSort: currentAlbumSort,
      imageSort: currentImageSort
    });
    if (newAlbumId) {params.set('album', newAlbumId.toString());}
    const targetUrl = `/valbum?${params.toString()}`;
    const currentUrl = `${window.location.pathname}${window.location.search}`;
    if (currentUrl === targetUrl) {
      // If URL is unchanged (e.g., navigating to root when already at root), force a refresh/fetch
      fetchAlbum(newAlbumId || null);
      router.refresh();
    } else {
      router.push(targetUrl);
    }
  };

  const handleImageClick = (image: ImageItemContent) => {
    const currentParams = new URLSearchParams(window.location.search);
    currentParams.set('image', image.id.toString());
    // Persist current sort preferences to URL on navigation
    currentParams.set('albumSort', currentAlbumSort);
    currentParams.set('imageSort', currentImageSort);
    router.push(`/valbum?${currentParams.toString()}`);
  };

  const handleCloseImage = () => {
    // Save the current image name before closing
    if (imageIdParam) {
      setLastViewedImage(imageIdParam);
    }
    const currentParams = new URLSearchParams(window.location.search);
    currentParams.delete('image');
    router.push(`/valbum?${currentParams.toString()}`);
  };

  return (
    <main>
      {loading ? (
        <p>Loading...</p>
      ) : album ? (
        <>
          {viewMode === 'gallery' && (
            <VirtualAlbumHierarchyView 
              key={albumIdParam} 
              album={album} 
              onAlbumClick={handleAlbumClick} 
              onImageClick={handleImageClick} 
              lastViewedImage={lastViewedImage}
              albumSort={currentAlbumSort}
              imageSort={currentImageSort}
              router={router}
              onSortChange={(albumSort, imageSort) => {
                // Update local state immediately without routing
                setCurrentAlbumSort(albumSort);
                setCurrentImageSort(imageSort);
              }}
              onSearchSubmit={() => {}}
              clearLastViewedImage={() => setLastViewedImage(null)}
            />
          )}
          {viewMode === 'image' && selectedImage && (
            <ImageView 
              image={selectedImage} 
              album={album} 
              onAlbumClick={() => {}} 
              onClose={handleCloseImage} 
              router={router}
              isFullscreen={isFullscreen}
              setIsFullscreen={setIsFullscreen}
              path='/valbum'
            />
          )}
        </>
      ) : (
        <p>No virtual album found.</p>
      )}
    </main>
  );
}