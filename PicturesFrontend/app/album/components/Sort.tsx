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
      <div className="sort-group">
        <label>Albums:</label>
        <select 
          value={`${albumSortField}-${albumSortOrder}`} 
          onChange={(e) => {
            const [field, order] = e.target.value.split('-') as [SortField, SortOrder];
            handleAlbumSortChange(field, order);
          }}>
          <option value="name-asc">Name (A-Z)</option>
          <option value="name-desc">Name (Z-A)</option>
          <option value="timestamp-asc">Date (Oldest)</option>
          <option value="timestamp-desc">Date (Newest)</option>
        </select>
      </div>
      <div className="sort-group">
        <label>Images:</label>
        <select 
          value={`${imageSortField}-${imageSortOrder}`}
          onChange={(e) => {
            const [field, order] = e.target.value.split('-') as [SortField, SortOrder];
            handleImageSortChange(field, order);
          }}>

          <option value="name-asc">Name (A-Z)</option>
          <option value="name-desc">Name (Z-A)</option>
          <option value="timestamp-asc">Date (Oldest)</option>
          <option value="timestamp-desc">Date (Newest)</option>
        </select>
      </div>
    </div>
  );
}

export type { SortField, SortOrder };
