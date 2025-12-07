'use client';
import './components/gallery.css';

import { AlbumHierarchyService, AlbumHierarchy } from './components/Album';
import { useEffect, useState } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';

export default function Page() {
  const [albums, setAlbums] = useState<AlbumHierarchy[]>([]);
  const [loading, setLoading] = useState(true);
  const router = useRouter();
  const searchParams = useSearchParams();
  const albumName = searchParams.get('name') || '';

  const fetchAlbums = async (albumNameParam: string = '') => {
    const base = process.env.NEXT_PUBLIC_WEATHER_API_BASE ?? 'http://localhost:5001';
    setLoading(true);
    try {
      const encodedAlbumName = albumNameParam ? encodeURIComponent(albumNameParam) : '';
      const res = await fetch(`${base}/api/v1/albums/${encodedAlbumName}`);
      if (!res.ok) {
        console.error('Failed to fetch albums', res.status);
        setAlbums([]);
      } else {
        const data = await res.json();
        setAlbums(data);
      }
    } catch (e) {
      console.error('Error fetching albums', e);
      setAlbums([]);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchAlbums(albumName);
  }, [albumName]);

  const handleAlbumClick = (newAlbumName: string) => {
    router.push(`/album?name=${encodeURIComponent(newAlbumName)}`);
  };

  return (
    <main>
      {loading ? <p>Loading...</p> : <AlbumHierarchyService key={albumName} albums={albums} onAlbumClick={handleAlbumClick} />}
    </main>
  );
}
