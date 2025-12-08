import React from 'react';
import { AlbumItemHierarchy } from './Album';
import './imageContent.css';

interface ImageViewProps {
  image: AlbumItemHierarchy;
  album: AlbumItemHierarchy;
  onClose: () => void;
  onPrev: (image: AlbumItemHierarchy, album: AlbumItemHierarchy) => void;
  onNext: (image: AlbumItemHierarchy, album: AlbumItemHierarchy) => void;
  isFullscreen: boolean;
  setIsFullscreen: (value: boolean) => void;
}

export function ImageView({ image, album, onClose, onPrev, onNext, isFullscreen, setIsFullscreen }: ImageViewProps) {
  const videoRef = React.useRef<HTMLVideoElement>(null);
  const [isSlideshow, setIsSlideshow] = React.useState(false);
  const [slideshowSpeed, setSlideshowSpeed] = React.useState(3000); // milliseconds
  const slideshowIntervalRef = React.useRef<NodeJS.Timeout | null>(null);


  // Sync state with actual fullscreen status on mount
  React.useEffect(() => {    
    setIsFullscreen(!!document.fullscreenElement);
  }, [setIsFullscreen]);


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
        case ' ':
        case 'Enter':
          if (image.is_movie && videoRef.current) {
            e.preventDefault();
            if (videoRef.current.paused) {
              videoRef.current.play();
            } else {
              videoRef.current.pause();
            }
          }
          break;
      }
    };

    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [image, album, onPrev, onNext, onClose]);


  // Fullscreen toggle
  const toggleFullscreen = () => {
    console.log('Toggle fullscreen clicked');
    if (!document.fullscreenElement) {
      console.log('Requesting fullscreen...');
      document.documentElement.requestFullscreen()
        .then(() => console.log('Fullscreen request succeeded'))
        .catch(err => console.error('Fullscreen request failed:', err));
    } else {
      console.log('Exiting fullscreen...');
      document.exitFullscreen()
        .then(() => console.log('Exit fullscreen succeeded'))
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

        <div className="nav nav-prev">
            <div className="toolbar">
                <button onClick={toggleFullscreen} className="fullscreen-button">
                    <svg viewBox="0 0 24 24" fill="none">
                        <path d={isFullscreen ? "M5 13H11V19M4 20L11 13M19 11H13V5M20 4L13 11" : "M10 20H4V14M4 20L11 13M14 4H20V10M20 4L13 11"} stroke="white" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                    </svg>
                </button>
                <button onClick={toggleSlideshow} className="slideshow-button">
                    <svg viewBox="0 0 24 24" fill="none">
                        {isSlideshow ? (
                            <path d="M6 4h4v16H6V4zm8 0h4v16h-4V4z" fill="white" stroke="white" stroke-width="2"/>
                        ) : (
                            <path d="M8 4 L18 12 L8 20 Z" fill="white" stroke="white" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                        )}
                    </svg>
                </button>
                <svg viewBox="0 0 24 24" fill="none">
                    <path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm0 15c-.55 0-1-.45-1-1v-4c0-.55.45-1 1-1s1 .45 1 1v4c0 .55-.45 1-1 1zm0-8c-.55 0-1-.45-1-1s.45-1 1-1 1 .45 1 1-.45 1-1 1z" fill="black" stroke="white"/>
                </svg>
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

        <div className="content">
            {image.is_movie 
                ? (<video ref={videoRef} src={image.image_original_path} controls onContextMenu={(e) => e.preventDefault()} />) 
                : (<img src={image.image_original_path} alt={image.name} onContextMenu={(e) => e.preventDefault()} />)}
            
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
