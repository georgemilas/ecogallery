# Plan: Performant Lazy-Loading for Large Gallery Thumbnails

## Current Implementation Analysis

### 1. Gallery Justify Logic ([gallery.ts](GalleryFrontend/app/album/components/gallery.ts))

**How it works:**
```typescript
export function justifyGallery(gallerySelector: string, targetHeight: number, onComplete?: () => void)
```

1. **Dual-path approach for layout calculation:**
   - **Fast path (with metadata):** If all images have `data-image-width` and `data-image-height` attributes, layout is calculated immediately using aspect ratios from metadata
   - **Fallback path (without metadata):** Waits for ALL images to load (`img.complete && img.naturalWidth > 0`) before calculating layout

2. **Layout algorithm:**
   - Groups images into rows based on `targetHeight` and container width
   - Calculates aspect ratios: `width / height`
   - Fills rows until they would exceed container width
   - Justifies each row by adjusting height to stretch images to fill width
   - Last row is NOT stretched (maintains `targetHeight`)

3. **Problem with fallback path:**
   - `allImagesLoaded()` waits for EVERY image to load before layout
   - With 1000+ images, this causes significant delay and layout shift
   - No incremental/progressive rendering

### 2. CancellableImage Component ([CancellableImage.tsx](GalleryFrontend/app/album/components/CancellableImage.tsx))

**How it works:**
```typescript
export function CancellableImage({ src, alt, enableCancellation, showLoadingPlaceholder, width, height, ... })
```

1. **Uses `useMediaLoader` hook** to fetch images
2. **Loading strategies** (based on URL pattern):
   - `progressive` (`/_thumbnails/400/`): Direct URL, no auth needed
   - `authenticated` (`/_thumbnails/`): Blob fetch with API key
   - `secure` (`/pictures/`): Full auth (API key + session)

3. **Placeholder support:**
   - `showLoadingPlaceholder={true}` shows a spinner while loading
   - Uses `width` and `height` props to calculate `aspectRatio` for placeholder sizing
   - Currently **NOT USED** in BaseHierarchyView (`showLoadingPlaceholder={false}`)

4. **Cancellation:**
   - Uses `AbortController` to cancel in-flight requests on unmount
   - Prevents memory leaks and wasted bandwidth

### 3. LazyLoadedImage Component (UNUSED)

**Current implementation:**
```typescript
export function LazyLoadedImage({ isVisible = true, loadDelay = 0, ...props })
```

1. **Visibility-based loading:** Only loads when `isVisible` is true
2. **Load delay:** Optional delay before starting load (for debouncing)
3. **Problem:** Requires external `isVisible` state - not using IntersectionObserver

**Why it's unused:** The component expects visibility state to be passed in, but BaseHierarchyView doesn't track which images are in viewport.

### 4. Metadata Usage in Justify

**In BaseHierarchyView ([BaseHierarchyView.tsx:286-300](GalleryFrontend/app/album/components/BaseHierarchyView.tsx#L286-L300)):**
```tsx
{props.album.images.map(r => {
    const width = r.is_movie ? r.video_metadata?.video_width ?? null : r.image_metadata?.image_width ?? null;
    const height = r.is_movie ? r.video_metadata?.video_height ?? null : r.image_metadata?.image_height ?? null;
    return (
        <li ... data-image-width={width ?? 0} data-image-height={height ?? 0}>
            <CancellableImage ... width={width} height={height} />
        </li>
    );
})}
```

**In justify logic ([gallery.ts:22-31](GalleryFrontend/app/album/components/gallery.ts#L22-L31)):**
```typescript
const imageData = items.map(item => {
    const width = parseInt(item.getAttribute('data-image-width') || '0');
    const height = parseInt(item.getAttribute('data-image-height') || '0');
    return { img, width, height, item };
});

const hasAllMetadata = imageData.every(data => data.width > 0 && data.height > 0);
```

**When metadata is available:**
- Layout is calculated immediately from data attributes
- Images start loading in parallel
- No layout shift as images load

**When metadata is missing (width=0, height=0):**
- Falls back to waiting for ALL images to load
- Gets `naturalWidth`/`naturalHeight` from loaded images
- Layout calculation delayed until every image completes
- Significant perceived latency for large galleries

### 5. Backend Data Delivery

**API Response structure ([AlbumsService.cs](GalleryApi/service/AlbumsService.cs)):**
```json
{
  "images": [
    {
      "id": 123,
      "name": "vacation.jpg",
      "thumbnail_path": "/pictures/_thumbnails/400/vacation.jpg",
      "image_metadata": {
        "image_width": 4000,
        "image_height": 3000,
        // ... other EXIF data
      },
      "video_metadata": null
    }
  ]
}
```

**Current behavior:**
- Metadata is fetched via `LEFT JOIN` in SQL - returns null if not extracted
- Image dimensions come from EXIF (`image_metadata.image_width/height`)
- Video dimensions come from FFmpeg (`video_metadata.video_width/height`)

**Potential issue:**
- If metadata extraction failed or wasn't run, dimensions are null
- No fallback dimension source

---

## Problems to Solve

### Problem 1: No True Lazy Loading
- All 1000+ images start loading immediately (browser native `loading="lazy"` isn't reliable)
- Network bandwidth saturated
- Memory pressure from too many concurrent fetches

### Problem 2: Layout Shift Without Metadata
- If even ONE image lacks metadata, entire gallery waits for all images
- Users see a blank gallery for seconds

### Problem 3: No Virtualization
- All 1000+ `<li>` elements are in DOM
- Large DOM = slow rendering and scrolling

### Problem 4: Unused LazyLoadedImage
- Good foundation but not connected to viewport tracking

---

## Proposed Solution: Progressive Lazy Loading Architecture

### Phase 1: Viewport-Aware Lazy Loading

**Goal:** Only load images that are visible or about to be visible.

#### 1.1 Create VirtualizedGallery Component

```typescript
interface VirtualizedGalleryProps {
  images: ImageItemContent[];
  targetHeight: number;
  onImageClick: (image: ImageItemContent) => void;
  overscan?: number; // Number of rows to pre-render above/below viewport
}
```

**Key features:**
- Use `IntersectionObserver` to track which rows are in/near viewport
- Only render `<li>` elements for visible rows + overscan
- Pre-calculate row layout using metadata (instant, no DOM needed)

#### 1.2 Enhance LazyLoadedImage with IntersectionObserver

```typescript
export function LazyLoadedImage({
  src,
  alt,
  width,
  height,
  loadDelay = 50, // Small delay to batch rapid visibility changes
  rootMargin = "200px", // Start loading 200px before entering viewport
  ...props
}: LazyLoadedImageProps) {
  const [isVisible, setIsVisible] = useState(false);
  const ref = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const observer = new IntersectionObserver(
      ([entry]) => setIsVisible(entry.isIntersecting),
      { rootMargin, threshold: 0 }
    );
    if (ref.current) observer.observe(ref.current);
    return () => observer.disconnect();
  }, [rootMargin]);

  return (
    <div ref={ref} style={{ aspectRatio: width && height ? `${width}/${height}` : undefined }}>
      {isVisible ? (
        <CancellableImage src={src} alt={alt} width={width} height={height} {...props} />
      ) : (
        <div className="image-placeholder" style={{ aspectRatio: `${width}/${height}` }} />
      )}
    </div>
  );
}
```

### Phase 2: Pre-Calculated Layout (No DOM Dependency)

**Goal:** Calculate justified layout BEFORE rendering, using only metadata.

#### 2.1 Create JustifyLayout Utility

```typescript
interface LayoutRow {
  images: ImageItemContent[];
  height: number;
  widths: number[];
}

function calculateJustifiedLayout(
  images: ImageItemContent[],
  containerWidth: number,
  targetHeight: number,
  gap: number
): LayoutRow[] {
  const rows: LayoutRow[] = [];
  let currentRow: ImageItemContent[] = [];
  let currentRowWidth = 0;

  for (const image of images) {
    const { width, height } = getImageDimensions(image);
    const aspectRatio = width / height || 1; // Fallback to square
    const imageWidth = targetHeight * aspectRatio;

    const gapsWidth = currentRow.length * gap;
    const potentialWidth = currentRowWidth + imageWidth + gapsWidth;

    if (potentialWidth > containerWidth && currentRow.length > 0) {
      rows.push(justifyRow(currentRow, containerWidth, gap, targetHeight, false));
      currentRow = [image];
      currentRowWidth = imageWidth;
    } else {
      currentRow.push(image);
      currentRowWidth += imageWidth;
    }
  }

  if (currentRow.length > 0) {
    rows.push(justifyRow(currentRow, containerWidth, gap, targetHeight, true));
  }

  return rows;
}
```

#### 2.2 Modify BaseHierarchyView to Use Pre-Calculated Layout

```typescript
const [layout, setLayout] = useState<LayoutRow[]>([]);
const containerRef = useRef<HTMLDivElement>(null);

useEffect(() => {
  if (!containerRef.current) return;
  const containerWidth = containerRef.current.clientWidth;
  const newLayout = calculateJustifiedLayout(
    props.album.images,
    containerWidth,
    getResponsiveHeight(),
    8 // gap
  );
  setLayout(newLayout);
}, [props.album.images, windowWidth]);

// Render using pre-calculated layout
<div ref={containerRef} className="gallery">
  {layout.map((row, rowIndex) => (
    <div key={rowIndex} className="gallery-row" style={{ height: row.height }}>
      {row.images.map((image, imgIndex) => (
        <LazyLoadedImage
          key={image.id}
          src={image.thumbnail_path}
          alt={image.name}
          width={row.widths[imgIndex]}
          height={row.height}
          onClick={() => props.onImageClick(image)}
        />
      ))}
    </div>
  ))}
</div>
```

### Phase 3: Handle Missing Metadata Gracefully

**Goal:** Never block layout for missing metadata.

#### 3.1 Backend: Add Fallback Dimensions

In thumbnail generation, always store dimensions:

```csharp
// MultipleThumbnailsProcessor.cs - when creating 400px thumbnail
var thumbnailWidth = originalWidth * 400 / originalHeight;
// Store in database if image_metadata is missing
await StoreThumbDimensions(imageId, thumbnailWidth, 400);
```

**Alternative:** Add `thumb_width` and `thumb_height` columns to `album_image` table (simpler than relying on full metadata).

#### 3.2 Frontend: Default Aspect Ratio

```typescript
function getImageDimensions(image: ImageItemContent): { width: number; height: number } {
  if (image.is_movie) {
    return {
      width: image.video_metadata?.video_width ?? 16,
      height: image.video_metadata?.video_height ?? 9
    };
  }
  return {
    width: image.image_metadata?.image_width ?? 4,
    height: image.image_metadata?.image_height ?? 3
  };
}
```

Uses 4:3 default for photos, 16:9 for videos - close enough for layout without metadata.

### Phase 4: Request Prioritization & Batching

**Goal:** Load visible images first, defer others.

#### 4.1 Priority Queue for Image Loading

```typescript
class ImageLoadQueue {
  private queue: Map<string, { priority: number; resolve: Function }> = new Map();
  private activeRequests = 0;
  private maxConcurrent = 6; // Browser limit per domain

  enqueue(url: string, priority: number): Promise<string> {
    return new Promise((resolve) => {
      this.queue.set(url, { priority, resolve });
      this.processQueue();
    });
  }

  private async processQueue() {
    if (this.activeRequests >= this.maxConcurrent) return;

    // Sort by priority (lower = higher priority)
    const sorted = [...this.queue.entries()]
      .sort((a, b) => a[1].priority - b[1].priority);

    const [url, { resolve }] = sorted[0] || [];
    if (!url) return;

    this.queue.delete(url);
    this.activeRequests++;

    try {
      const blobUrl = await fetchImage(url);
      resolve(blobUrl);
    } finally {
      this.activeRequests--;
      this.processQueue();
    }
  }

  updatePriority(url: string, newPriority: number) {
    const item = this.queue.get(url);
    if (item) item.priority = newPriority;
  }

  cancelAll() {
    // Cancel pending requests
  }
}
```

#### 4.2 Priority Based on Viewport Distance

```typescript
// In LazyLoadedImage
const distanceFromViewport = calculateDistanceFromViewport(ref.current);
imageQueue.enqueue(src, distanceFromViewport);
```

### Phase 5: Backend Optimization

#### 5.1 Add Thumbnail Dimensions to album_image Table

```sql
ALTER TABLE album_image ADD COLUMN thumb_width INTEGER;
ALTER TABLE album_image ADD COLUMN thumb_height INTEGER;
```

Update during thumbnail generation:
```csharp
// After generating 400px thumbnail
await UpdateThumbDimensions(imageId, thumbWidth, 400);
```

#### 5.2 Return Thumbnail Dimensions in API

```sql
SELECT
    ai.id,
    ai.thumb_width,
    ai.thumb_height,
    -- existing fields...
FROM album_image ai
```

This guarantees dimensions are always available, even if full EXIF metadata extraction failed.

---

## Implementation Order

### Step 1: Quick Wins (Low Risk)
1. Enable `showLoadingPlaceholder={true}` in BaseHierarchyView
2. Use default aspect ratios for missing metadata (4:3 / 16:9)
3. Add `rootMargin` to start loading before visible

### Step 2: Core Architecture
1. Create pre-calculated layout utility (`calculateJustifiedLayout`)
2. Refactor BaseHierarchyView to use pre-calculated layout
3. Enhance LazyLoadedImage with IntersectionObserver

### Step 3: Backend Support
1. Add `thumb_width`/`thumb_height` to `album_image` table
2. Update thumbnail processor to store dimensions
3. Return dimensions in API response

### Step 4: Advanced Optimization
1. Implement priority queue for image loading
2. Add virtualization for extremely large galleries (10K+)
3. Consider row-based rendering instead of individual images

---

## Expected Improvements

| Metric | Current | After Phase 2 | After Phase 4 |
|--------|---------|---------------|---------------|
| Time to First Image | 2-3s (waits for layout) | <100ms | <100ms |
| Time to Interactive | 5-10s (all images loading) | <500ms | <500ms |
| Memory Usage | High (all blobs at once) | Medium | Low |
| Scroll Performance | Janky (large DOM) | Smooth | Very Smooth |
| Bandwidth | Saturated | Controlled | Prioritized |

---

## Files to Modify

### Frontend
- `GalleryFrontend/app/album/components/CancellableImage.tsx` - Enhance LazyLoadedImage
- `GalleryFrontend/app/album/components/gallery.ts` - Add pre-calculated layout
- `GalleryFrontend/app/album/components/BaseHierarchyView.tsx` - Use new components
- `GalleryFrontend/app/album/hooks/useImageLoader.tsx` - Add priority queue

### Backend
- `GalleryLib/db/db.sql` - Add thumb dimensions columns
- `GalleryLib/service/thumbnail/MultipleThumbnailsProcessor.cs` - Store dimensions
- `GalleryLib/repository/AlbumRepository.cs` - Return dimensions in query
- `GalleryLib/model/album/AlbumContentHierarchical.cs` - Add properties

---

## Open Questions for Review

1. **Default aspect ratio:** Should we use 4:3 for photos and 16:9 for videos, or calculate average from known images?

2. **Virtualization threshold:** At what image count should we switch to virtualized rendering? (Proposal: 200+)

3. **Overscan amount:** How many rows to pre-render outside viewport? (Proposal: 2 rows)

4. **Priority queue:** Should we use existing `useGalleryThumbnailLoader` hook or create new prioritization system?

5. **Backend migration:** Is adding `thumb_width`/`thumb_height` columns acceptable, or should we ensure metadata extraction always runs?

---

## Next Steps

1. **Review this plan** - Confirm approach and answer open questions
2. **Prototype Phase 1** - IntersectionObserver-based LazyLoadedImage
3. **Prototype Phase 2** - Pre-calculated layout
4. **Test with real data** - Validate performance with 1000+ images
5. **Iterate** - Adjust based on real-world performance