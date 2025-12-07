'use client';
import './components/gallery.css';

import { AlbumHierarchyService, AlbumHierarchy } from './components/Album';
import { useEffect, useState } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';

export default function Page() {
  const [album, setAlbum] = useState<AlbumHierarchy | null>(null);
  const [loading, setLoading] = useState(true);
  const router = useRouter();
  const searchParams = useSearchParams();
  const albumName = searchParams.get('name') || '';

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
        const data = await res.json();
        // Convert plain objects to AlbumHierarchy class instances
        const convertToAlbum = (obj: any): AlbumHierarchy => {
          const album = Object.assign(new AlbumHierarchy(), obj);
          if (album.content) { // Recursively convert nested content
            album.content = album.content.map(convertToAlbum);
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
    fetchAlbum(albumName);
  }, [albumName]);

  const handleAlbumClick = (newAlbumName: string) => {
    router.push(`/album?name=${encodeURIComponent(newAlbumName)}`);
  };

  return (
    <main>
      {loading ? <p>Loading...</p> : album ? <AlbumHierarchyService key={albumName} album={album} onAlbumClick={handleAlbumClick} /> : <p>No album found.</p>}
    </main>
  );
}
