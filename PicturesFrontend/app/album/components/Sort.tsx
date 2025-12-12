import React from 'react';
import './sort.css';
import { ItemContent, AlbumItemContent, ImageItemContent, AlbumItemHierarchy } from './AlbumHierarchyProps';

type SortField = 'name' | 'timestamp';
type SortOrder = 'asc' | 'desc';

interface SortPanelProps {
  album: AlbumItemHierarchy;
  onSortedAlbumsChange: () => void;
  onSortedImagesChange: () => void;
  initialAlbumSort?: string;
  initialImageSort?: string;
  onSortChange?: (albumSort: string, imageSort: string) => void;
}

export function SortPanel({ 
  album,
  onSortedAlbumsChange,
  onSortedImagesChange,
  initialAlbumSort = 'timestamp-desc',
  initialImageSort = 'timestamp-desc',
  onSortChange
}: SortPanelProps) {
  const [albumSortField, setAlbumSortField] = React.useState<SortField>(() => {
    const [field] = (initialAlbumSort || 'timestamp-desc').split('-') as [SortField, SortOrder];
    return field;
  });
  const [albumSortOrder, setAlbumSortOrder] = React.useState<SortOrder>(() => {
    const [, order] = (initialAlbumSort || 'timestamp-desc').split('-') as [SortField, SortOrder];
    return order;
  });
  const [imageSortField, setImageSortField] = React.useState<SortField>(() => {
    const [field] = (initialImageSort || 'timestamp-desc').split('-') as [SortField, SortOrder];
    return field;
  });
  const [imageSortOrder, setImageSortOrder] = React.useState<SortOrder>(() => {
    const [, order] = (initialImageSort || 'timestamp-desc').split('-') as [SortField, SortOrder];
    return order;
  });

  const sortItems = (items: ItemContent[], field: SortField, order: SortOrder, useAlbumName: boolean = false) => {
    items.sort((a, b) => {
      let comparison = 0;
      if (field === 'name') {
        const nameA = useAlbumName ? album.get_name(a.name) : a.name;
        const nameB = useAlbumName ? album.get_name(b.name) : b.name;
        comparison = nameA.localeCompare(nameB);
      } else {
        const timeA = useAlbumName ? new Date(a.last_updated_utc).getTime() : new Date(a.item_timestamp_utc).getTime();
        const timeB = useAlbumName ? new Date(b.last_updated_utc).getTime() : new Date(b.item_timestamp_utc).getTime();
        comparison = timeA - timeB;
      }
      return order === 'asc' ? comparison : -comparison;
    });
  };

  React.useEffect(() => {
    if (album.albums && album.albums.length > 0) {
      sortItems(album.albums, albumSortField, albumSortOrder, true);
      onSortedAlbumsChange();
    }
  }, [albumSortField, albumSortOrder, onSortedAlbumsChange]);

  React.useEffect(() => {
    if (album.images && album.images.length > 0) {
      sortItems(album.images, imageSortField, imageSortOrder, false);
      onSortedImagesChange();
    }
  }, [imageSortField, imageSortOrder, onSortedImagesChange]);

  const handleAlbumSortChange = (field: SortField, order: SortOrder) => {
    setAlbumSortField(field);
    setAlbumSortOrder(order);
    onSortChange?.(`${field}-${order}`, `${imageSortField}-${imageSortOrder}`);
  };

  const handleImageSortChange = (field: SortField, order: SortOrder) => {
    setImageSortField(field);
    setImageSortOrder(order);
    onSortChange?.(`${albumSortField}-${albumSortOrder}`, `${field}-${order}`);
  };

  return (
    <div className="sort-panel">
      <div className="sort-controls">
        {/* Album sorting */}
        <span className="sort-label">Album:</span>
        <button 
          className={`sort-btn ${albumSortField === 'name' && albumSortOrder === 'asc' ? 'active' : ''}`}
          title="Sort albums by name A-Z"
          onClick={() => handleAlbumSortChange('name', 'asc')}>
          <svg viewBox="0 0 16 16" width="14" height="14">
            <path d="M8 4L8 12M8 4L5 7M8 4L11 7" stroke="currentColor" strokeWidth="2" fill="none" strokeLinecap="round" strokeLinejoin="round"/>
          </svg>
        </button>
        <button 
          className={`sort-btn ${albumSortField === 'name' && albumSortOrder === 'desc' ? 'active' : ''}`}
          title="Sort albums by name Z-A"
          onClick={() => handleAlbumSortChange('name', 'desc')}>
          <svg viewBox="0 0 16 16" width="14" height="14">
            <path d="M8 12L8 4M8 12L5 9M8 12L11 9" stroke="currentColor" strokeWidth="2" fill="none" strokeLinecap="round" strokeLinejoin="round"/>
          </svg>
        </button>
        <button 
          className={`sort-btn ${albumSortField === 'timestamp' && albumSortOrder === 'asc' ? 'active' : ''}`}
          title="Sort albums by date (oldest first)"
          onClick={() => handleAlbumSortChange('timestamp', 'asc')}>
          <svg viewBox="0 0 16 16" width="14" height="14">
            <text x="1" y="11" fontSize="10" fill="currentColor" fontWeight="bold">📅</text>
            <path d="M13 7L13 13M13 7L11 9M13 7L15 9" stroke="currentColor" strokeWidth="1.5" fill="none" strokeLinecap="round" strokeLinejoin="round"/>
          </svg>
        </button>
        <button 
          className={`sort-btn ${albumSortField === 'timestamp' && albumSortOrder === 'desc' ? 'active' : ''}`}
          title="Sort albums by date (newest first)"
          onClick={() => handleAlbumSortChange('timestamp', 'desc')}>
          <svg viewBox="0 0 16 16" width="14" height="14">
            <text x="1" y="11" fontSize="10" fill="currentColor" fontWeight="bold">📅</text>
            <path d="M13 13L13 7M13 13L11 11M13 13L15 11" stroke="currentColor" strokeWidth="1.5" fill="none" strokeLinecap="round" strokeLinejoin="round"/>
          </svg>
        </button>
      </div>  
      <div className="sort-controls">  
        {/* Image sorting */}
        <span className="sort-label">Images:</span>
        <button 
          className={`sort-btn ${imageSortField === 'name' && imageSortOrder === 'asc' ? 'active' : ''}`}
          title="Sort images by name A-Z"
          onClick={() => handleImageSortChange('name', 'asc')}>
          <svg viewBox="0 0 16 16" width="14" height="14">
            <path d="M8 4L8 12M8 4L5 7M8 4L11 7" stroke="currentColor" strokeWidth="2" fill="none" strokeLinecap="round" strokeLinejoin="round"/>
          </svg>
        </button>
        <button 
          className={`sort-btn ${imageSortField === 'name' && imageSortOrder === 'desc' ? 'active' : ''}`}
          title="Sort images by name Z-A"
          onClick={() => handleImageSortChange('name', 'desc')}>
          <svg viewBox="0 0 16 16" width="14" height="14">
            <path d="M8 12L8 4M8 12L5 9M8 12L11 9" stroke="currentColor" strokeWidth="2" fill="none" strokeLinecap="round" strokeLinejoin="round"/>
          </svg>
        </button>
        <button 
          className={`sort-btn ${imageSortField === 'timestamp' && imageSortOrder === 'asc' ? 'active' : ''}`}
          title="Sort images by date (oldest first)"
          onClick={() => handleImageSortChange('timestamp', 'asc')}>
          <svg viewBox="0 0 16 16" width="14" height="14">
            <text x="1" y="11" fontSize="10" fill="currentColor" fontWeight="bold">📅</text>
            <path d="M13 7L13 13M13 7L11 9M13 7L15 9" stroke="currentColor" strokeWidth="1.5" fill="none" strokeLinecap="round" strokeLinejoin="round"/>
          </svg>
        </button>
        <button 
          className={`sort-btn ${imageSortField === 'timestamp' && imageSortOrder === 'desc' ? 'active' : ''}`}
          title="Sort images by date (newest first)"
          onClick={() => handleImageSortChange('timestamp', 'desc')}>
          <svg viewBox="0 0 16 16" width="14" height="14">
            <text x="1" y="11" fontSize="10" fill="currentColor" fontWeight="bold">📅</text>
            <path d="M13 13L13 7M13 13L11 11M13 13L15 11" stroke="currentColor" strokeWidth="1.5" fill="none" strokeLinecap="round" strokeLinejoin="round"/>
          </svg>
        </button>
      </div>
    </div>
  );
}

export type { SortField, SortOrder };
