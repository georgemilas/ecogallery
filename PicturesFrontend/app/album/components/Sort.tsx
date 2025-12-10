import React from 'react';
import './sort.css';
import { AlbumItemHierarchy } from './Album';

type SortField = 'name' | 'timestamp';
type SortOrder = 'asc' | 'desc';

interface SortPanelProps {
  albums: AlbumItemHierarchy[];
  images: AlbumItemHierarchy[];
  getAlbumName: (item: AlbumItemHierarchy) => string;
  onSortedAlbumsChange: (sorted: AlbumItemHierarchy[]) => void;
  onSortedImagesChange: (sorted: AlbumItemHierarchy[]) => void;
}

export function SortPanel({ 
  albums,
  images,
  getAlbumName,
  onSortedAlbumsChange,
  onSortedImagesChange
}: SortPanelProps) {
  const [albumSortField, setAlbumSortField] = React.useState<SortField>('timestamp');
  const [albumSortOrder, setAlbumSortOrder] = React.useState<SortOrder>('desc');
  const [imageSortField, setImageSortField] = React.useState<SortField>('timestamp');
  const [imageSortOrder, setImageSortOrder] = React.useState<SortOrder>('desc');

  const sortItems = (items: AlbumItemHierarchy[], field: SortField, order: SortOrder, useAlbumName: boolean = false) => {
    return items.toSorted((a, b) => {
      let comparison = 0;
      if (field === 'name') {
        const nameA = useAlbumName ? getAlbumName(a) : a.name;
        const nameB = useAlbumName ? getAlbumName(b) : b.name;
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
    console.log('Sort: albums changed', albums.length);
    const sorted = sortItems(albums, albumSortField, albumSortOrder, true);
    console.log('Sort: sorted albums', sorted.length);
    onSortedAlbumsChange(sorted);
  }, [albums, albumSortField, albumSortOrder, getAlbumName, onSortedAlbumsChange]);

  React.useEffect(() => {
    console.log('Sort: images changed', images.length);
    const sorted = sortItems(images, imageSortField, imageSortOrder, false);
    console.log('Sort: sorted images', sorted.length);
    onSortedImagesChange(sorted);
  }, [images, imageSortField, imageSortOrder, onSortedImagesChange]);

  const handleAlbumSortChange = (field: SortField, order: SortOrder) => {
    setAlbumSortField(field);
    setAlbumSortOrder(order);
  };

  const handleImageSortChange = (field: SortField, order: SortOrder) => {
    setImageSortField(field);
    setImageSortOrder(order);
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
        <svg width="16" height="16" viewBox="0 0 24 24" fill="none"><path d="M12 4L4 12H9V20H15V12H20L12 4Z" fill="black" stroke='white'/></svg>
        <svg width="16" height="16" viewBox="0 0 24 24" fill="none"><path d="M12 20L4 12H9V4H15V12H20L12 20Z" fill="black" stroke='white'/></svg>
        <select 
          value={`${imageSortField}-${imageSortOrder}`}
          onChange={(e) => {
            const [field, order] = e.target.value.split('-') as [SortField, SortOrder];
            handleImageSortChange(field, order);
          }}>

          <option value="name-asc">Name <svg width="16" height="16" viewBox="0 0 24 24" fill="none">
                    <path d="M12 4L4 12H9V20H15V12H20L12 4Z" fill="black" stroke='white'/>
                    </svg></option>
          <option value="name-desc">Name (Z-A)</option>
          <option value="timestamp-asc">Date (Oldest)</option>
          <option value="timestamp-desc">Date (Newest)</option>
        </select>
      </div>
    </div>
  );
}

export type { SortField, SortOrder };
