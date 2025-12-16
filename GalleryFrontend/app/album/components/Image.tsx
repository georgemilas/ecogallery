import React from 'react';
import { AlbumItemHierarchy, ImageItemContent } from './AlbumHierarchyProps';
import { ExifPanel } from './Exif';
import { ImageViewZoom } from './ImageViewZoom';
import './imageContent.css';

interface ImageViewProps {
  image: ImageItemContent;
  album: AlbumItemHierarchy;
  onAlbumClick: (albumPath: string) => void;
  onClose: () => void;
  onPrev: (image: ImageItemContent, album: AlbumItemHierarchy) => void;
  onNext: (image: ImageItemContent, album: AlbumItemHierarchy) => void;
  isFullscreen: boolean;
  setIsFullscreen: (value: boolean) => void;
}

export function ImageView({ image, album, onAlbumClick,onClose, onPrev, onNext, isFullscreen, setIsFullscreen }: ImageViewProps) {
  const videoRef = React.useRef<HTMLVideoElement>(null);
  const imageRef = React.useRef<HTMLImageElement>(null);
  const containerRef = React.useRef<HTMLDivElement>(null);
  const [isSlideshow, setIsSlideshow] = React.useState(false);
  const [slideshowSpeed, setSlideshowSpeed] = React.useState(3000);
  const slideshowIntervalRef = React.useRef<NodeJS.Timeout | null>(null);
  const [showExif, setShowExif] = React.useState(false);
  
  // Touch state (for swipe and pinch)
  const touchStartX = React.useRef<number>(0);
  const touchEndX = React.useRef<number>(0);
  const initialPinchDistance = React.useRef<number>(0);
  const lastZoom = React.useRef<number>(1);

  // Use zoom hook for all zoom-related functionality
  const { state: zoomState, handlers: zoomHandlers, setZoom, setIs1to1 } = ImageViewZoom(
    imageRef,
    containerRef,
    image.is_movie,
    image.name
  );
  const { zoom, position, isDragging, is1to1 } = zoomState;

  // Sync state with actual fullscreen status on mount
  React.useEffect(() => {    
    setIsFullscreen(!!document.fullscreenElement);
  }, [setIsFullscreen]);

  // Touch/swipe navigation + pinch zoom
  React.useEffect(() => {
    const handleTouchStart = (e: TouchEvent) => {
      if (e.touches.length === 2 && !image.is_movie) {
        // Pinch zoom
        const dx = e.touches[0].clientX - e.touches[1].clientX;
        const dy = e.touches[0].clientY - e.touches[1].clientY;
        initialPinchDistance.current = Math.sqrt(dx * dx + dy * dy);
        lastZoom.current = zoom;
      } else if (e.touches.length === 1) {
        touchStartX.current = e.touches[0].clientX;
      }
    };

    const handleTouchMove = (e: TouchEvent) => {
      if (e.touches.length === 2 && !image.is_movie) {
        // Pinch zoom
        const dx = e.touches[0].clientX - e.touches[1].clientX;
        const dy = e.touches[0].clientY - e.touches[1].clientY;
        const distance = Math.sqrt(dx * dx + dy * dy);
        const scale = distance / initialPinchDistance.current;
        const newZoom = Math.min(Math.max(0.5, lastZoom.current * scale), 10);
        setZoom(newZoom);
        setIs1to1(false);
      } else if (e.touches.length === 1 && zoom <= 1) {
        touchEndX.current = e.touches[0].clientX;
      }
    };

    const handleTouchEnd = (e: TouchEvent) => {
      if (e.touches.length === 0 && zoom <= 1) {
        const swipeThreshold = 50;
        const diff = touchStartX.current - touchEndX.current;

        if (Math.abs(diff) > swipeThreshold) {
          if (diff > 0) {
            onNext(image, album);
          } else {
            onPrev(image, album);
          }
        }

        touchStartX.current = 0;
        touchEndX.current = 0;
      }
    };

    window.addEventListener('touchstart', handleTouchStart);
    window.addEventListener('touchmove', handleTouchMove);
    window.addEventListener('touchend', handleTouchEnd);

    return () => {
      window.removeEventListener('touchstart', handleTouchStart);
      window.removeEventListener('touchmove', handleTouchMove);
      window.removeEventListener('touchend', handleTouchEnd);
    };
  }, [image, album, onPrev, onNext, zoom, setZoom, setIs1to1]);


  // Slideshow effect
  React.useEffect(() => {
    if (isSlideshow) {
      slideshowIntervalRef.current = setInterval(() => {
        onNext(image, album);
      }, slideshowSpeed);
    } else {
      if (slideshowIntervalRef.current) {
        clearInterval(slideshowIntervalRef.current);
        slideshowIntervalRef.current = null;
      }
    }

    return () => {
      if (slideshowIntervalRef.current) {
        clearInterval(slideshowIntervalRef.current);
      }
    };
  }, [isSlideshow, slideshowSpeed, image, album, onNext]);

  
  // Keyboard navigation
  React.useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      switch(e.key) {
        case 'ArrowLeft':
          onPrev(image, album);
          break;
        case 'ArrowRight':
          onNext(image, album);
          break;
        case 'Escape':
          if (!document.fullscreenElement) {
            onClose();
          }
          break;
        case 'f':
        case 'F':
          toggleFullscreen();
          break;
        case 'e':
        case 'E':
        case 'i':
        case 'I':
          toggleExif();
          break;
        case '0':
          zoomHandlers.reset();
          break;
        case '1':
          zoomHandlers.toggle1to1();
          break;
        case '=':
        case '+':
          zoomHandlers.zoomIn();
          break;
        case '-':
        case '_':
          zoomHandlers.zoomOut();
          break;
        case ' ':
        case 'Enter':
          e.preventDefault(); // Prevent default for spacebar and enter
          if (image.is_movie && videoRef.current) {
            if (videoRef.current.paused) {
              videoRef.current.play();
            } else {
              videoRef.current.pause();
            }
          } else {
            setIsSlideshow(prev => !prev);
          }             
          break;
          break;
      }
    };

    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [image, album, onPrev, onNext, onClose, zoomHandlers]);


  // Fullscreen toggle
  const toggleFullscreen = () => {
    if (!document.fullscreenElement) {
      console.log('Requesting fullscreen');
      document.documentElement.requestFullscreen()
        .catch(err => console.error('Fullscreen request failed:', err));
    } else {
      document.exitFullscreen()
        .catch(err => console.error('Exit fullscreen failed:', err));
    }
  };


  // Slideshow toggle  
  const toggleSlideshow = () => {
    setIsSlideshow(!isSlideshow);
  };

  const increaseSpeed = () => {
    setSlideshowSpeed(prev => Math.max(500, prev - 500)); // Decrease interval (faster), minimum 0.5s
  };

  const decreaseSpeed = () => {
    setSlideshowSpeed(prev => Math.min(10000, prev + 500)); // Increase interval (slower), maximum 10s
  };

  const toggleExif = () => {
    setShowExif(prev => !prev);
  };

  return (
   
    <div className="viewer">
        {isSlideshow && (
          <div className="slideshow-controls">
            <button onClick={decreaseSpeed} className="speed-button" title="Slower">
              <svg viewBox="0 0 24 24" fill="none">
                <path d="M5 12h14" stroke="white" stroke-width="2" stroke-linecap="round"/>
              </svg>
            </button>
            <span className="speed-display">{(slideshowSpeed / 1000).toFixed(1)}s</span>
            <button onClick={increaseSpeed} className="speed-button" title="Faster">
              <svg viewBox="0 0 24 24" fill="none">
                <path d="M12 5v14M5 12h14" stroke="white" stroke-width="2" stroke-linecap="round"/>
              </svg>
            </button>
          </div>
        )}

        {showExif && image.image_exif && (
          <ExifPanel exif={image.image_exif} album={album} image={image} onClose={toggleExif} />
        )}

        <div className="nav nav-prev">
            <div className="toolbar">
                <button onClick={toggleFullscreen} className="fullscreen-button" title="Fullscreen (F)">
                    <svg viewBox="0 0 24 24" fill="none">
                        <path d={isFullscreen ? "M5 13H11V19M4 20L11 13M19 11H13V5M20 4L13 11" : "M10 20H4V14M4 20L11 13M14 4H20V10M20 4L13 11"} stroke="white" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                    </svg>
                </button>
                <button onClick={toggleSlideshow} className="slideshow-button"  title="Slideshow (Space/Enter)">
                    <svg viewBox="0 0 24 24" fill="none">
                        {isSlideshow ? (
                            <path d="M6 4h4v16H6V4zm8 0h4v16h-4V4z" fill="white" stroke="white" stroke-width="2"/>
                        ) : (
                            <path d="M8 4 L18 12 L8 20 Z" fill="white" stroke="white" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                        )}
                    </svg>
                </button>
                <button onClick={toggleExif} className="exif-button" title="EXIF Info (E/I)">
                    <svg viewBox="0 0 24 24" fill="none">
                        <path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm0 15c-.55 0-1-.45-1-1v-4c0-.55.45-1 1-1s1 .45 1 1v4c0 .55-.45 1-1 1zm0-8c-.55 0-1-.45-1-1s.45-1 1-1 1 .45 1 1-.45 1-1 1z" fill="black" stroke="white"/>
                    </svg>
                </button>
                {!image.is_movie && (
                  <button onClick={zoomHandlers.toggle1to1} className={`zoom-button ${is1to1 ? 'active' : ''}`} title={is1to1 ? "Fit to Screen (0/1)" : "1:1 Zoom (+/-/0/1)"}>
                    <svg viewBox="0 0 24 24" fill="none">
                      {is1to1 ? (
                        // Fit to screen icon - diagonal arrows pointing outward
                        <>
                          <path d="M2 8C2 4.7 4.7 2 8 2H16C19.3 2 22 4.7 22 8V16C22 19.3 19.3 22 16 22H8C4.7 22 2 19.3 2 16V8Z" stroke="white" strokeWidth="1.5" fill="none"/>                          
                          <path d="M10 10L7 7M7 7L10 7M7 7L7 10" stroke="white" strokeWidth="1.2" strokeLinecap="round" strokeLinejoin="round"/>
                          <path d="M14 10L17 7M17 7L14 7M17 7L17 10" stroke="white" strokeWidth="1.2" strokeLinecap="round" strokeLinejoin="round"/>
                          <path d="M10 14L7 17M7 17L10 17M7 17L7 14" stroke="white" strokeWidth="1.2" strokeLinecap="round" strokeLinejoin="round"/>
                          <path d="M14 14L17 17M17 17L14 17M17 17L17 14" stroke="white" strokeWidth="1.2" strokeLinecap="round" strokeLinejoin="round"/>
                        </>
                      ) : (
                        // 1:1 zoom icon
                        <>
                          <path d="M2 8C2 4.7 4.7 2 8 2H16C19.3 2 22 4.7 22 8V16C22 19.3 19.3 22 16 22H8C4.7 22 2 19.3 2 16V8Z" stroke="white" strokeWidth="1.5" fill="none"/>
                          <text x="12" y="15.5" fontSize="9" fill="white" textAnchor="middle" fontWeight="bold">1:1</text>
                        </>
                      )}
                    </svg>
                  </button>
                )}
            </div>

            <div className="nav-btn">
                <button onClick={() => onPrev(image, album)} className="prev-button">
                    <svg viewBox="0 0 24 24" fill="none">
                        <polyline points="15,4 7,12 15,20" stroke="white" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                    </svg>
                </button>
            </div>

            <div className="spacer-prev"></div>
        </div>

          <div className="content" ref={containerRef}>
              <nav className="breadcrumbs">
              <a href="#"onClick={(e) => {e.preventDefault(); onAlbumClick('');}}>
                <svg width="16" height="16" viewBox="0 0 16 16" fill="currentColor" style={{verticalAlign: 'middle', marginTop: '-10px', marginLeft: '4px', marginBottom: '4px', marginRight: '4px'}}>
                  <path d="M8 2L2 7v7h4v-4h4v4h4V7L8 2z"/>
                </svg>
              </a>
              {album.navigation_path_segments.map((segment, index) => {
                const pathToSegment = '\\' + album.navigation_path_segments.slice(0, index + 1).join('\\');
                return (
                  <span key={index}>
                    {' > '} <a href="#" onClick={(e) => {e.preventDefault(); onAlbumClick(pathToSegment);}}>{segment}</a>
                  </span>
                );
              })}
            </nav>
            {image.is_movie 
                ? (<video ref={videoRef} src={image.image_original_path}
                   poster={image.image_uhd_path || image.thumbnail_path}  
                   controls onContextMenu={(e) => e.preventDefault()} />) 
                : (
                  <img ref={imageRef} src={image.image_uhd_path} alt={image.name} onContextMenu={(e) => e.preventDefault()}
                    style={{
                      transform: `translate(${position.x}px, ${position.y}px) scale(${zoom})`,
                      transformOrigin: '0 0',
                      cursor: zoom > 1 ? (isDragging ? 'grabbing' : 'grab') : 'default',
                      transition: isDragging ? 'none' : 'transform 0.1s ease-out',
                    }}
                  />
                )}
        </div>

        <div className="nav nav-next">
            <div className="toolbar">
                <button onClick={onClose} className="close-button">
                    <svg viewBox="0 0 24 24" fill="none">
                        <path d="M4,4 L19,20 M19,4 L4,20" stroke="white" fill="black" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/>
                    </svg>                
                </button>
            </div>
            <div className="nav-btn">
                <button onClick={() => onNext(image, album)} className="next-button">
                    <svg viewBox="0 0 24 24" fill="none">
                        <polyline points="9,4 17,12 9,20" stroke="white" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                    </svg>
                </button>
            </div>
            <div className="spacer-next"></div>
        </div>
    </div>


  );
}
