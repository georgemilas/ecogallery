'use client';

import React from 'react';
import { useAuthenticatedImage } from './useAuthenticatedImage';

interface AuthenticatedVideoProps extends React.VideoHTMLAttributes<HTMLVideoElement> {
  src: string;
  poster?: string;
}

/**
 * Video component with X-API-Key + session authentication.
 * Routes through Next.js API proxy which adds X-API-Key server-side and supports range requests.
 * Poster uses blob conversion.
 * 
 * Usage: <AuthenticatedVideo src="/pictures/video.mp4" poster="/pictures/thumb.jpg" />
 */
export const AuthenticatedVideo = React.forwardRef<HTMLVideoElement, AuthenticatedVideoProps>(
  ({ src, poster, ...props }, ref) => {
    const posterUrl = useAuthenticatedImage(poster || '');
    
    // Extract pathname from absolute URLs
    const getPath = (url: string) => {
      if (!url) return url;
      try {
        const urlObj = new URL(url);
        return urlObj.pathname;
      } catch {
        return url;
      }
    };

    // Route video through API proxy which adds X-API-Key and supports range requests
    const videoSrc = src ? `/api/media${getPath(src)}` : undefined;

    return (
      <video
        ref={ref}
        src={videoSrc}
        poster={posterUrl || undefined}
        crossOrigin="use-credentials"
        {...props}
      />
    );
  }
);

AuthenticatedVideo.displayName = 'AuthenticatedVideo';
