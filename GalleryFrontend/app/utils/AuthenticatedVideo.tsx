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

    // Serve video directly through nginx (bypasses Next.js proxy for streaming performance)
    // Auth: api key via query param + session cookie (sent automatically as same-origin request)
    const apiKey = process.env.NEXT_PUBLIC_API_KEY || '';
    const videoSrc = src ? `${getPath(src)}?q=${apiKey}` : undefined;

    return (
      <video
        ref={ref}
        src={videoSrc}
        poster={posterUrl || undefined}
        //crossOrigin="use-credentials"  // Not needed since we're using same-origin URLs and cookies are sent automatically
        {...props}
      />
    );
  }
);

AuthenticatedVideo.displayName = 'AuthenticatedVideo';
