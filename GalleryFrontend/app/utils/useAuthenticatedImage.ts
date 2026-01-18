'use client';

import { useState, useEffect } from 'react';
import { apiFetch } from './apiFetch';

/**
 * Hook to load an image with authentication headers and request cancellation
 * Returns an object URL that can be used in img src
 */
export function useAuthenticatedImage(url: string | null | undefined): string | null {
  const [objectUrl, setObjectUrl] = useState<string | null>(null);

  useEffect(() => {
    if (!url) {
      setObjectUrl(null);
      return;
    }

    const abortController = new AbortController();
    let currentObjectUrl: string | null = null;

    apiFetch(url, {
      signal: abortController.signal,
      headers: {
        'Accept': 'image/*'
      }
    })
      .then(res => {
        if (!res.ok) throw new Error(`Failed to load image: ${res.status}`);
        return res.blob();
      })
      .then(blob => {
        if (abortController.signal.aborted) return;
        currentObjectUrl = URL.createObjectURL(blob);
        setObjectUrl(currentObjectUrl);
      })
      .catch(err => {
        if (err.name === 'AbortError') {
          // Request was cancelled, this is expected
          return;
        }
        console.error('Error loading authenticated image:', err);
        setObjectUrl(null);
      });

    // Cleanup: cancel request and revoke object URL when component unmounts or URL changes
    return () => {
      abortController.abort();
      if (currentObjectUrl) {
        URL.revokeObjectURL(currentObjectUrl);
      }
    };
  }, [url]);

  return objectUrl;
}
