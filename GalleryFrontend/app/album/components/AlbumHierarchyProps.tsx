import { AppRouterInstance } from "next/dist/shared/lib/app-router-context.shared-runtime";

export interface ImageMetadata extends ItemMetadata {
  camera: string | null;
  lens: string | null;
  focal_length: string | null;
  aperture: string | null;
  exposure_time: string | null;
  iso: number | null;
  rating: number | null;
  flash: string | null;
  metering_mode: string | null;
  exposure_program: string | null;
  exposure_bias: string | null;
  exposure_mode: string | null;
  white_balance: string | null;
  color_space: string | null;
  scene_capture_type: string | null;
  circle_of_confusion: number | null;
  field_of_view: number | null;
  depth_of_field: number | null;
  hyperfocal_distance: number | null;
  normalized_light_value: number | null;
  software: string | null;
  serial_number: string | null;
  lens_serial_number: string | null;
  image_width: number | null;
  image_height: number | null;
  orientation: number | null;
}

export interface VideoMetadata extends ItemMetadata {
  duration: string | null;              // ISO 8601 duration string (e.g., "PT1M20S" for 1:20)
  video_width: number | null;
  video_height: number | null;
  video_codec: string | null;
  audio_codec: string | null;
  pixel_format: string | null;
  frame_rate: number | null;
  video_bit_rate: number | null;
  audio_sample_rate: number | null;
  audio_channels: number | null;
  audio_bit_rate: number | null;
  format_name: string | null;
  software: string | null;
  camera: string | null;
  rotation: number | null;  
}

export interface ItemMetadata {
  id: number;
  album_image_id: number;
  file_name: string;
  file_path: string;
  file_size_bytes: number | null;
  date_taken: string | null;
  date_modified: string | null;
  last_updated_utc: string;
}


export interface AlbumPathElement {
  name: string;
  id: number;
}

export interface ItemContent {
  id: number;
  name: string;
  description?: string;
  navigation_path_segments: Array<AlbumPathElement>;
  thumbnail_path: string;
  last_updated_utc: Date;
  item_timestamp_utc: Date;
}

export interface FaceBox {
  face_id: number;
  person_id: number | null;
  person_name: string | null;
  album_image_id: number;
  bounding_box_x: number;
  bounding_box_y: number;
  bounding_box_width: number;
  bounding_box_height: number;
  confidence: number;
}

export interface ImageItemContent extends ItemContent {
  is_movie: boolean;
  image_hd_path: string;
  image_uhd_path: string;
  image_original_path: string;
  image_width: number;
  image_height: number;
  image_metadata: ImageMetadata | null;
  video_metadata: VideoMetadata | null;
  faces?: FaceBox[];
}

export interface AlbumItemContent extends ItemContent {
  image_hd_path: string;
  settings: AlbumSettings | null;
}

export interface AlbumSettings {
  album_id: number;
  search_id?: string;          // hash of search expression (for search result preferences)
  user_id: number;
  banner_position_y: number;   // Y position of the banner image (0-100%)
  album_sort: string;          // name or timestamp & asc or desc
  image_sort: string;          // name or timestamp & asc or desc
}

export interface SearchInfo {
  expression: string;
  limit: number;
  offset: number;
  count: number;
  group_by_p_hash: boolean;
}

export class AlbumItemHierarchy implements AlbumItemContent {
  id: number = 0;
  name: string = '';
  description?: string;   // optional in AlbumItemContent interface therefor we must add it here
  navigation_path_segments: Array<AlbumPathElement> = [];
  thumbnail_path: string = '';
  image_hd_path: string = '';
  banner_position_y?: number; // Y position of the banner image (0-100%)
  last_updated_utc: Date = new Date();
  item_timestamp_utc: Date = new Date();
  albums: AlbumItemContent[] = [];
  images: ImageItemContent[] = [];
  settings!: AlbumSettings;
  search_info: SearchInfo | null = null;

  /**
   * Get the name out of a path, ex: \2025\vacation\Florida => Florida
   * @param path The path from which to extract the name
   * @returns The name extracted from the path
   */
  get_name(path: string): string {
    // Split on both '/' and '\' regardless of which is present
    const parts = path.split(/[/\\]+/);
    return parts.pop() || 'Pictures Gallery';
  }

  /**
   * album.name is the entire path, this returns the name portion, ex: album.name==\2025\vacation\Florida => Florida 
   * @returns The actual name of the album
   */
  album_name(): string {
    return this.get_name(this.name);
  }
}

export interface AlbumHierarchyProps {
  album: AlbumItemHierarchy;
  onAlbumClick: (albumId: number | null) => void;
  onImageClick: (image: ImageItemContent) => void;
  onSearchSubmit: (expression: string, offset: number) => void;
  onGetApiUrl: (apiUrl: string) => void;
  lastViewedImage?: number | null;
  settings: AlbumSettings;
  router: AppRouterInstance;
  onSortChange?: (settings: AlbumSettings) => void;
  clearLastViewedImage?: () => void;
  onFaceSearch?: (personId: number, personName: string | null) => void;
  onFaceDelete?: (faceId: number) => void;
  onPersonDelete?: (personId: number) => void;
  onSearchByName?: (name: string) => void;
  onSearchByPersonId?: (personId: number) => void;
  onSortedImagesChange?: (images: ImageItemContent[]) => void;
  searchEditor?: {
    isOpen: boolean;
    setIsOpen: (open: boolean) => void;
    text: string;
    setText: (text: string) => void;
    error: string | null;
    clearError: () => void;
    panelWidth: number;
    setPanelWidth: (width: number) => void;
  };
}


