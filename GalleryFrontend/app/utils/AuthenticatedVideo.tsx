'use client';

import React from 'react';
import { useAuthenticatedImage } from './useAuthenticatedImage';

interface AuthenticatedVideoProps extends React.VideoHTMLAttributes<HTMLVideoElement> {
  src: string;
  poster?: string;
}

export const AuthenticatedVideo = React.forwardRef<HTMLVideoElement, AuthenticatedVideoProps>(
  ({ src, poster, ...props }, ref) => {
    const videoUrl = useAuthenticatedImage(src);
    const posterUrl = useAuthenticatedImage(poster || '');

    if (!videoUrl) {
      return <div style={{ opacity: 0.3 }}>Loading video...</div>;
    }

    return (
      <video
        ref={ref}
        src={videoUrl}
        poster={posterUrl || undefined}
        {...props}
      />
    );
  }
);

AuthenticatedVideo.displayName = 'AuthenticatedVideo';
