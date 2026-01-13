import React from 'react';
import { AlbumItemHierarchy, ImageItemContent } from './AlbumHierarchyProps';
import { MetadataPanel } from './Exif';
import { ImageZoomAndTouchNavigation } from './ImageZoomAndTouchNavigation';
import { AuthenticatedImage } from '@/app/utils/AuthenticatedImage';
import { AuthenticatedVideo } from '@/app/utils/AuthenticatedVideo';

// function supportsNativeTouchZoom() {
//   // if (typeof navigator === 'undefined') return false;
//   // return /Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(navigator.userAgent);
//   if (typeof window === 'undefined') return false;
//   return (
//     ('ontouchstart' in window || (navigator && navigator.maxTouchPoints > 0)) &&
//     window.matchMedia && window.matchMedia('(pointer: coarse)').matches
//   );
// }
import { AppRouterInstance } from 'next/dist/shared/lib/app-router-context.shared-runtime';
import './imageContent.css';

interface ImageViewProps {
  image: ImageItemContent;
  album: AlbumItemHierarchy;
  onAlbumClick: (albumId: number | null) => void;
  onClose: () => void;
  router: AppRouterInstance;
  isFullscreen: boolean;  
  setIsFullscreen: (value: boolean) => void;
  path: string;
  useOriginalImage?: boolean; // If true, use image_original_path; otherwise use image_uhd_path
}

export function ImageView(props: ImageViewProps): JSX.Element {
  const videoRef = React.useRef<HTMLVideoElement>(null);
  const imageRef = React.useRef<HTMLImageElement>(null);
  const containerRef = React.useRef<HTMLDivElement>(null);
  const [isSlideshow, setIsSlideshow] = React.useState(false);
  const [slideshowSpeed, setSlideshowSpeed] = React.useState(3000);
  const slideshowIntervalRef = React.useRef<NodeJS.Timeout | null>(null);
  const [showExif, setShowExif] = React.useState(false);
  
  // Navigation handlers
  const handlePrevImage = React.useCallback(() => {
    const content = props.album.images; 
    const ix = content.findIndex(item => item.id === props.image.id); // Using id to avoid issues with duplicate names
    const prev = ix > 0 ? content[ix - 1] : content[content.length - 1]; // Repeat to last if at start
    const currentParams = new URLSearchParams(window.location.search);
    currentParams.set('image', prev.id.toString());
    props.router.push(`${props.path}?${currentParams.toString()}`);
  }, [props.album.images, props.image.id, props.router, props.path]);

  const handleNextImage = React.useCallback(() => {
    const content = props.album.images; 
    const ix = content.findIndex(item => item.id === props.image.id); // Using id to avoid issues with duplicate names
    const next = ix < content.length - 1 ? content[ix + 1] : content[0]; // Repeat to first if at end
    const currentParams = new URLSearchParams(window.location.search);
    currentParams.set('image', next.id.toString());
    props.router.push(`${props.path}?${currentParams.toString()}`);
  }, [props.album.images, props.image.id, props.router, props.path]);


  // Use zoom hook for all zoom-related functionality, but disable zoom if device supports native touch zoom
  const zoom = ImageZoomAndTouchNavigation(imageRef, containerRef, props.image.is_movie, props.image.id, handlePrevImage, handleNextImage);
  // const zoom = React.useMemo(() => {
  //   if (supportsNativeTouchZoom()) {
  //     // Only enable swipe navigation, no zoom
  //     return {
  //       state: { zoom: 1, position: { x: 0, y: 0 }, isDragging: false, is1to1: false },
  //       handlers: {
  //         toggle1to1: () => {},
  //         reset: () => {},
  //         zoomIn: () => {},
  //         zoomOut: () => {},
  //       },
  //       setZoom: () => {},
  //       setIs1to1: () => {},
  //     };
  //   }
  //   return ImageZoomAndTouchNavigation(imageRef, containerRef, props.image.is_movie, props.image.id, handlePrevImage, handleNextImage);
  // }, [props.image.id, props.image.is_movie, handlePrevImage, handleNextImage]);

  
  // Sync state with actual fullscreen status on mount
  React.useEffect(() => {    
    props.setIsFullscreen(!!document.fullscreenElement);
  }, [props.setIsFullscreen]);

  ///////////////////////////////////////////////////////////////////////////////////////////
  // Slideshow effect
  React.useEffect(() => {
    if (isSlideshow) {
      slideshowIntervalRef.current = setInterval(() => {
        handleNextImage();
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
  }, [isSlideshow, slideshowSpeed, props.image, props.album]);

  
  ///////////////////////////////////////////////////////////////////////////////////////////
  // Keyboard navigation
  React.useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      switch(e.key) {
        case 'ArrowLeft':
          handlePrevImage();
          break;
        case 'ArrowRight':
          handleNextImage();
          break;
        case 'Escape':
          if (!document.fullscreenElement) {
            props.onClose();
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
          zoom.handlers.reset();
          break;
        case '1':
          zoom.handlers.toggle1to1();
          break;
        case '=':
        case '+':
          zoom.handlers.zoomIn();
          break;
        case '-':
        case '_':
          zoom.handlers.zoomOut();
          break;
        case ' ':         //spacebar for slideshow toggle  
        //case 'Enter':   //some TVs will trigger Enter when clicking the remote OK button, and it should just be cllicked
          e.preventDefault(); // Prevent default for spacebar and enter
          if (props.image.is_movie && videoRef.current) {
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
  }, [handlePrevImage, handleNextImage, props.onClose, props.image, zoom.handlers]);


  ///////////////////////////////////////////////////////////////////////////////////////////
  // Fullscreen toggle
  const toggleFullscreen = () => {
    if (!document.fullscreenElement) {
      console.log('Requesting fullscreen');
      //iOS safary and other browsers may not support fullscreen, so we log this ourselfs to keep the console log clean without the stack trace of the error  
      document.documentElement.requestFullscreen().catch(err => console.error('Fullscreen request failed:', err));  
    } else {
      document.exitFullscreen().catch(err => console.error('Exit fullscreen failed:', err));
    }
  };


  ///////////////////////////////////////////////////////////////////////////////////////////
  // Slideshow toggle  
  const toggleSlideshow = () => setIsSlideshow(!isSlideshow);
  const increaseSpeed = () => setSlideshowSpeed(prev => Math.max(500, prev - 500)); // Decrease interval (faster), minimum 0.5s
  const decreaseSpeed = () => setSlideshowSpeed(prev => Math.min(10000, prev + 500)); // Increase interval (slower), maximum 10s
  const toggleExif = () => setShowExif(prev => !prev);


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

        {showExif && (props.image.image_metadata || props.image.video_metadata) && (
          <MetadataPanel 
            exif={props.image.image_metadata} 
            videoMetadata={props.image.video_metadata}
            album={props.album} 
            image={props.image} 
            onClose={toggleExif} 
          />
        )}

        {/* Content - full viewport */}
        <div className="content" ref={containerRef}>
              <nav className="breadcrumbs">
                <a href="#"onClick={(e) => {e.preventDefault(); props.onAlbumClick(null);}}>
                  <svg width="16" height="16" viewBox="0 0 16 16" fill="currentColor" style={{verticalAlign: 'middle', marginTop: '0px', marginLeft: '4px', marginBottom: '4px', marginRight: '4px'}}>
                    <path d="M8 2L2 7v7h4v-4h4v4h4V7L8 2z"/>
                  </svg>
                </a>
                {props.album.navigation_path_segments.slice(1).map((segment, index) => {
                  //const pathToSegment = '\\' + props.album.navigation_path_segments.slice(0, index + 1).join('\\');
                  return (
                    <span key={index}>
                      {' > '} <a href="#" onClick={(e) => {e.preventDefault(); props.onAlbumClick(segment.id);}}>{segment.name}</a>
                    </span>
                  );
                })}
              </nav>
            {props.image.is_movie 
                ? (<AuthenticatedVideo ref={videoRef} src={props.image.image_original_path}
                   poster={props.image.image_uhd_path || props.image.thumbnail_path}  
                   controls onContextMenu={(e) => e.preventDefault()} />) 
                : (
                  <AuthenticatedImage ref={imageRef} src={props.useOriginalImage ? props.image.image_original_path : props.image.image_uhd_path} alt={props.image.name} onContextMenu={(e) => e.preventDefault()}
                    style={{
                      transform: `translate(${zoom.state.position.x}px, ${zoom.state.position.y}px) scale(${zoom.state.zoom})`,
                      transformOrigin: '0 0',
                      cursor: zoom.state.zoom > 1 ? (zoom.state.isDragging ? 'grabbing' : 'grab') : 'default',
                      transition: zoom.state.isDragging ? 'none' : 'transform 0.1s ease-out',
                    }}
                  />
                )}
        </div>

        {/* Left overlay - toolbar and prev button */}
        <div className="nav-overlay nav-overlay-left">
            <div className="toolbar">
                <button onClick={toggleFullscreen} className="fullscreen-button" title="Fullscreen (F)">
                    <svg viewBox="0 0 24 24" fill="none">
                        <path d={props.isFullscreen ? "M5 13H11V19M4 20L11 13M19 11H13V5M20 4L13 11" : "M10 20H4V14M4 20L11 13M14 4H20V10M20 4L13 11"} stroke="white" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                    </svg>
                </button>
                <button onClick={toggleSlideshow} className="slideshow-button"  title="Slideshow (Spacebar)">
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
                {!props.image.is_movie && (
                  <button onClick={zoom.handlers.toggle1to1} className={`zoom-button ${zoom.state.is1to1 ? 'active' : ''}`} title={zoom.state.is1to1 ? "Fit to Screen (0/1)" : "1:1 Zoom (+/-/0/1)"}>
                    <svg viewBox="0 0 24 24" fill="none">
                      {zoom.state.is1to1 ? (
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

            <button onClick={handlePrevImage} className="nav-arrow prev-button">
                <svg viewBox="0 0 24 24" fill="none">
                    <polyline points="15,4 7,12 15,20" stroke="white" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                </svg>
            </button>
        </div>

        {/* Right overlay - close button and next button */}
        <div className="nav-overlay nav-overlay-right">
            <div className="toolbar">
                <button onClick={props.onClose} className="close-button">
                    <svg viewBox="0 0 24 24" fill="none">
                        <path d="M4,4 L19,20 M19,4 L4,20" stroke="white" fill="black" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/>                
                    </svg>
                </button>
            </div>
            
            <button onClick={handleNextImage} className="nav-arrow next-button">
                <svg viewBox="0 0 24 24" fill="none">
                    <polyline points="9,4 17,12 9,20" stroke="white" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                </svg>
            </button>
        </div>
    </div>


  );
}
