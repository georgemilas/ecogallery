'use client';

import React from 'react';
import { useAuthenticatedImage } from './useAuthenticatedImage';

interface AuthenticatedImageProps extends React.ImgHTMLAttributes<HTMLImageElement> {
  src: string;
  alt: string;
  /** If true, uses blob conversion (needed for cancellation in galleries). Default false. */
  useBlob?: boolean;
}

/**
 * Image component with X-API-Key + session authentication.
 * Automatically uses blob conversion for files <10MB (full security)
 * and API proxy for files >10MB (session cookie security only).
 * 
 * Usage: 
 *   <AuthenticatedImage src="/pictures/..." alt="..." />
 */
export const AuthenticatedImage = React.forwardRef<HTMLImageElement, AuthenticatedImageProps>(
  ({ src, alt, useBlob = true, ...props }, ref) => {
    const imageSrc = useAuthenticatedImage(src);

    if (!imageSrc) {
      return (
        <img 
          {...props} 
          ref={ref}
          alt={alt}
          style={{ ...props.style, opacity: 0.3 }}
        />
      );
    }

    return (
      <img 
        {...props} 
        ref={ref}
        src={imageSrc}
        alt={alt}
        crossOrigin="use-credentials"
      />
    );
  }
);

AuthenticatedImage.displayName = 'AuthenticatedImage';
