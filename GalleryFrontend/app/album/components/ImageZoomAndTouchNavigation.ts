//import { setTimeout } from 'node:timers/promises';
import React, { useMemo } from 'react';

export interface ZoomState {
  zoom: number;
  position: { x: number; y: number };
  isDragging: boolean;
  is1to1: boolean;
}

export interface ZoomHandlers {
  toggle1to1: () => void;
  reset: () => void;
  zoomIn: () => void;
  zoomOut: () => void;
}

export interface ImageZoomResult {
  state: ZoomState;
  handlers: ZoomHandlers;
  setZoom: React.Dispatch<React.SetStateAction<number>>;
  setIs1to1: React.Dispatch<React.SetStateAction<boolean>>;
}

export function ImageZoomAndTouchNavigation(
  imageRef: React.RefObject<HTMLImageElement>,
  containerRef: React.RefObject<HTMLDivElement>,
  isVideo: boolean,
  imageId: number,
  onPrevImage?: () => void,
  onNextImage?: () => void
): ImageZoomResult 
{
  const [zoom, setZoom] = React.useState(1);
  const [position, setPosition] = React.useState({ x: 0, y: 0 });
  const [origX, setOrigX] = React.useState(0);
  const [origY, setOrigY] = React.useState(0);
  const [isDragging, setIsDragging] = React.useState(false);
  const [dragStart, setDragStart] = React.useState({ x: 0, y: 0 });
  const [is1to1, setIs1to1] = React.useState(false);


  // Set origX and origY once per image load
  React.useEffect(() => {
    if (!imageRef.current) return;
    // Wait for image to be fully loaded and rendered
    const handle = setTimeout(() => {
      if (imageRef.current) {
        setOrigX(imageRef.current.getBoundingClientRect().left);
        setOrigY(imageRef.current.getBoundingClientRect().top);
      }
    }, 100);
    return () => clearTimeout(handle);
  }, [imageId, imageRef]);
  
  function supportsNativeTouchZoom() {
    // if (typeof navigator === 'undefined') return false;
    // return /Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(navigator.userAgent);
    if (typeof window === 'undefined') return false;
    return (
      ('ontouchstart' in window || (navigator && navigator.maxTouchPoints > 0)) &&
      window.matchMedia && window.matchMedia('(pointer: coarse)').matches
    );
  }
  const isTouchDevice = useMemo(() => supportsNativeTouchZoom(), []);

  // Reset zoom when image changes
  React.useEffect(() => {
    setZoom(1);
    setPosition({ x: 0, y: 0 });
    setIs1to1(false);
  }, [imageId]);

  // Touch/swipe navigation + pinch zoom
  React.useEffect(() => {
    const touchStartX = { current: 0 };
    const touchEndX = { current: 0 };
    const initialPinchDistance = { current: 0 };
    const lastZoom = { current: 1 };
    const isPinching = { current: false };

    const handleTouchStart = (e: TouchEvent) => {
      if (e.touches.length === 2) {
        isPinching.current = true;
        // If not a video and not native touch device, handle pinch zoom
        if (!isVideo && !isTouchDevice) {
          const dx = e.touches[0].clientX - e.touches[1].clientX;
          const dy = e.touches[0].clientY - e.touches[1].clientY;
          const dist = Math.sqrt(dx * dx + dy * dy);
          if (dist > 10) { // Ignore if initial pinch is too small
            initialPinchDistance.current = dist;
            lastZoom.current = zoom;
          } else {
            initialPinchDistance.current = 0;
          }
        }
      } else if (e.touches.length === 1) {
        isPinching.current = false;
        touchStartX.current = e.touches[0].clientX;
      }
    };

    const handleTouchMove = (e: TouchEvent) => {
      if (e.touches.length === 2 && !isVideo && !isTouchDevice) {
        // Pinch zoom
        const container = containerRef.current;
        if (!container) return;
        if (!initialPinchDistance.current || initialPinchDistance.current < 10) return; // Ignore if initial pinch was too small
        const rect = container.getBoundingClientRect();
        // Midpoint between the two touches
        const midX = (e.touches[0].clientX + e.touches[1].clientX) / 2 - rect.left;
        const midY = (e.touches[0].clientY + e.touches[1].clientY) / 2 - rect.top;
        const dx = e.touches[0].clientX - e.touches[1].clientX;
        const dy = e.touches[0].clientY - e.touches[1].clientY;
        const distance = Math.sqrt(dx * dx + dy * dy);
        let scale = distance / initialPinchDistance.current;
        // Clamp scale factor per gesture to avoid jumps
        scale = Math.max(0.5, Math.min(scale, 2.0));
        const newZoom = Math.min(Math.max(0.5, lastZoom.current * scale), 10);
        // Zoom towards the midpoint between the two touches, relative to the container
        const zoomScale = newZoom / zoom;
        setZoom(newZoom);
        setPosition({
          x: midX - (midX - position.x) * zoomScale,
          y: midY - (midY - position.y) * zoomScale,
        });
        setIs1to1(true);
      } else if (e.touches.length === 1 && zoom <= 1) {
        touchEndX.current = e.touches[0].clientX;
      }
    };

    const handleTouchEnd = (e: TouchEvent) => {
      // Only allow swipe if not pinching
      if (e.touches.length === 0 && zoom <= 1 && !isPinching.current) {
        const swipeThreshold = 50;
        const diff = touchStartX.current - touchEndX.current;

        if (Math.abs(diff) > swipeThreshold) {
          if (diff > 0) {
            onNextImage?.();
          } else {
            onPrevImage?.();
          }
        }
      }
      // Reset all refs
      touchStartX.current = 0;
      touchEndX.current = 0;
      isPinching.current = false;
    };

    window.addEventListener('touchstart', handleTouchStart);
    window.addEventListener('touchmove', handleTouchMove);
    window.addEventListener('touchend', handleTouchEnd);

    return () => {
      window.removeEventListener('touchstart', handleTouchStart);
      window.removeEventListener('touchmove', handleTouchMove);
      window.removeEventListener('touchend', handleTouchEnd);
    };
  }, [zoom, isVideo, onPrevImage, onNextImage, isTouchDevice]);

  // Mouse wheel zoom
  React.useEffect(() => {
    if (isVideo) return;

    const handleWheel = (e: WheelEvent) => {
      e.preventDefault();
      const container = containerRef.current;
      const img = imageRef.current;
      if (!container || !img) return;

      const rect = container.getBoundingClientRect();
      const x = e.clientX - origX;
      const y = e.clientY - origY;
      
      const delta = e.deltaY > 0 ? 0.9 : 1.1;
      const newZoom = Math.min(Math.max(0.5, zoom * delta), 10);      

      // Zoom towards cursor position
      const scale = newZoom / zoom;

      setZoom(newZoom);
      setPosition({
        x: x - ((x - (position.x)) * scale),
        y: y - ((y - position.y) * scale),
      });
      setIs1to1(true);
    };

    const container = containerRef.current;
    if (container) {
      container.addEventListener('wheel', handleWheel, { passive: false });
    }

    return () => {
      if (container) {
        container.removeEventListener('wheel', handleWheel);
      }
    };
  }, [zoom, position, isVideo, imageRef, containerRef]);

  // Mouse drag to pan
  React.useEffect(() => {
    if (isVideo || zoom <= 1) return;

    const handleMouseDown = (e: MouseEvent) => {
      if (e.button === 0) {
        setIsDragging(true);
        setDragStart({ x: e.clientX - position.x, y: e.clientY - position.y });
      }
    };

    const handleMouseMove = (e: MouseEvent) => {
      if (isDragging) {
        setPosition({
          x: e.clientX - dragStart.x,
          y: e.clientY - dragStart.y,
        });
      }
    };

    const handleMouseUp = () => {
      setIsDragging(false);
    };

    const container = containerRef.current;
    if (container) {
      container.addEventListener('mousedown', handleMouseDown);
      window.addEventListener('mousemove', handleMouseMove);
      window.addEventListener('mouseup', handleMouseUp);
    }

    return () => {
      if (container) {
        container.removeEventListener('mousedown', handleMouseDown);
      }
      window.removeEventListener('mousemove', handleMouseMove);
      window.removeEventListener('mouseup', handleMouseUp);
    };
  }, [isDragging, dragStart, position, zoom, isVideo, containerRef, origX, origY]);


  //1 to 1
  const toggle1to1 = React.useCallback(() => {
    if (isVideo) return;
    if (is1to1) {
      setZoom(1);
      setPosition({ x: 0, y: 0 });
      setIs1to1(false);
    } else {
      const img = imageRef.current;
      const container = containerRef.current;
      if (!img || !container) return;

      const containerRect = container.getBoundingClientRect();
      const naturalWidth = img.naturalWidth;
      const naturalHeight = img.naturalHeight;
      const displayedWidth = img.getBoundingClientRect().width;
      const displayedHeight = img.getBoundingClientRect().height;
      
      const actualZoom = naturalWidth / displayedWidth;
      setZoom(actualZoom);
  
      setPosition({
        x: (displayedWidth - naturalWidth) / 2,
        y: (displayedHeight - naturalHeight) / 2,
      });
      setIs1to1(true);        
    }
  }, [isVideo, is1to1, imageRef, containerRef]);

  const reset = React.useCallback(() => {
    setZoom(1);
    setPosition({ x: 0, y: 0 });
    setIs1to1(false);
  }, []);

  const zoomIn = React.useCallback(() => {
    if (isVideo) return;
    setZoom(prev => Math.min(prev * 1.2, 10));
    setIs1to1(false);
  }, [isVideo]);

  const zoomOut = React.useCallback(() => {
    if (isVideo) return;
    setZoom(prev => Math.max(prev / 1.2, 0.5));
    setIs1to1(false);
  }, [isVideo]);




  const state: ZoomState = {zoom, position, isDragging, is1to1};
  const handlers: ZoomHandlers = {toggle1to1, reset, zoomIn, zoomOut};
  const result: ImageZoomResult = { state, handlers, setZoom, setIs1to1 };
  return result;
}
