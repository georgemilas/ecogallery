import React, { useState, useRef, useEffect } from 'react';
import { AlbumItemHierarchy } from './AlbumHierarchyProps';
import { useAuth } from '@/app/contexts/AuthContext';
import { AuthenticatedImage } from '@/app/utils/AuthenticatedImage';


interface DraggableBannerProps {
  album: AlbumItemHierarchy;
  isEditMode: boolean;
  onEditModeChange: (isEditMode: boolean) => void;
  onPositionSave: (objectPositionY: number) => Promise<void>;
  objectPositionY?: number;
  label?: React.ReactNode;
  imageSrcOverride?: string | null;
}

export function DraggableBanner({ album, isEditMode, onEditModeChange, onPositionSave, objectPositionY: propObjectPositionY, label, imageSrcOverride }: DraggableBannerProps): JSX.Element {
  const imgRef = useRef<HTMLImageElement>(null);
  const containerRef = useRef<HTMLDivElement>(null);
  const [objectPositionY, setObjectPositionY] = useState(propObjectPositionY ?? 38);
  const [isDragging, setIsDragging] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const dragStartY = useRef(0);
  const startPositionY = useRef(0);
  const { user } = useAuth();

  // Extract current Y position from album if available
  useEffect(() => {
    if (propObjectPositionY != null) {
      setObjectPositionY(propObjectPositionY);
    }
  }, [propObjectPositionY]);

  const handleMouseDown = (e: React.MouseEvent) => {
    if (!isEditMode) return;
    setIsDragging(true);
    dragStartY.current = e.clientY;
    startPositionY.current = objectPositionY;
  };

  const handleMouseMove = (e: MouseEvent) => {
    if (!isDragging || !isEditMode) return;

    const container = containerRef.current;
    if (!container) return;

    const containerHeight = container.offsetHeight;
    const pixelDelta = e.clientY - dragStartY.current;
    // Each pixel of mouse movement = ~0.5% of object-position
    const percentDelta = (pixelDelta / containerHeight) * 100;
    
    // Clamp between 0% and 100%
    const newPosition = Math.max(0, Math.min(100, startPositionY.current - percentDelta));
    setObjectPositionY(newPosition);
  };

  const handleMouseUp = () => {
    setIsDragging(false);
  };

  useEffect(() => {
    if (isDragging) {
      window.addEventListener('mousemove', handleMouseMove);
      window.addEventListener('mouseup', handleMouseUp);
      return () => {
        window.removeEventListener('mousemove', handleMouseMove);
        window.removeEventListener('mouseup', handleMouseUp);
      };
    }
  }, [isDragging, isEditMode, objectPositionY, startPositionY]);

  const handleSavePosition = async () => {
    setIsSaving(true);
    try {
      await onPositionSave(objectPositionY);
      onEditModeChange(false);
    } catch (err) {
      console.error('Failed to save position:', err);
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <div 
      ref={containerRef}
      className="gallery-banner"
      onMouseDown={isEditMode ? handleMouseDown : undefined}
      style={{ cursor: isEditMode ? (isDragging ? 'grabbing' : 'grab') : 'default' }}
    >
      <AuthenticatedImage
        ref={imgRef}
        src={imageSrcOverride || album.image_hd_path}
        alt={album.album_name()}
        style={{
          objectPosition: `50% ${objectPositionY}%`,
          userSelect: isEditMode ? 'none' : 'auto',
        }}
        className="gallery-banner-img"
      />

      {/* Banner label overlay (always shown, overlays bottom of banner) */}
      {label && (
        <div className="gallery-banner-label">
          {label}
        </div>
      )}

      {/* Edit mode controls */}
      {isEditMode && (
        <div className="banner-edit-controls">
          <div className="banner-edit-info">
            <p>Drag the image to adjust the visible area</p>
            <p className="position-display">Position: {objectPositionY.toFixed(1)}%</p>
          </div>
          <div className="banner-edit-buttons">
            <button 
              onClick={handleSavePosition} 
              disabled={isSaving}
              className="save-button"
              title="Save position"
            >
              {isSaving ? 'Saving...' : 'Save'}
            </button>
            <button 
              onClick={() => {
                setObjectPositionY(propObjectPositionY ?? 38);
                onEditModeChange(false);
              }}
              className="cancel-button"
              title="Cancel editing"
            >
              Cancel
            </button>
          </div>
        </div>
      )}

      {/* Edit button in header */}
      {!isEditMode && user?.is_admin && (
        <button
          onClick={() => onEditModeChange(true)}
          className="banner-edit-button"
          title="Edit banner position"
        >
          <svg viewBox="0 0 24 24" fill="none" width="18" height="18">
            <path d="M12 2 L12 6 M12 2 L9.5 4.5 M12 2 L14.5 4.5" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/>
            <path d="M12 22 L12 18 M12 22 L9.5 19.5 M12 22 L14.5 19.5" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/>
            <path d="M2 12 L6 12 M2 12 L4.5 9.5 M2 12 L4.5 14.5" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/>
            <path d="M22 12 L18 12 M22 12 L19.5 9.5 M22 12 L19.5 14.5" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/>
            <circle cx="12" cy="12" r="1.4" stroke="currentColor" strokeWidth="1.8" fill="none"/>
          </svg>
        </button>
      )}
    </div>
  );
}
