import { useState, useEffect, useRef, useMemo } from 'react';
import { ImageItemContent } from './AlbumHierarchyProps';

export interface LayoutImage {
  image: ImageItemContent;
  width: number; // calculated width in pixels
}

export interface LayoutRow {
  images: LayoutImage[];
  height: number; // row height in pixels
  top: number;    // cumulative top position from start of gallery
}

export interface VirtualizedGalleryLayout {
  rows: LayoutRow[];
  totalHeight: number;
  containerRef: React.RefObject<HTMLDivElement>;
}

interface UseVirtualizedGalleryOptions {
  images: ImageItemContent[];
  targetHeight: number;
  gap?: number;
}

/**
 * Get image dimensions with fallbacks
 */
function getImageDimensions(image: ImageItemContent): { width: number; height: number } {
  // Use direct properties first (backend now always provides these)
  if (image.image_width > 0 && image.image_height > 0) {
    return { width: image.image_width, height: image.image_height };
  }

  // Fallback to metadata
  if (image.is_movie && image.video_metadata) {
    const w = image.video_metadata.video_width;
    const h = image.video_metadata.video_height;
    if (w && h && w > 0 && h > 0) {
      return { width: w, height: h };
    }
    return { width: 16, height: 9 }; // Default 16:9 for video
  }

  if (image.image_metadata) {
    const w = image.image_metadata.image_width;
    const h = image.image_metadata.image_height;
    if (w && h && w > 0 && h > 0) {
      return { width: w, height: h };
    }
  }

  return { width: 4, height: 3 }; // Default 4:3 for images
}

/**
 * Pre-calculate justified gallery layout from image metadata.
 * No DOM dependency - works purely with data.
 */
function calculateJustifiedLayout(images: ImageItemContent[], containerWidth: number, targetHeight: number, gap: number ): LayoutRow[] {
  if (containerWidth <= 0 || images.length === 0) {
    return [];
  }

  const rows: LayoutRow[] = [];
  let currentRow: { image: ImageItemContent; ratio: number }[] = [];
  let currentRowWidth = 0;
  let cumulativeTop = 0;

  const justifyRow = (rowImages: { image: ImageItemContent; ratio: number }[], isLastRow: boolean): LayoutRow => {
    const totalGaps = (rowImages.length - 1) * gap;
    const availableWidth = containerWidth - totalGaps;
    const totalRatio = rowImages.reduce((sum, item) => sum + item.ratio, 0);

    let adjustedHeight = availableWidth / totalRatio;

    // Don't stretch last row too much
    const isJustified = !(isLastRow && adjustedHeight > targetHeight);
    if (!isJustified) {
      adjustedHeight = targetHeight;
    }

    // Calculate widths
    const layoutImages: LayoutImage[] = rowImages.map((item, i) => {
      let width = Math.floor(adjustedHeight * item.ratio);

      // Add remainder to last image if justified
      if (isJustified && i === rowImages.length - 1) {
        const calculatedTotal = rowImages.reduce(
          (sum, r) => sum + Math.floor(adjustedHeight * r.ratio),
          0
        );
        const remainder = Math.round(availableWidth - calculatedTotal);
        width += remainder - 1; // -1 to ensure fit
      }

      return { image: item.image, width };
    });

    const rowHeight = Math.floor(adjustedHeight);
    const row: LayoutRow = { images: layoutImages, height: rowHeight, top: cumulativeTop };
    cumulativeTop += rowHeight + gap;
    return row;
  };

  for (let i = 0; i < images.length; i++) {
    const image = images[i];
    const dims = getImageDimensions(image);
    const ratio = dims.width / dims.height;
    const imageWidth = targetHeight * ratio;

    const gapsWidth = currentRow.length * gap;
    const potentialWidth = currentRowWidth + imageWidth + gapsWidth;

    if (potentialWidth > containerWidth && currentRow.length > 0) {
      // Current row is full, justify it
      rows.push(justifyRow(currentRow, false));
      currentRow = [{ image, ratio }];
      currentRowWidth = imageWidth;
    } else {
      currentRow.push({ image, ratio });
      currentRowWidth += imageWidth;
    }

    // Handle last image
    if (i === images.length - 1 && currentRow.length > 0) {
      rows.push(justifyRow(currentRow, true));
    }
  }

  return rows;
}

/**
 * Hook for virtualized gallery with pre-calculated layout.
 * Returns row layout data for rendering.
 */
export function useVirtualizedGallery({images, targetHeight, gap = 8, }: UseVirtualizedGalleryOptions): VirtualizedGalleryLayout {
  const containerRef = useRef<HTMLDivElement>(null) as React.RefObject<HTMLDivElement>;
  const [containerWidth, setContainerWidth] = useState(0);

  // Calculate layout when images or container width changes
  const rows = useMemo(() => {
    return calculateJustifiedLayout(images, containerWidth, targetHeight, gap);
  }, [images, containerWidth, targetHeight, gap]);

  const totalHeight = useMemo(() => {
    if (rows.length === 0) return 0;
    // Sum all row heights plus gaps
    return rows.reduce((sum, row, i) => {
      return sum + row.height + (i < rows.length - 1 ? gap : 0);
    }, 0);
  }, [rows, gap]);

  // Measure container width
  useEffect(() => {
    const container = containerRef.current;
    if (!container) return;

    const measureWidth = () => {
      const styles = getComputedStyle(container);
      const paddingLeft = parseFloat(styles.paddingLeft) || 0;
      const paddingRight = parseFloat(styles.paddingRight) || 0;
      const width = container.clientWidth - paddingLeft - paddingRight;
      setContainerWidth(width);
    };

    measureWidth();

    const resizeObserver = new ResizeObserver(() => {
      measureWidth();
    });
    resizeObserver.observe(container);

    return () => {
      resizeObserver.disconnect();
    };
  }, []);

  return {
    rows,
    totalHeight,
    containerRef,
  };
}

// Export for use in components
export { calculateJustifiedLayout, getImageDimensions };
