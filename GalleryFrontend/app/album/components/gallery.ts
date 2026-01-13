// Debounce helper - waits until resizing stops
export function debounce<T extends (...args: any[]) => any>(
  fn: T,
  delay: number
): (...args: Parameters<T>) => void {
  let timeout: NodeJS.Timeout | undefined;
  return function (...args: Parameters<T>) {
    clearTimeout(timeout);
    timeout = setTimeout(() => fn(...args), delay);
  };
}

export function justifyGallery(gallerySelector: string, targetHeight: number, onComplete?: () => void): void {
  const gallery = document.querySelector<HTMLElement>(gallerySelector);
  if (!gallery) {
    onComplete?.();
    return;
  }
  console.log('Justifying gallery layout');
  const items = Array.from(gallery.querySelectorAll<HTMLLIElement>('li.gallery-item'));
  
  // Extract dimensions from data attributes (set from image metadata)
  const imageData = items.map(item => {
    const img = item.querySelector<HTMLImageElement>('img');
    const width = parseInt(item.getAttribute('data-image-width') || '0');
    const height = parseInt(item.getAttribute('data-image-height') || '0');
    return { img, width, height, item };
  });

  // Check if we have metadata dimensions for all images
  const hasAllMetadata = imageData.every(data => data.width > 0 && data.height > 0);

  if (hasAllMetadata) {
    // Fast path: use metadata dimensions immediately
    console.log('Using metadata dimensions for gallery layout');
    layoutGalleryWithMetadata(gallery, imageData, targetHeight);
    onComplete?.();
  } else {
    // Fallback: wait for images to load and use their natural dimensions
    console.log('Waiting for images to load for gallery layout');
    const images = imageData.map(d => d.img).filter((img): img is HTMLImageElement => img !== null);
    
    Promise.all(
      images.map((img) => {
        // For images that are already complete and have natural dimensions, resolve immediately
        if (img.complete && img.naturalWidth > 0 && img.naturalHeight > 0) {
          return Promise.resolve();
        }
        // Otherwise wait for the load event
        return new Promise((resolve) => {
          img.onload = resolve;
          img.onerror = resolve;
        });
      })
    ).then(() => {
      layoutGalleryWithLoadedImages(gallery, images, targetHeight);
      onComplete?.();
    });
  }
}

interface ImageData {
  img: HTMLImageElement | null;
  width: number;
  height: number;
  item: HTMLLIElement;
}

function layoutGalleryWithMetadata(gallery: HTMLElement, imageData: ImageData[], targetHeight: number): void {
  // Get computed styles to account for padding and gap
  const styles = getComputedStyle(gallery);
  const paddingLeft = parseFloat(styles.paddingLeft) || 0;
  const paddingRight = parseFloat(styles.paddingRight) || 0;
  const gap = parseFloat(styles.gap) || 0;

  // Actual usable width
  const containerWidth = gallery.clientWidth - paddingLeft - paddingRight;
  
  let row: ImageData[] = [];
  let rowWidth = 0;

  imageData.forEach((data, index) => {
    // Skip if we don't have valid dimensions
    if (!data.width || !data.height || !data.img) {
      return;
    }

    const ratio = data.width / data.height;
    const imgWidth = targetHeight * ratio;

    // Gaps would be (row.length) gaps if we add this image
    const gapsWidth = row.length * gap;
    const potentialRowWidth = rowWidth + imgWidth + gapsWidth;

    if (potentialRowWidth > containerWidth && row.length > 0) {
      justifyRowWithMetadata(row, containerWidth, gap, false, targetHeight);
      row = [data];
      rowWidth = imgWidth;
    } else {
      row.push(data);
      rowWidth += imgWidth;
    }

    if (index === imageData.length - 1 && row.length > 0) {
      justifyRowWithMetadata(row, containerWidth, gap, true, targetHeight);
    }
  });
}

function justifyRowWithMetadata(imageDataList: ImageData[], containerWidth: number, gap: number, isLastRow: boolean, targetHeight: number): void {
  const ratios = imageDataList.map((data) => data.width / data.height);
  const totalGaps = (imageDataList.length - 1) * gap;
  const availableWidth = containerWidth - totalGaps;

  const totalRatio = ratios.reduce((sum, r) => sum + r, 0);
  let adjustedHeight = availableWidth / totalRatio;

  // Don't stretch last row too much
  const isJustified = !(isLastRow && adjustedHeight > targetHeight);
  if (!isJustified) {
    adjustedHeight = targetHeight;
  }

  // Calculate widths
  const widths = ratios.map((r) => adjustedHeight * r);

  // Fix rounding: only add remainder if row is justified
  const flooredWidths = widths.map((w) => Math.floor(w));
  const totalCalculatedWidth = flooredWidths.reduce((sum, w) => sum + w, 0);
  const remainder = isJustified ? Math.round(availableWidth - totalCalculatedWidth) : 0;

  imageDataList.forEach((data, i) => {
    if (!data.img) return;
    
    const isLast = i === imageDataList.length - 1;
    const width = isLast ? flooredWidths[i] + remainder - 1 : flooredWidths[i]; //-1 to make sure it fits, just in case

    data.img.style.height = `${Math.floor(adjustedHeight)}px`;
    data.img.style.width = `${width}px`;
  });
}

// Fallback function that uses loaded image dimensions
function layoutGalleryWithLoadedImages(gallery: HTMLElement, images: HTMLImageElement[], targetHeight: number): void {
  const styles = getComputedStyle(gallery);
  const paddingLeft = parseFloat(styles.paddingLeft) || 0;
  const paddingRight = parseFloat(styles.paddingRight) || 0;
  const gap = parseFloat(styles.gap) || 0;
  const containerWidth = gallery.clientWidth - paddingLeft - paddingRight;
  
  let row: HTMLImageElement[] = [];
  let rowWidth = 0;

  images.forEach((img, index) => {
    const ratio = img.naturalWidth / img.naturalHeight;
    const imgWidth = targetHeight * ratio;

    const gapsWidth = row.length * gap;
    const potentialRowWidth = rowWidth + imgWidth + gapsWidth;

    if (potentialRowWidth > containerWidth && row.length > 0) {
      justifyRowWithLoadedImages(row, containerWidth, gap, false, targetHeight);
      row = [img];
      rowWidth = imgWidth;
    } else {
      row.push(img);
      rowWidth += imgWidth;
    }

    if (index === images.length - 1 && row.length > 0) {
      justifyRowWithLoadedImages(row, containerWidth, gap, true, targetHeight);
    }
  });
}

function justifyRowWithLoadedImages(images: HTMLImageElement[], containerWidth: number, gap: number, isLastRow: boolean, targetHeight: number): void {
  const ratios = images.map((img) => img.naturalWidth / img.naturalHeight);
  const totalGaps = (images.length - 1) * gap;
  const availableWidth = containerWidth - totalGaps;

  const totalRatio = ratios.reduce((sum, r) => sum + r, 0);
  let adjustedHeight = availableWidth / totalRatio;

  const isJustified = !(isLastRow && adjustedHeight > targetHeight);
  if (!isJustified) {
    adjustedHeight = targetHeight;
  }

  const widths = ratios.map((r) => adjustedHeight * r);
  const flooredWidths = widths.map((w) => Math.floor(w));
  const totalCalculatedWidth = flooredWidths.reduce((sum, w) => sum + w, 0);
  const remainder = isJustified ? Math.round(availableWidth - totalCalculatedWidth) : 0;

  images.forEach((img, i) => {
    const isLast = i === images.length - 1;
    const width = isLast ? flooredWidths[i] + remainder - 1 : flooredWidths[i];

    img.style.height = `${Math.floor(adjustedHeight)}px`;
    img.style.width = `${width}px`;
  });
}
