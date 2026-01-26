import React, { useCallback, useEffect, useRef, useState } from 'react';
import { ImageItemContent } from './AlbumHierarchyProps';
import { useVirtualizedGallery, LayoutRow } from './useVirtualizedGallery';
import { CancellableImage } from './CancellableImage';

interface VirtualizedGalleryProps {
  images: ImageItemContent[];
  targetHeight: number;
  gap?: number;
  overscan?: number;
  onImageClick: (image: ImageItemContent) => void;
  getImageLabel: (imageName: string) => string;
  lastViewedImageId?: number | null;
}

interface GalleryRowComponentProps {
  row: LayoutRow;
  rowIndex: number;
  isVisible: boolean;
  gap: number;
  onImageClick: (image: ImageItemContent) => void;
  getImageLabel: (imageName: string) => string;
  onRef: (element: HTMLDivElement | null) => void;
}

function GalleryRowComponent({row, rowIndex, isVisible, gap, onImageClick, getImageLabel, onRef }: GalleryRowComponentProps) {
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
}

function GalleryItem({ image, width, height, isVisible, onClick, label }: GalleryItemProps) {
  const handleClick = (e: React.MouseEvent) => {
    e.preventDefault();
    onClick();
  };

  return (
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
        {image.is_movie && (
          <svg className="gallery-item-video-icon" viewBox="0 0 24 24" fill="none">
            <path d="M8 5v14l11-7L8 5z" fill="currentColor" />
          </svg>
        )}
        <span className="gallery-item-label">{label}</span>
        {image.description && <span className="gallery-item-label">{image.description}</span>}
      </a>
    </div>
  );
}

export function VirtualizedGallery({images, targetHeight, gap = 8, overscan = 2, onImageClick, getImageLabel, lastViewedImageId, }: VirtualizedGalleryProps) {
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

  // INSTANT SCROLL: When rows are calculated and we have a target, scroll immediately using calculated position
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
    const container = containerRef.current;
    if (!container) {
      return;
    }

    // Calculate scroll position: gallery container offset + row's top position + half row height (to center)
    const containerRect = container.getBoundingClientRect();
    const containerTop = containerRect.top + window.scrollY;
    const rowCenterY = containerTop + row.top + row.height / 2;
    const scrollTarget = rowCenterY - window.innerHeight / 2;

    // Scroll immediately to calculated position
    window.scrollTo({ top: Math.max(0, scrollTarget), behavior: 'instant' });
    hasScrolledRef.current = true;

    // Mark the target row and neighbors as visible so images load
    setVisibleRows((prev) => {
      const next = new Set(prev);
      next.add(rowIndex);
      return expandWithOverscan(next);
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
        />
      ))}
    </div>
  );
}
