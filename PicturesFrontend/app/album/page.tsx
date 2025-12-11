'use client';
import './components/gallery.css';

import { AlbumHierarchyComponent } from './components/Album';
import { AlbumItemHierarchy, ImageItemContent } from './components/AlbumHierarchyProps';
import { ImageView } from './components/Image';
import { useEffect, useState, Suspense } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';

function AlbumPage() {
  const [album, setAlbum] = useState<AlbumItemHierarchy | null>(null);
  const [loading, setLoading] = useState(true);
  const [isFullscreen, setIsFullscreen] = useState(false);
  const [lastViewedImage, setLastViewedImage] = useState<string | null>(null);
  const router = useRouter();
  const searchParams = useSearchParams();
  const albumNameParam = searchParams.get('name') || '';
  const imageNameParam = searchParams.get('image');
  const albumSort = searchParams.get('albumSort') || 'timestamp-desc';
  const imageSort = searchParams.get('imageSort') || 'timestamp-desc';
  
  const viewMode = imageNameParam ? 'image' : 'gallery';
  const selectedImage = album?.images.find(item => item.name === imageNameParam) || null;

  const fetchAlbum = async (albumNameParam: string = '') => {
    const base = process.env.NEXT_PUBLIC_PICTURES_API_BASE ?? 'http://localhost:5001';
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
    const currentParams = new URLSearchParams(window.location.search);
    currentParams.set('name', newAlbumName);
    currentParams.delete('image'); // Clear image param when navigating to different album
    // Keep albumSort and imageSort params
    router.push(`/album?${currentParams.toString()}`);
  };

  const handleImageClick = (image: ImageItemContent) => {
    const currentParams = new URLSearchParams(window.location.search);
    currentParams.set('image', image.name);
    router.push(`/album?${currentParams.toString()}`);
  };

  const handleCloseImage = () => {
    // Save the current image name before closing
    if (imageNameParam) {
      setLastViewedImage(imageNameParam);
    }
    const currentParams = new URLSearchParams(window.location.search);
    currentParams.delete('image');
    router.push(`/album?${currentParams.toString()}`);
  };

  const handlePrevImage = (image: ImageItemContent, album: AlbumItemHierarchy) => {
    //var content = album.images.toSorted((a, b) => new Date(b.item_timestamp_utc).getTime() - new Date(a.item_timestamp_utc).getTime());
    var content = album.images;  //use the order as provided by the sorter in album view 
    var ix = content.findIndex(item => item.name === image.name);
    var prev = ix > 0 ? content[ix - 1] : content[content.length - 1];  //repeat to last if at start
    const currentParams = new URLSearchParams(window.location.search);
    currentParams.set('image', prev.name);
    router.push(`/album?${currentParams.toString()}`);
  };

  const handleNextImage = (image: ImageItemContent, album: AlbumItemHierarchy) => {
    //var content = album.images.toSorted((a, b) => new Date(b.item_timestamp_utc).getTime() - new Date(a.item_timestamp_utc).getTime());
    var content = album.images;  //use the order as provided by the sorter in album view
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
            <AlbumHierarchyComponent 
              key={albumNameParam} 
              album={album} 
              onAlbumClick={handleAlbumClick} 
              onImageClick={handleImageClick} 
              lastViewedImage={lastViewedImage}
              albumSort={albumSort}
              imageSort={imageSort}
              onSortChange={(albumSort, imageSort) => {
                const params = new URLSearchParams(window.location.search);
                params.set('albumSort', albumSort);
                params.set('imageSort', imageSort);
                router.push(`/album?${params.toString()}`);
              }}
            />
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

export default function Page() {
  return (
    <Suspense fallback={<div>Loading...</div>}>
      <AlbumPage />
    </Suspense>
  );
}
