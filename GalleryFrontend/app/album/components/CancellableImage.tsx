import React from 'react';
import { useMediaLoader } from '../hooks/useImageLoader';

interface CancellableImageProps {
  src: string | null;
  alt: string;
  loading?: 'lazy' | 'eager';
  className?: string;
  style?: React.CSSProperties;
  onLoad?: () => void;
  onError?: () => void;
  /** Whether to enable request cancellation (default: true) */
  enableCancellation?: boolean;
  /** Show loading placeholder while image loads */
  showLoadingPlaceholder?: boolean;
  /** Image width for proper placeholder sizing */
  width?: number | null;
  /** Image height for proper placeholder sizing */
  height?: number | null;
}

/**
 * Image and video component with request cancellation support.
 * Converts image URLs to blob URLs using fetch() with AbortController.
 * Videos use direct URLs after authentication to avoid memory issues.
 * Automatically cancels requests when component unmounts.
 */
export function CancellableImage({
  src,
  alt,
  loading = 'lazy',
  className,
  style,
  onLoad,
  onError,
  enableCancellation = true,
  showLoadingPlaceholder = false,
  width,
  height,
  ...props
}: CancellableImageProps) {
  const imageState = useMediaLoader(src, { enableCancellation });

  const handleLoad = () => {
    onLoad?.();
  };

  const handleError = () => {
    onError?.();
  };

  // Calculate placeholder dimensions to maintain aspect ratio
  const getPlaceholderStyle = (): React.CSSProperties => {
    const baseStyle = style || {};
    
    if (width && height) {
      const aspectRatio = width / height;
      return {
        ...baseStyle,
        aspectRatio: aspectRatio.toString(),
        width: width > 400 ? '400px' : `${width}px`, // Cap max width for thumbnails
        height: 'auto'
      };
    }
    
    return {
      ...baseStyle,
      minHeight: '100px' // Fallback for unknown dimensions
    };
  };

  // Show loading placeholder if enabled and image is loading
  if (showLoadingPlaceholder && imageState.loading) {
    return (
      <div 
        className={`image-loading-placeholder ${className || ''}`}
        style={getPlaceholderStyle()}
        aria-label={`Loading ${alt}`}
      >
        <div className="loading-spinner" />
      </div>
    );
  }

  // Show error state or fallback to original src
  const imageSrc = imageState.error ? src : (imageState.src || src);

  return (
    <img
      {...props}
      src={imageSrc || undefined}
      alt={alt}
      loading={loading}
      className={className}
      style={style}
      onLoad={handleLoad}
      onError={handleError}
    />
  );
}

interface LazyLoadedImageProps extends CancellableImageProps {
  /** Whether the image is currently visible (for intersection observer) */
  isVisible?: boolean;
  /** Delay before starting image load when visible (ms) */
  loadDelay?: number;
}

/**
 * Lazy-loaded image with intersection observer and cancellation support.
 * Only starts loading when the image enters the viewport.
 */
export function LazyLoadedImage({
  isVisible = true,
  loadDelay = 0,
  ...props
}: LazyLoadedImageProps) {
  const [shouldLoad, setShouldLoad] = React.useState(!props.src || isVisible);
  const timeoutRef = React.useRef<NodeJS.Timeout | null>(null);

  React.useEffect(() => {
    if (isVisible && !shouldLoad && props.src) {
      if (loadDelay > 0) {
        timeoutRef.current = setTimeout(() => {
          setShouldLoad(true);
        }, loadDelay);
      } else {
        setShouldLoad(true);
      }
    }

    return () => {
      if (timeoutRef.current) {
        clearTimeout(timeoutRef.current);
        timeoutRef.current = null;
      }
    };
  }, [isVisible, shouldLoad, loadDelay, props.src]);

  const imageSrc = shouldLoad ? props.src : null;

  return <CancellableImage {...props} src={imageSrc} />;
}