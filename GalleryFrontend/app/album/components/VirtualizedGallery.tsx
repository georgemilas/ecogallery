import React, { useCallback, useEffect, useRef, useState } from 'react';
import { useRouter } from 'next/navigation';
import { ImageItemContent, FaceBox } from './AlbumHierarchyProps';
import { useVirtualizedGallery, LayoutRow } from './useVirtualizedGallery';
import { CancellableImage } from './CancellableImage';
import { apiFetch } from '@/app/utils/apiFetch';
import { useAuth } from '@/app/contexts/AuthContext';

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
}

function GalleryRowComponent({row, rowIndex, isVisible, gap, onImageClick, getImageLabel, onRef, showFaceBoxes, onFaceSearch, onFaceDelete, onPersonDelete, onSearchByName, onSearchByPersonId }: GalleryRowComponentProps) {
  return (
    <div
      ref={onRef}
      data-row-index={rowIndex}
      className="gallery-row"
      style={{
        display: 'flex',
        gap: `${gap}px`,
        height: `${row.height}px`,
        marginBottom: `${gap}px`,
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
        />
      ))}
    </div>
  );
}

interface FaceContextMenuProps {
  face: FaceBox;
  position: { x: number; y: number };
  onClose: () => void;
  onNameUpdate: (personId: number, newName: string) => void;
  onFaceSearch: (personId: number, personName: string | null) => void;
  onFaceDelete?: (faceId: number) => void;
  onPersonDelete?: (personId: number) => void;
  onSearchByName?: (name: string) => void;
  onSearchByPersonId?: (personId: number) => void;
}

function FaceContextMenu({ face, position, onClose, onNameUpdate, onFaceSearch, onFaceDelete, onPersonDelete, onSearchByName, onSearchByPersonId }: FaceContextMenuProps) {
  const { user } = useAuth();
  const [name, setName] = useState(face.person_name || '');
  const [saving, setSaving] = useState(false);
  const [deleting, setDeleting] = useState(false);
  const [showDeletePersonConfirm, setShowDeletePersonConfirm] = useState(false);
  const menuRef = useRef<HTMLDivElement>(null);
  const [adjustedPosition, setAdjustedPosition] = useState(position);

  // Adjust position to keep menu within viewport
  useEffect(() => {
    if (menuRef.current) {
      const rect = menuRef.current.getBoundingClientRect();
      const padding = 10; // Minimum distance from edge
      let newX = position.x;
      let newY = position.y;

      // Adjust horizontal position if menu goes off right edge
      if (position.x + rect.width > window.innerWidth - padding) {
        newX = window.innerWidth - rect.width - padding;
      }
      // Adjust if menu goes off left edge
      if (newX < padding) {
        newX = padding;
      }

      // Adjust vertical position if menu goes off bottom edge
      if (position.y + rect.height > window.innerHeight - padding) {
        newY = window.innerHeight - rect.height - padding;
      }
      // Adjust if menu goes off top edge
      if (newY < padding) {
        newY = padding;
      }

      if (newX !== position.x || newY !== position.y) {
        setAdjustedPosition({ x: newX, y: newY });
      }
    }
  }, [position]);

  useEffect(() => {
    const handleClickOutside = (e: MouseEvent) => {
      if (menuRef.current && !menuRef.current.contains(e.target as Node)) {
        onClose();
      }
    };
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, [onClose]);

  const handleSave = async () => {
    if (!face.person_id) return;
    setSaving(true);
    try {
      const response = await apiFetch(`/api/v1/faces/person/${face.person_id}/name`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ name: name || null }),
      });
      if (response.ok) {
        onNameUpdate(face.person_id, name);
        onClose();
      }
    } catch (e) {
      console.error('Failed to update name:', e);
    } finally {
      setSaving(false);
    }
  };

  const handleFindAll = () => {
    if (face.person_id) {
      onFaceSearch(face.person_id, face.person_name);
    }
    onClose();
  };

  const handleSearchByName = () => {
    if (name.trim() && onSearchByName) {
      onSearchByName(name.trim());
      onClose();
    }
  };

  const handleSearchByPersonId = () => {
    if (face.person_id && onSearchByPersonId) {
      onSearchByPersonId(face.person_id);
      onClose();
    }
  };

  const handleDeleteFace = async () => {
    if (!face.face_id || !onFaceDelete) return;
    setDeleting(true);
    try {
      const response = await apiFetch(`/api/v1/faces/${face.face_id}`, {
        method: 'DELETE',
      });
      if (response.ok) {
        onFaceDelete(face.face_id);
        onClose();
      }
    } catch (e) {
      console.error('Failed to delete face:', e);
    } finally {
      setDeleting(false);
    }
  };

  const handleDeletePerson = async () => {
    if (!face.person_id || !onPersonDelete) return;
    setDeleting(true);
    try {
      const response = await apiFetch(`/api/v1/faces/person/${face.person_id}`, {
        method: 'DELETE',
      });
      if (response.ok) {
        onPersonDelete(face.person_id);
        onClose();
      }
    } catch (e) {
      console.error('Failed to delete person:', e);
    } finally {
      setDeleting(false);
      setShowDeletePersonConfirm(false);
    }
  };

  return (
    <div
      ref={menuRef}
      style={{
        position: 'fixed',
        left: adjustedPosition.x,
        top: adjustedPosition.y,
        backgroundColor: '#2a2a2a',
        border: '1px solid #e8f09e',
        borderRadius: '8px',
        padding: '12px',
        zIndex: 3000,
        minWidth: '200px',
        boxShadow: '0 4px 12px rgba(0,0,0,0.5)',
      }}
      onClick={(e) => e.stopPropagation()}
    >
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: '12px' }}>
        <div style={{ color: '#e8f09e', fontWeight: 'bold', fontSize: '12px' }}>
          Face #{face.face_id}
          {face.person_id && (
            <span style={{ color: '#888', fontWeight: 'normal' }}>
              {' '}(Person #{face.person_id})
              {user?.is_admin && onSearchByPersonId && (
                <button
                  onClick={handleSearchByPersonId}
                  style={{
                    background: 'none',
                    border: 'none',
                    color: '#888',
                    cursor: 'pointer',
                    padding: '0 0 0 4px',
                    verticalAlign: 'middle',
                  }}
                  title="Search by Person ID"
                >
                  <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                    <circle cx="10" cy="10" r="6" />
                    <line x1="16" y1="16" x2="21" y2="21" />
                  </svg>
                </button>
              )}
            </span>
          )}
        </div>
        <button
          onClick={onClose}
          style={{
            background: 'none',
            border: 'none',
            color: '#888',
            cursor: 'pointer',
            padding: '0',
            marginLeft: '8px',
            lineHeight: '1',
          }}
          title="Close"
        >
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
            <path d="M18 6L6 18M6 6l12 12" />
          </svg>
        </button>
      </div>

      {user?.is_admin && (
        <>
          <div style={{ marginBottom: '8px', display: 'flex', gap: '4px' }}>
            <input
              type="text"
              value={name}
              onChange={(e) => setName(e.target.value)}
              placeholder="Enter name..."
              style={{
                flex: 1,
                padding: '6px 8px',
                backgroundColor: '#1a1a1a',
                border: '1px solid #555',
                borderRadius: '4px',
                color: 'white',
                fontSize: '12px',
              }}
              onKeyDown={(e) => {
                if (e.key === 'Enter') handleSave();
                if (e.key === 'Escape') onClose();
              }}
              autoFocus
            />
            {onSearchByName && (
              <button
                onClick={handleSearchByName}
                disabled={!name.trim()}
                style={{
                  background: 'none',
                  border: '1px solid #555',
                  borderRadius: '4px',
                  color: name.trim() ? '#e8f09e' : '#555',
                  cursor: name.trim() ? 'pointer' : 'not-allowed',
                  padding: '4px 6px',
                }}
                title="Search by Name"
              >
                <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                  <circle cx="10" cy="10" r="6" />
                  <line x1="16" y1="16" x2="21" y2="21" />
                </svg>
              </button>
            )}
          </div>

          <button
            onClick={handleSave}
            disabled={saving || !face.person_id}
            style={{
              padding: '6px 12px',
              backgroundColor: '#4CAF50',
              color: 'white',
              border: 'none',
              borderRadius: '4px',
              cursor: saving || !face.person_id ? 'not-allowed' : 'pointer',
              opacity: saving || !face.person_id ? 0.5 : 1,
              fontSize: '12px',
              marginBottom: '8px',
              width: '100%',
            }}
          >
            {saving ? 'Saving...' : 'Save Name'}
          </button>
        </>
      )}

      <button
        onClick={handleFindAll}
        disabled={!face.person_id}
        style={{
          padding: '6px 12px',
          backgroundColor: '#e8f09e',
          color: '#333',
          border: 'none',
          borderRadius: '4px',
          cursor: !face.person_id ? 'not-allowed' : 'pointer',
          opacity: !face.person_id ? 0.5 : 1,
          fontSize: '12px',
          width: '100%',
          marginBottom: user?.is_admin ? '8px' : '0',
        }}
      >
        Find All Photos
      </button>

      {user?.is_admin && (
        <>
          {!showDeletePersonConfirm ? (
            <div style={{ display: 'flex', gap: '8px', marginTop: '8px', borderTop: '1px solid #444', paddingTop: '8px' }}>
              <button
                onClick={handleDeleteFace}
                disabled={deleting}
                style={{
                  flex: 1,
                  padding: '6px 8px',
                  backgroundColor: '#dc3545',
                  color: 'white',
                  border: 'none',
                  borderRadius: '4px',
                  cursor: deleting ? 'not-allowed' : 'pointer',
                  opacity: deleting ? 0.5 : 1,
                  fontSize: '11px',
                }}
                title="Delete this face record"
              >
                {deleting ? '...' : 'Delete Face'}
              </button>
              <button
                onClick={() => setShowDeletePersonConfirm(true)}
                disabled={!face.person_id || deleting}
                style={{
                  flex: 1,
                  padding: '6px 8px',
                  backgroundColor: '#dc3545',
                  color: 'white',
                  border: 'none',
                  borderRadius: '4px',
                  cursor: !face.person_id || deleting ? 'not-allowed' : 'pointer',
                  opacity: !face.person_id || deleting ? 0.5 : 1,
                  fontSize: '11px',
                }}
                title="Delete entire person and all associated faces"
              >
                Delete Person
              </button>
            </div>
          ) : (
            <div style={{ marginTop: '8px', borderTop: '1px solid #444', paddingTop: '8px' }}>
              <div style={{ color: '#ff6b6b', fontSize: '11px', marginBottom: '8px' }}>
                Delete person #{face.person_id} and ALL associated faces?
              </div>
              <div style={{ display: 'flex', gap: '8px' }}>
                <button
                  onClick={handleDeletePerson}
                  disabled={deleting}
                  style={{
                    flex: 1,
                    padding: '6px 8px',
                    backgroundColor: '#dc3545',
                    color: 'white',
                    border: 'none',
                    borderRadius: '4px',
                    cursor: deleting ? 'not-allowed' : 'pointer',
                    opacity: deleting ? 0.5 : 1,
                    fontSize: '11px',
                  }}
                >
                  {deleting ? 'Deleting...' : 'Yes, Delete'}
                </button>
                <button
                  onClick={() => setShowDeletePersonConfirm(false)}
                  style={{
                    flex: 1,
                    padding: '6px 8px',
                    backgroundColor: '#555',
                    color: 'white',
                    border: 'none',
                    borderRadius: '4px',
                    cursor: 'pointer',
                    fontSize: '11px',
                  }}
                >
                  Cancel
                </button>
              </div>
            </div>
          )}
        </>
      )}
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
}

function GalleryItem({ image, width, height, isVisible, onClick, label, showFaceBoxes, onFaceSearch, onFaceDelete, onPersonDelete, onSearchByName, onSearchByPersonId }: GalleryItemProps) {
  const [selectedFace, setSelectedFace] = useState<{ face: FaceBox; position: { x: number; y: number } } | null>(null);
  const [faceNames, setFaceNames] = useState<Record<number, string>>({});

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

  const handleNameUpdate = (personId: number, newName: string) => {
    setFaceNames(prev => ({ ...prev, [personId]: newName }));
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
          onNameUpdate={handleNameUpdate}
          onFaceSearch={handleFaceSearch}
          onFaceDelete={onFaceDelete}
          onPersonDelete={onPersonDelete}
          onSearchByName={onSearchByName}
          onSearchByPersonId={onSearchByPersonId}
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
            src={image.thumbnail_path}
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
        {image.is_movie && (
          <svg className="gallery-item-video-icon" viewBox="0 0 24 24" fill="none">
            <path d="M8 5v14l11-7L8 5z" fill="currentColor" />
          </svg>
        )}
        <span className="gallery-item-label">{label}</span>
        {image.description && <span className="gallery-item-label">{image.description}</span>}
      </a>
    </div>
    </>
  );
}

export function VirtualizedGallery({images, targetHeight, gap = 8, overscan = 2, onImageClick, getImageLabel, lastViewedImageId, showFaceBoxes = false, onFaceSearch, onFaceDelete, onPersonDelete, onSearchByName, onSearchByPersonId }: VirtualizedGalleryProps) {
  const { rows, totalHeight, containerRef } = useVirtualizedGallery({images, targetHeight, gap, });

  const rowRefs = useRef<Map<number, HTMLDivElement>>(new Map());
  const observerRef = useRef<IntersectionObserver | null>(null);
  const [visibleRows, setVisibleRows] = useState<Set<number>>(new Set());

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
        />
      ))}
    </div>
  );
}
