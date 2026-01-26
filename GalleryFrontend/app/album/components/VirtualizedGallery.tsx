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
  const [scrollTargetRowIndex, setScrollTargetRowIndex] = useState<number | null>(null);

  // Find which row contains a given image ID
  const findRowIndexForImage = useCallback((imageId: number): number => {
    for (let i = 0; i < rows.length; i++) {
      if (rows[i].images.some(({ image }) => image.id === imageId)) {
        return i;
      }
    }
    return -1;
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

  // When lastViewedImageId changes, find its row and mark it visible
  useEffect(() => {
    if (!lastViewedImageId || rows.length === 0) {
      setScrollTargetRowIndex(null);
      return;
    }

    const rowIndex = findRowIndexForImage(lastViewedImageId);
    if (rowIndex >= 0) {
      setScrollTargetRowIndex(rowIndex);
      // Immediately add the target row and its neighbors to visible rows
      setVisibleRows((prev) => {
        const next = new Set(prev);
        next.add(rowIndex);
        return expandWithOverscan(next);
      });
    }
  }, [lastViewedImageId, rows.length, findRowIndexForImage, expandWithOverscan]);

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

  // Scroll to last viewed image after its row becomes visible
  useEffect(() => {
    if (scrollTargetRowIndex === null || !lastViewedImageId || rows.length === 0) return;
    if (!visibleRows.has(scrollTargetRowIndex)) return;

    const scrollToImage = () => {
      const element = document.querySelector(`[data-image-id="${lastViewedImageId}"]`);
      if (element) {
        element.scrollIntoView({ behavior: 'instant', block: 'center' });
        return true;
      }
      return false;
    };

    // Wait a frame for the image to render, then scroll
    const timer = setTimeout(() => {
      if (scrollToImage()) {
        setScrollTargetRowIndex(null); // Clear target after successful scroll
      } else {
        // Retry once more
        setTimeout(() => {
          scrollToImage();
          setScrollTargetRowIndex(null);
        }, 100);
      }
    }, 50);

    return () => clearTimeout(timer);
  }, [scrollTargetRowIndex, visibleRows, lastViewedImageId, rows.length]);

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
