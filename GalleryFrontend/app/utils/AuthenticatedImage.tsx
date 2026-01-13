'use client';

import React from 'react';
import { useAuthenticatedImage } from './useAuthenticatedImage';

interface AuthenticatedImageProps extends React.ImgHTMLAttributes<HTMLImageElement> {
  src: string;
  alt: string;
}

/**
 * Image component that automatically loads images with authentication headers
 * Usage: <AuthenticatedImage src="/pictures/..." alt="..." />
 */
export const AuthenticatedImage = React.forwardRef<HTMLImageElement, AuthenticatedImageProps>(
  ({ src, alt, ...props }, ref) => {
    const authenticatedSrc = useAuthenticatedImage(src);

    if (!authenticatedSrc) {
      // Show placeholder or loading state
      return (
        <img 
          {...props} 
          ref={ref}
          alt={alt}
          style={{ ...props.style, opacity: 0.3 }}
        />
      );
    }

    return <img {...props} ref={ref} src={authenticatedSrc} alt={alt} />;
  }
);

AuthenticatedImage.displayName = 'AuthenticatedImage';
