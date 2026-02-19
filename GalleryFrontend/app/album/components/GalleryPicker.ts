import { ImageItemContent } from './AlbumHierarchyProps';

export type PickerMode = 'single_image' | 'multi_image' | 'folder' | null;

// galleryPicker state flows: BaseAlbumPage → BaseAlbumPageProps → VirtualAlbumPage/AlbumPage 
//                                          → VirtualAlbumHierarchyView/AlbumHierarchyView → BaseHierarchyView 
//                                          → VirtualizedGallery → GalleryItem

export interface GalleryPickerState {
  /** Current picker mode — null means inactive */
  mode: PickerMode;
  setMode: (mode: PickerMode) => void;
  /** Selected image paths (1 for single_image, N for multi_image) */
  selectedPaths: string[];
  setSelectedPaths: (paths: string[]) => void;
  /** Called when a gallery image radio button is clicked */
  onPick: (image: ImageItemContent) => void;
  /** HD thumbnail URL of the last picked image (for banner preview) */
  selectedHdPath: string | null;
  setSelectedHdPath: (path: string | null) => void;
  /** Server-side path separator (\ on Windows, / on Linux) */
  pathSep: string;
  setPathSep: (sep: string) => void;
}
