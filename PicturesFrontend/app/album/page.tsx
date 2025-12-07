'use client';
import './components/gallery.css';

import { AlbumHierarchyService, AlbumHierarchy } from './components/Album';
import { useEffect, useState } from 'react';

export default function Page() {
  const [albums, setAlbums] = useState<AlbumHierarchy[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    async function fetchAlbums() {
      const base = process.env.NEXT_PUBLIC_WEATHER_API_BASE ?? 'http://localhost:5001';
      try {
        const res = await fetch(`${base}/api/v1/albums/%5C2023`);
        if (!res.ok) {
          console.error('Failed to fetch root albums', res.status);
          setAlbums([]);
        } else {
          const data = await res.json();
          setAlbums(data);
        }
      } catch (e) {
        console.error('Error fetching root albums', e);
        setAlbums([]);
      } finally {
        setLoading(false);
      }
    }
    fetchAlbums();
  }, []);

  return (
    <main>
      {loading ? <p>Loading...</p> : <AlbumHierarchyService albums={albums} />}
    </main>
  );
}
