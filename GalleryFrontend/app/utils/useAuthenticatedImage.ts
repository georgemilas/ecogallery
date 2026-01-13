'use client';

import { useState, useEffect } from 'react';
import { apiFetch } from './apiFetch';

/**
 * Hook to load an image with authentication headers
 * Returns an object URL that can be used in img src
 */
export function useAuthenticatedImage(url: string | null | undefined): string | null {
  const [objectUrl, setObjectUrl] = useState<string | null>(null);

  useEffect(() => {
    if (!url) {
      setObjectUrl(null);
      return;
    }

    let cancelled = false;
    let currentObjectUrl: string | null = null;

    apiFetch(url)
      .then(res => {
        if (!res.ok) throw new Error(`Failed to load image: ${res.status}`);
        return res.blob();
      })
      .then(blob => {
        if (cancelled) return;
        currentObjectUrl = URL.createObjectURL(blob);
        setObjectUrl(currentObjectUrl);
      })
      .catch(err => {
        console.error('Error loading authenticated image:', err);
        setObjectUrl(null);
      });

    // Cleanup: revoke object URL when component unmounts or URL changes
    return () => {
      cancelled = true;
      if (currentObjectUrl) {
        URL.revokeObjectURL(currentObjectUrl);
      }
    };
  }, [url]);

  return objectUrl;
}
