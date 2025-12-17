import { setTimeout } from 'node:timers/promises';
import React from 'react';

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

export function ImageViewZoom(
  imageRef: React.RefObject<HTMLImageElement>,
  containerRef: React.RefObject<HTMLDivElement>,
  isVideo: boolean,
  imageId: number
) {
  const [zoom, setZoom] = React.useState(1);
  const [position, setPosition] = React.useState({ x: 0, y: 0 });
  const [isDragging, setIsDragging] = React.useState(false);
  const [dragStart, setDragStart] = React.useState({ x: 0, y: 0 });
  const [is1to1, setIs1to1] = React.useState(false);

  // Reset zoom when image changes
  React.useEffect(() => {
    setZoom(1);
    setPosition({ x: 0, y: 0 });
    setIs1to1(false);
  }, [imageId]);

  // Mouse wheel zoom
  React.useEffect(() => {
    if (isVideo) return;

    const handleWheel = (e: WheelEvent) => {
      e.preventDefault();
      
      const container = containerRef.current;
      const img = imageRef.current;
      if (!container || !img) return;

      const rect = container.getBoundingClientRect();
      const x = e.clientX - rect.left;
      const y = e.clientY - rect.top;

      const delta = e.deltaY > 0 ? 0.9 : 1.1;
      const newZoom = Math.min(Math.max(0.5, zoom * delta), 10);

      // Zoom towards cursor position
      const scale = newZoom / zoom;
      setZoom(newZoom);
      setPosition({
        x: x - (x - position.x) * scale,
        y: y - (y - position.y) * scale,
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
  }, [isDragging, dragStart, position, zoom, isVideo, containerRef]);

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
        
        const actualZoom = naturalWidth / displayedWidth;
        setZoom(actualZoom);
    
        setPosition({
          x: (containerRect.width - naturalWidth) / 2,
          y: (containerRect.height - naturalHeight) / 2,
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

  const state: ZoomState = {
    zoom,
    position,
    isDragging,
    is1to1,
  };

  const handlers: ZoomHandlers = {
    toggle1to1,
    reset,
    zoomIn,
    zoomOut,
  };

  return { state, handlers, setZoom, setIs1to1 };
}
