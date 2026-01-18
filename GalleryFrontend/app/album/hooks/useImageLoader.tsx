import { useEffect, useRef, useCallback, useState } from 'react';
import { apiFetch } from '@/app/utils/apiFetch';

interface UseImageLoaderOptions {
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
 * Custom hook for loading images with request cancellation support.
 * Converts image URLs to blob URLs using fetch() with AbortController.
 * All pending requests are automatically cancelled when the component unmounts.
 */
export function useImageLoader(
  imageUrl: string | null,
  options: UseImageLoaderOptions = {}
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

  const loadImage = useCallback(async (url: string) => {
    // Don't reload if it's the same URL and we already have a blob URL
    if (url === currentUrlRef.current && state.src && !state.error) {
      return;
    }

    cleanup();
    currentUrlRef.current = url;
    
    setState(prev => ({ ...prev, loading: true, error: false }));

    try {
      if (!enableCancellation) {
        // Fallback to standard image loading without cancellation
        setState({ src: url, loading: false, error: false });
        return;
      }

      abortControllerRef.current = new AbortController();
      
      const response = await apiFetch(url, {
        signal: abortControllerRef.current.signal,
        headers: {
          'Accept': 'image/*'
        }
      });

      if (!response.ok) {
        throw new Error(`Failed to load image: ${response.status}`);
      }

      const blob = await response.blob();
      
      if (abortControllerRef.current?.signal.aborted) {
        return; // Request was cancelled
      }

      const blobUrl = URL.createObjectURL(blob);
      setState({ src: blobUrl, loading: false, error: false });
      
    } catch (error) {
      if (error instanceof Error && error.name === 'AbortError') {
        // Request was cancelled, don't update state
        return;
      }
      
      console.warn('Failed to load image with cancellation, falling back to standard loading:', error);
      // Fallback to standard image loading on error
      setState({ src: url, loading: false, error: false });
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
        loadImage(imageUrl);
      }, loadDelay);
    } else {
      loadImage(imageUrl);
    }

    return () => {
      if (timeoutRef.current) {
        clearTimeout(timeoutRef.current);
        timeoutRef.current = null;
      }
    };
  }, [imageUrl, loadImage, loadDelay]);

  // Cleanup on unmount
  useEffect(() => {
    return cleanup;
  }, [cleanup]);

  return state;
}

/**
 * Hook for managing multiple image downloads with global cancellation.
 * Useful for gallery components that load many images at once.
 */
export function useGalleryImageLoader() {
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