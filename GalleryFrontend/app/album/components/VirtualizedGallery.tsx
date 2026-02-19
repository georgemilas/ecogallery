import React, { useCallback, useEffect, useRef, useState } from 'react';
import { ImageItemContent, FaceBox, ImageLocationCluster, LocationHandlers } from './AlbumHierarchyProps';
import { LocationContextMenu } from './LocationContextMenu';
import { FaceContextMenu } from './FaceContextMenu';
import { useVirtualizedGallery, LayoutRow } from './useVirtualizedGallery';
import { CancellableImage } from './CancellableImage';
import { GalleryPickerState } from './GalleryPicker';

interface VirtualizedGalleryProps {
  images: ImageItemContent[];
  targetHeight: number;
  gap?: number;
  overscan?: number;
  onImageClick: (image: ImageItemContent) => void;
  getImageLabel: (imageName: string) => string;
  lastViewedImageId?: number | null;
  showFaceBoxes?: boolean;
  onFaceSearch?: (personId: number, personName: string | null) => void;
  onFaceDelete?: (faceId: number) => void;
  onPersonDelete?: (personId: number) => void;
  onSearchByName?: (name: string) => void;
  onSearchByPersonId?: (personId: number) => void;
  onSearchByClusterId?: (clusterId: number) => void;
  onSearchByClusterName?: (name: string) => void;
  galleryPicker?: GalleryPickerState;
}

interface GalleryRowComponentProps {
  row: LayoutRow;
  rowIndex: number;
  isVisible: boolean;
  gap: number;
  onImageClick: (image: ImageItemContent) => void;
  getImageLabel: (imageName: string) => string;
  onRef: (element: HTMLDivElement | null) => void;
  showFaceBoxes?: boolean;
  onFaceSearch?: (personId: number, personName: string | null) => void;
  onFaceDelete?: (faceId: number) => void;
  onPersonDelete?: (personId: number) => void;
  onSearchByName?: (name: string) => void;
  onSearchByPersonId?: (personId: number) => void;
  faceNames: Record<number, string>;
  onFaceNameUpdate: (personId: number, newName: string) => void;
  locationHandlers: LocationHandlers;
  galleryPicker?: GalleryPickerState;
}

function GalleryRowComponent({row, rowIndex, isVisible, gap, onImageClick, getImageLabel, onRef, showFaceBoxes, onFaceSearch, onFaceDelete, onPersonDelete, onSearchByName, onSearchByPersonId, faceNames, onFaceNameUpdate, locationHandlers, galleryPicker }: GalleryRowComponentProps) {
  return (
    <div
      ref={onRef}
      data-row-index={rowIndex}
      className="gallery-row"
      style={{
        display: 'flex',
        gap: `${gap}px`,
        height: `${row.height}px`,
      }}
    >
      {row.images.map(({ image, width }) => (
        <GalleryItem
          key={image.id}
          image={image}
          width={width}
          height={row.height}
          isVisible={isVisible}
          onClick={() => onImageClick(image)}
          label={getImageLabel(image.name)}
          showFaceBoxes={showFaceBoxes}
          onFaceSearch={onFaceSearch}
          onFaceDelete={onFaceDelete}
          onPersonDelete={onPersonDelete}
          onSearchByName={onSearchByName}
          onSearchByPersonId={onSearchByPersonId}
          faceNames={faceNames}
          onFaceNameUpdate={onFaceNameUpdate}
          locationHandlers={locationHandlers}
          galleryPicker={galleryPicker}
        />
      ))}
    </div>
  );
}

interface GalleryItemProps {
  image: ImageItemContent;
  width: number;
  height: number;
  isVisible: boolean;
  onClick: () => void;
  label: string;
  showFaceBoxes?: boolean;
  onFaceSearch?: (personId: number, personName: string | null) => void;
  onFaceDelete?: (faceId: number) => void;
  onPersonDelete?: (personId: number) => void;
  onSearchByName?: (name: string) => void;
  onSearchByPersonId?: (personId: number) => void;
  faceNames: Record<number, string>;
  onFaceNameUpdate: (personId: number, newName: string) => void;
  locationHandlers: LocationHandlers;
  galleryPicker?: GalleryPickerState;
}

function GalleryItem({ image, width, height, isVisible, onClick, label, showFaceBoxes, onFaceSearch, onFaceDelete, onPersonDelete, onSearchByName, onSearchByPersonId, faceNames, onFaceNameUpdate, locationHandlers, galleryPicker }: GalleryItemProps) {
  const [selectedFace, setSelectedFace] = useState<{ face: FaceBox; position: { x: number; y: number } } | null>(null);
  const [showLocationMenu, setShowLocationMenu] = useState<{ position: { x: number; y: number } } | null>(null);

  const handleClick = (e: React.MouseEvent) => {
    e.preventDefault();
    onClick();
  };

  const handleFaceClick = (e: React.MouseEvent, face: FaceBox) => {
    e.preventDefault();
    e.stopPropagation();
    setSelectedFace({
      face,
      position: { x: e.clientX, y: e.clientY },
    });
  };

  const handleFaceSearch = (personId: number, personName: string | null) => {
    onFaceSearch?.(personId, personName);
  };

  // Calculate face box position scaled to thumbnail dimensions
  const renderFaceBoxes = () => {
    if (!showFaceBoxes || !image.faces || image.faces.length === 0) return null;
    if (!image.image_width || !image.image_height) return null;

    // Scale factor from original image to thumbnail
    const scaleX = width / image.image_width;
    const scaleY = height / image.image_height;

    return image.faces.map((face: FaceBox) => {
      const displayName = face.person_id ? (faceNames[face.person_id] ?? face.person_name) : face.person_name;

      const boxStyle: React.CSSProperties = {
        position: 'absolute',
        left: `${face.bounding_box_x * scaleX}px`,
        top: `${face.bounding_box_y * scaleY}px`,
        width: `${face.bounding_box_width * scaleX}px`,
        height: `${face.bounding_box_height * scaleY}px`,
        border: '2px solid #e8f09e',
        borderRadius: '2px',
        cursor: 'pointer',
        boxSizing: 'border-box',
      };

      return (
        <div
          key={face.face_id}
          style={boxStyle}
          onClick={(e) => handleFaceClick(e, { ...face, person_name: displayName })}
        >
          {displayName && (
            <span style={{
              position: 'absolute',
              bottom: '-18px',
              left: '0',
              backgroundColor: 'rgba(0, 0, 0, 0.7)',
              color: '#e8f09e',
              fontSize: '10px',
              padding: '1px 4px',
              borderRadius: '2px',
              whiteSpace: 'nowrap',
            }}>
              {displayName}
            </span>
          )}
        </div>
      );
    });
  };

  return (
    <>
      {selectedFace && (
        <FaceContextMenu
          face={selectedFace.face}
          position={selectedFace.position}
          onClose={() => setSelectedFace(null)}
          onNameUpdate={onFaceNameUpdate}
          onFaceSearch={handleFaceSearch}
          onFaceDelete={onFaceDelete}
          onPersonDelete={onPersonDelete}
          onSearchByName={onSearchByName}
          onSearchByPersonId={onSearchByPersonId}
        />
      )}
      {showLocationMenu && image.locations && (
        <LocationContextMenu
          clusters={image.locations}
          position={showLocationMenu.position}
          onClose={() => setShowLocationMenu(null)}
          locationHandlers={locationHandlers}
        />
      )}
      <div
      className="gallery-item"
      data-image-id={image.id}
      data-image-name={image.name}
      style={{
        width: `${width}px`,
        height: `${height}px`,
        flexShrink: 0,
        position: 'relative',
        overflow: 'hidden',
      }}
    >
      <a href={`#${image.id}`} onClick={handleClick}>
        {isVisible ? (
          <CancellableImage
            src={height > 400 ? image.image_small_hd_path : image.thumbnail_path}
            alt={image.name}
            loading="lazy"
            enableCancellation={true}
            showLoadingPlaceholder={false}
            style={{
              width: `${width}px`,
              height: `${height}px`,
              objectFit: 'cover',
              display: 'block',
            }}
          />
        ) : (
          <div
            className="gallery-item-placeholder"
            style={{
              width: `${width}px`,
              height: `${height}px`,
              backgroundColor: '#1a1a1a',
            }}
          />
        )}
        {renderFaceBoxes()}
        {image.locations && image.locations.length > 0 && (() => {
          // Find the best display name: check each tier from smallest to largest,
          // using updated clusterNames first, then original data
          const getLocationName = (l: ImageLocationCluster) => locationHandlers.clusterNames[l.cluster_id] || l.name;
          const bestLocation = image.locations.toSorted((a, b) => a.tier_meters - b.tier_meters).find(l => getLocationName(l));
          const displayName = bestLocation ? getLocationName(bestLocation) : null;

          return (
            <div
              onClick={(e) => { e.preventDefault(); e.stopPropagation(); setShowLocationMenu({ position: { x: e.clientX, y: e.clientY } }); }}
              style={{
                position: 'absolute',
                top: '4px',
                left: '4px',
                backgroundColor: 'rgba(0, 0, 0, 0.6)',
                borderRadius: '4px',
                padding: '2px 6px',
                cursor: 'pointer',
                display: 'flex',
                alignItems: 'center',
                gap: '4px',
                zIndex: 10,
              }}
              title={displayName || 'Location'}
            >
              <svg width="12" height="12" viewBox="0 0 24 24" fill="#e8f09e" stroke="none">
                <path d="M12 2C8.13 2 5 5.13 5 9c0 5.25 7 13 7 13s7-7.75 7-13c0-3.87-3.13-7-7-7zm0 9.5c-1.38 0-2.5-1.12-2.5-2.5s1.12-2.5 2.5-2.5 2.5 1.12 2.5 2.5-1.12 2.5-2.5 2.5z"/>
              </svg>
              {displayName && (
                <span style={{ color: '#e8f09e', fontSize: '10px', whiteSpace: 'nowrap', maxWidth: '80px', overflow: 'hidden', textOverflow: 'ellipsis' }}>
                  {displayName}
                </span>
              )}
            </div>
          );
        })()}
        {image.is_movie && (
          <svg className="gallery-item-video-icon" viewBox="0 0 24 24" fill="none">
            <path d="M8 5v14l11-7L8 5z" fill="currentColor" />
          </svg>
        )}
        <span className="gallery-item-label">{label}</span>
        {image.description && <span className="gallery-item-label">{image.description}</span>}
      </a>
      {galleryPicker?.mode && (() => {
        // Normalize URL path for comparison: decode %20, convert slashes to match selectedPaths format
        const decodedUrl = decodeURIComponent(image.image_original_path);
        const isSelected = galleryPicker.selectedPaths.some(p => {
          // Normalize separators: compare with both / and \ variants
          const normalizedP = p.replace(/[\\/]/g, '/');
          return decodedUrl.replace(/[\\/]/g, '/').endsWith(normalizedP);
        });
        const isMulti = galleryPicker.mode === 'multi_image';
        return (
        <div
          onClick={(e) => { e.preventDefault(); e.stopPropagation(); galleryPicker.onPick(image); }}
          style={{
            position: 'absolute',
            top: '6px',
            right: '6px',
            width: '22px',
            height: '22px',
            borderRadius: isMulti ? '4px' : '50%',
            border: `2px solid ${isSelected ? '#e8f09e' : 'rgba(255,255,255,0.7)'}`,
            backgroundColor: isSelected ? '#e8f09e' : 'rgba(0,0,0,0.4)',
            cursor: 'pointer',
            zIndex: 10,
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            boxShadow: '0 1px 3px rgba(0,0,0,0.5)',
          }}
          title={isMulti ? 'Toggle image selection' : 'Select as cover image'}
        >
          {isSelected && (
            <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="#333" strokeWidth="3" strokeLinecap="round" strokeLinejoin="round">
              <polyline points="20 6 9 17 4 12" />
            </svg>
          )}
        </div>
        );
      })()}
    </div>
    </>
  );
}

export function VirtualizedGallery({images, targetHeight, gap = 8, overscan = 2, onImageClick, getImageLabel, lastViewedImageId, showFaceBoxes = false, onFaceSearch, onFaceDelete, onPersonDelete, onSearchByName, onSearchByPersonId, onSearchByClusterId, onSearchByClusterName, galleryPicker }: VirtualizedGalleryProps) {
  const { rows, totalHeight, containerRef } = useVirtualizedGallery({images, targetHeight, gap, });

  const rowRefs = useRef<Map<number, HTMLDivElement>>(new Map());
  const observerRef = useRef<IntersectionObserver | null>(null);
  const [visibleRows, setVisibleRows] = useState<Set<number>>(new Set());

  // Shared face names state - lifted from GalleryItem so all items share the same names
  const [faceNames, setFaceNames] = useState<Record<number, string>>({});
  const handleFaceNameUpdate = useCallback((personId: number, newName: string) => {
    setFaceNames(prev => ({ ...prev, [personId]: newName }));
  }, []);

  // Shared cluster names state - lifted so all items share updated names
  const [clusterNames, setClusterNames] = useState<Record<number, string>>({});
  const handleClusterNameUpdate = useCallback((clusterId: number, newName: string) => {
    setClusterNames(prev => ({ ...prev, [clusterId]: newName }));
  }, []);
  const locationHandlers: LocationHandlers = {
    clusterNames,
    onClusterNameUpdate: handleClusterNameUpdate,
    onSearchByClusterId,
    onSearchByClusterName,
  };

  // Track scroll state
  const pendingScrollTarget = useRef<number | null>(null);
  const hasScrolledRef = useRef<boolean>(false);

  // Store lastViewedImageId in ref when it changes
  useEffect(() => {
    if (lastViewedImageId) {
      pendingScrollTarget.current = lastViewedImageId;
      hasScrolledRef.current = false;
    }
  }, [lastViewedImageId]);

  // Find which row contains a given image ID and return row info
  const findRowForImage = useCallback((imageId: number): { rowIndex: number; row: LayoutRow } | null => {
    for (let i = 0; i < rows.length; i++) {
      if (rows[i].images.some(({ image }) => image.id === imageId)) {
        return { rowIndex: i, row: rows[i] };
      }
    }
    return null;
  }, [rows]);

  // Expand visible rows with overscan
  const expandWithOverscan = useCallback((indices: Set<number>): Set<number> => {
    const expanded = new Set<number>();
    indices.forEach((idx) => {
      for (let i = Math.max(0, idx - overscan); i <= Math.min(rows.length - 1, idx + overscan); i++) {
        expanded.add(i);
      }
    });
    return expanded;
  }, [overscan, rows.length]);

  // INSTANT SCROLL: When rows are calculated and we have a target, scroll using pure math
  useEffect(() => {
    const targetId = pendingScrollTarget.current;
    if (!targetId || rows.length === 0 || hasScrolledRef.current) {
      return;
    }

    const result = findRowForImage(targetId);
    if (!result) {
      return;
    }

    const { rowIndex, row } = result;

    // Mark the target row and neighbors as visible so images load
    setVisibleRows((prev) => {
      const next = new Set(prev);
      next.add(rowIndex);
      return expandWithOverscan(next);
    });

    // Wait for layout to complete before measuring positions
    requestAnimationFrame(() => {
      requestAnimationFrame(() => {
        const container = containerRef.current;
        if (!container) {
          hasScrolledRef.current = true;
          return;
        }

        // Get container's absolute position in the document
        const containerRect = container.getBoundingClientRect();
        const containerTopInDocument = containerRect.top + window.scrollY;

        // Calculate the target row's center position in the document
        const rowCenterY = containerTopInDocument + row.top + row.height / 2;

        // Scroll so the row is centered in viewport
        const scrollTarget = rowCenterY - window.innerHeight / 2;

        window.scrollTo({ top: Math.max(0, scrollTarget), behavior: 'instant' });
        hasScrolledRef.current = true;
      });
    });
  }, [rows, findRowForImage, expandWithOverscan, containerRef]);

  // Set up IntersectionObserver for row visibility
  useEffect(() => {
    const rootMargin = `${targetHeight * (overscan + 1)}px 0px`;

    observerRef.current = new IntersectionObserver(
      (entries) => {
        setVisibleRows((prev) => {
          const next = new Set(prev);
          entries.forEach((entry) => {
            const rowIndex = parseInt(entry.target.getAttribute('data-row-index') || '-1', 10);
            if (rowIndex >= 0) {
              if (entry.isIntersecting) {
                next.add(rowIndex);
              } else {
                next.delete(rowIndex);
              }
            }
          });
          return expandWithOverscan(next);
        });
      },
      {
        rootMargin,
        threshold: 0,
      }
    );

    // Observe all registered row refs
    rowRefs.current.forEach((element) => {
      observerRef.current?.observe(element);
    });

    return () => {
      observerRef.current?.disconnect();
    };
  }, [targetHeight, overscan, rows.length, expandWithOverscan]);

  // Register row ref with observer
  const registerRowRef = useCallback(
    (rowIndex: number) => (element: HTMLDivElement | null) => {
      if (element) {
        rowRefs.current.set(rowIndex, element);
        observerRef.current?.observe(element);
      } else {
        const oldElement = rowRefs.current.get(rowIndex);
        if (oldElement) {
          observerRef.current?.unobserve(oldElement);
        }
        rowRefs.current.delete(rowIndex);
      }
    },
    []
  );

  if (images.length === 0) {
    return null;
  }

  return (
    <div
      ref={containerRef}
      className="gallery virtualized-gallery"
      style={{
        minHeight: totalHeight > 0 ? `${totalHeight}px` : undefined,
        display: 'flex',
        flexDirection: 'column',
        gap: `${gap}px`,
      }}
    >
      {rows.map((row, rowIndex) => (
        <GalleryRowComponent
          key={rowIndex}
          row={row}
          rowIndex={rowIndex}
          isVisible={visibleRows.has(rowIndex)}
          gap={gap}
          onImageClick={onImageClick}
          getImageLabel={getImageLabel}
          onRef={registerRowRef(rowIndex)}
          showFaceBoxes={showFaceBoxes}
          onFaceSearch={onFaceSearch}
          onFaceDelete={onFaceDelete}
          onPersonDelete={onPersonDelete}
          onSearchByName={onSearchByName}
          onSearchByPersonId={onSearchByPersonId}
          faceNames={faceNames}
          onFaceNameUpdate={handleFaceNameUpdate}
          locationHandlers={locationHandlers}
          galleryPicker={galleryPicker}
        />
      ))}
    </div>
  );
}
