'use client';
import './components/gallery.css';

import { AlbumHierarchyComponent, AlbumItemHierarchy } from './components/Album';
import { ImageView } from './components/Image';
import { useEffect, useState } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';

export default function Page() {
  const [album, setAlbum] = useState<AlbumItemHierarchy | null>(null);
  const [loading, setLoading] = useState(true);
  const [isFullscreen, setIsFullscreen] = useState(false);
  const router = useRouter();
  const searchParams = useSearchParams();
  const albumNameParam = searchParams.get('name') || '';
  const imageNameParam = searchParams.get('image');
  
  const viewMode = imageNameParam ? 'image' : 'gallery';
  const selectedImage = album?.content.find(item => item.name === imageNameParam) || null;

  const fetchAlbum = async (albumNameParam: string = '') => {
    const base = process.env.NEXT_PUBLIC_WEATHER_API_BASE ?? 'http://localhost:5001';
    setLoading(true);
    try {
      const encodedAlbumName = albumNameParam ? encodeURIComponent(albumNameParam) : '';
      const res = await fetch(`${base}/api/v1/albums/${encodedAlbumName}`);
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

  useEffect(() => {
    fetchAlbum(albumNameParam);
  }, [albumNameParam]);

  
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
    router.push(`/album?name=${encodeURIComponent(newAlbumName)}`);
  };

  const handleImageClick = (image: AlbumItemHierarchy) => {
    const currentParams = new URLSearchParams(window.location.search);
    currentParams.set('image', image.name);
    router.push(`/album?${currentParams.toString()}`);
  };

  const handleCloseImage = () => {
    const currentParams = new URLSearchParams(window.location.search);
    currentParams.delete('image');
    router.push(`/album?${currentParams.toString()}`);
  };

  const handlePrevImage = (image: AlbumItemHierarchy, album: AlbumItemHierarchy) => {
    var content = album.content.filter(a => !a.is_album).toSorted((a, b) => new Date(b.item_timestamp_utc).getTime() - new Date(a.item_timestamp_utc).getTime());
    var ix = content.findIndex(item => item.name === image.name);
    var prev = ix > 0 ? content[ix - 1] : content[content.length - 1];  //repeat to last if at start
    const currentParams = new URLSearchParams(window.location.search);
    currentParams.set('image', prev.name);
    router.push(`/album?${currentParams.toString()}`);
  };

  const handleNextImage = (image: AlbumItemHierarchy, album: AlbumItemHierarchy) => {
    var content = album.content.filter(a => !a.is_album).toSorted((a, b) => new Date(b.item_timestamp_utc).getTime() - new Date(a.item_timestamp_utc).getTime());
    var ix = content.findIndex(item => item.name === image.name);
    var next = ix < content.length - 1 ? content[ix + 1] : content[0];  //repeat to first if at end
    const currentParams = new URLSearchParams(window.location.search);
    currentParams.set('image', next.name);
    router.push(`/album?${currentParams.toString()}`);
  };

  return (
    <main>
      {loading ? (
        <p>Loading...</p>
      ) : album ? (
        <>
          {viewMode === 'gallery' && (
            <AlbumHierarchyComponent key={albumNameParam} album={album} onAlbumClick={handleAlbumClick} onImageClick={handleImageClick} />
          )}
          {viewMode === 'image' && selectedImage && (
            <ImageView 
              image={selectedImage} 
              album={album} 
              onClose={handleCloseImage} 
              onPrev={handlePrevImage} 
              onNext={handleNextImage}
              isFullscreen={isFullscreen}
              setIsFullscreen={setIsFullscreen}
            />
          )}
        </>
      ) : (
        <p>No album found.</p>
      )}
    </main>
  );
}
