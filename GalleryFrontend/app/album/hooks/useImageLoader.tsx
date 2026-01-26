import { useEffect, useRef, useCallback, useState } from 'react';
import { apiFetch } from '@/app/utils/apiFetch';

/**
 * Determines if we're in a private album context that requires session authentication
 */
function isPrivateAlbumContext(): boolean {
  if (typeof window === 'undefined') return true; // Assume private on SSR
  
  const pathname = window.location.pathname;
  return pathname.startsWith('/album'); // Private albums vs /valbum public virtual albums
}

/**
 * Determines the media loading strategy based on nginx URL patterns and album context
 */
function getMediaLoadingStrategy(url: string): 'progressive' | 'authenticated' | 'secure' {
  if (url.includes('/_thumbnails/400/')) {
    return 'progressive'; // No auth needed, direct progressive loading
  } else if (url.includes('/_thumbnails/')) {
    return 'authenticated'; // Requires auth, use blob approach
  } else {
    return 'secure'; // Full images/videos, use current secure approach
  }
}

interface UseMediaLoaderOptions {
  /** Whether to enable request cancellation (default: true) */
  enableCancellation?: boolean;
  /** Delay before starting image load to debounce requests (default: 0) */
  loadDelay?: number;
}

interface ImageLoadState {
  src: string | null;
  loading: boolean;
  error: boolean;
}

/**
 * Custom hook for loading images and videos with request cancellation support and proper authentication.
 * 
 * Authentication Strategy:
 * - Progressive (/_thumbnails/400/): No authentication required for performance
 * - Authenticated (/_thumbnails/): Requires X-API-Key authentication
 * - Secure (/pictures/): Requires X-API-Key + session authentication for private albums
 * 
 * Converts image URLs to blob URLs using fetch() with AbortController.
 * Videos use direct URLs after authentication to avoid memory issues.
 * All pending requests are automatically cancelled when the component unmounts.
 */
export function useMediaLoader(
  imageUrl: string | null,
  options: UseMediaLoaderOptions = {}
): ImageLoadState {
  const { enableCancellation = true, loadDelay = 0 } = options;
  const [state, setState] = useState<ImageLoadState>({
    src: null,
    loading: false,
    error: false
  });
  
  const abortControllerRef = useRef<AbortController | null>(null);
  const timeoutRef = useRef<NodeJS.Timeout | null>(null);
  const currentUrlRef = useRef<string | null>(null);

  const cleanup = useCallback(() => {
    if (abortControllerRef.current) {
      abortControllerRef.current.abort();
      abortControllerRef.current = null;
    }
    if (timeoutRef.current) {
      clearTimeout(timeoutRef.current);
      timeoutRef.current = null;
    }
    if (state.src && state.src.startsWith('blob:')) {
      URL.revokeObjectURL(state.src);
    }
  }, [state.src]);

  const loadMedia = useCallback(async (url: string) => {
    //console.log('loadMedia called for URL:', url);
    
    // Don't reload if it's the same URL and we already have a blob URL
    if (url === currentUrlRef.current && state.src && !state.error) {
      //console.log('Skipping reload for same URL:', url);
      return;
    }

    cleanup();
    currentUrlRef.current = url;
    
    setState(prev => ({ ...prev, loading: true, error: false }));

    const strategy = getMediaLoadingStrategy(url);
    //console.log('Selected strategy:', strategy, 'for URL:', url);

    try {
      
      switch (strategy) {
        case 'progressive':
          // 400px thumbnails - direct progressive loading (no auth needed)
          //console.log('Using progressive loading for:', url);
          setState({ src: url, loading: false, error: false });
          return;
          
        case 'authenticated':
        case 'secure':
          // Use authenticated blob loading for full images and secured thumbnails
          // Always require X-API-Key, and session token for private albums
          console.log('Using authenticated loading for:', url, 'Private context:', isPrivateAlbumContext());
          
          if (enableCancellation) {
            abortControllerRef.current = new AbortController();
          }
          
          // Prepare headers for authentication
          const authHeaders: Record<string, string> = {
            'Accept': url.includes('/video/') ? 'video/*' : 'image/*'
          };
          
          // For private albums, ensure we have session token
          if (isPrivateAlbumContext()) {
            const token = localStorage.getItem('sessionToken');
            if (!token) {
              throw new Error('Session authentication required for private album access');
            }
            // apiFetch will automatically add the session token
          }
          
          const response = await apiFetch(url, {
            signal: enableCancellation ? abortControllerRef.current?.signal : undefined,
            headers: authHeaders
          });
          
          // Accept both 200 (OK) and 206 (Partial Content) as success
          if (response.status < 200 || response.status >= 300) {
            throw new Error(`Failed to load media: ${response.status}`);
          }
          
          const blob = await response.blob();
          if (enableCancellation && abortControllerRef.current?.signal.aborted) {
            return;
          }
          
          const blobUrl = URL.createObjectURL(blob);
          setState({ src: blobUrl, loading: false, error: false });
          return;
          
        default:
          throw new Error(`Unknown loading strategy: ${strategy}`);
      }
      
    } catch (error) {
      if (error instanceof Error && error.name === 'AbortError') {
        // Request was cancelled, don't update state
        return;
      }
      
      console.warn('Failed to load media with authentication:', error, 'URL:', url, 'Strategy:', strategy, 'Private:', isPrivateAlbumContext());
      setState({ src: null, loading: false, error: true });
    }
  }, [enableCancellation, state.src, state.error, cleanup]);

  useEffect(() => {
    if (!imageUrl) {
      cleanup();
      setState({ src: null, loading: false, error: false });
      currentUrlRef.current = null;
      return;
    }

    if (loadDelay > 0) {
      timeoutRef.current = setTimeout(() => {
        loadMedia(imageUrl);
      }, loadDelay);
    } else {
      loadMedia(imageUrl);
    }

    return () => {
      if (timeoutRef.current) {
        clearTimeout(timeoutRef.current);
        timeoutRef.current = null;
      }
    };
  }, [imageUrl, loadMedia, loadDelay]);

  // Cleanup on unmount
  useEffect(() => {
    return cleanup;
  }, [cleanup]);

  return state;
}

/**
 * Hook for managing multiple thumbnail downloads with global cancellation.
 * Useful for gallery components that load many thumbnails at once.
 * Thumbnails are always images, even for video content.
 */
export function useGalleryThumbnailLoader() {
  const abortControllersRef = useRef<Map<string, AbortController>>(new Map());
  const blobUrlsRef = useRef<Set<string>>(new Set());

  const loadImage = useCallback(async (imageUrl: string): Promise<string> => {
    // Cancel any existing request for this URL
    const existingController = abortControllersRef.current.get(imageUrl);
    if (existingController) {
      existingController.abort();
    }

    const controller = new AbortController();
    abortControllersRef.current.set(imageUrl, controller);

    try {
      const response = await apiFetch(imageUrl, {
        signal: controller.signal,
        headers: { 'Accept': 'image/*' }
      });

      if (!response.ok) {
        throw new Error(`Failed to load image: ${response.status}`);
      }

      const blob = await response.blob();
      
      if (controller.signal.aborted) {
        return imageUrl; // Return original URL as fallback
      }

      const blobUrl = URL.createObjectURL(blob);
      blobUrlsRef.current.add(blobUrl);
      
      // Clean up the controller for this URL
      abortControllersRef.current.delete(imageUrl);
      
      return blobUrl;
    } catch (error) {
      abortControllersRef.current.delete(imageUrl);
      
      if (error instanceof Error && error.name === 'AbortError') {
        return imageUrl; // Return original URL as fallback
      }
      
      console.warn('Failed to load image:', error);
      return imageUrl; // Return original URL as fallback
    }
  }, []);

  const cancelAllRequests = useCallback(() => {
    // Cancel all pending requests
    for (const controller of abortControllersRef.current.values()) {
      controller.abort();
    }
    abortControllersRef.current.clear();

    // Revoke all blob URLs
    for (const blobUrl of blobUrlsRef.current) {
      URL.revokeObjectURL(blobUrl);
    }
    blobUrlsRef.current.clear();
  }, []);

  // Cleanup on unmount
  useEffect(() => {
    return cancelAllRequests;
  }, [cancelAllRequests]);

  return { loadImage, cancelAllRequests };
}