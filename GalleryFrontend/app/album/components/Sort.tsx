import React from 'react';
import './sort.css';
import { ItemContent, AlbumItemHierarchy } from './AlbumHierarchyProps';

type SortField = 'name' | 'timestamp';
type SortOrder = 'asc' | 'desc';
type SortType = 'albums' | 'images';

interface SortControlProps {
  type: SortType;
  album: AlbumItemHierarchy;
  onSortChange: () => void;
  initialSort?: string;
  onSortUpdate?: (sortValue: string) => void;
}

export function SortControl(props: SortControlProps): JSX.Element {
  const [sortField, setSortField] = React.useState<SortField>(() => {
    const [field] = (props.initialSort || 'timestamp-desc').split('-') as [SortField, SortOrder];
    return field;
  });
  const [sortOrder, setSortOrder] = React.useState<SortOrder>(() => {
    const [, order] = (props.initialSort || 'timestamp-desc').split('-') as [SortField, SortOrder];
    return order;
  });
  console.log('SortControl initialized with', { initialSort: props.initialSort, sortField, sortOrder });

  // Reset sort state if initialSort changes (e.g., after loading from server)
  React.useEffect(() => {
    if (props.initialSort) {
      const [field, order] = props.initialSort.split('-') as [SortField, SortOrder];
      setSortField(field);
      setSortOrder(order);
      console.log('SortControl now loaded with', { sortField, sortOrder });
    }
  }, [props.initialSort]);

  const isAlbumType = props.type === 'albums';
  const items = isAlbumType ? props.album.albums : props.album.images;

  const sortItems = (items: ItemContent[], field: SortField, order: SortOrder) => {
    items.sort((a, b) => {
      let comparison = 0;
      if (field === 'name') {
        const nameA = isAlbumType ? props.album.get_name(a.name) : a.name;
        const nameB = isAlbumType ? props.album.get_name(b.name) : b.name;
        comparison = nameA.localeCompare(nameB);
      } else {
        const timeA = isAlbumType ? new Date(a.last_updated_utc).getTime() : new Date(a.item_timestamp_utc).getTime();
        const timeB = isAlbumType ? new Date(b.last_updated_utc).getTime() : new Date(b.item_timestamp_utc).getTime();
        comparison = timeA - timeB;
      }
      return order === 'asc' ? comparison : -comparison;
    });
  };

  React.useEffect(() => {
    if (items && items.length > 0) {
      sortItems(items, sortField, sortOrder);
      props.onSortChange();
    }
  }, [sortField, sortOrder, props.onSortChange]);

  const handleSortChange = (field: SortField, order: SortOrder) => {
    setSortField(field);
    setSortOrder(order);
    props.onSortUpdate?.(`${field}-${order}`);
  };

  const label = props.type === 'albums' ? 'Albums' : 'Images';

  return (
    <div className="sort-control">
      
      <button 
        className={`sort-btn ${sortField === 'name' && sortOrder === 'asc' ? 'active' : ''}`}
        title={`Sort ${props.type} by name A-Z`}
        onClick={() => handleSortChange('name', 'asc')}>
        <svg viewBox="0 0 16 16" width="14" height="14">
          <path d="M8 4L8 12M8 4L5 7M8 4L11 7" stroke="currentColor" strokeWidth="2" fill="none" strokeLinecap="round" strokeLinejoin="round"/>
        </svg>
      </button>
      <button 
        className={`sort-btn ${sortField === 'name' && sortOrder === 'desc' ? 'active' : ''}`}
        title={`Sort ${props.type} by name Z-A`}
        onClick={() => handleSortChange('name', 'desc')}>
        <svg viewBox="0 0 16 16" width="14" height="14">
          <path d="M8 12L8 4M8 12L5 9M8 12L11 9" stroke="currentColor" strokeWidth="2" fill="none" strokeLinecap="round" strokeLinejoin="round"/>
        </svg>
      </button>
      <button 
        className={`sort-btn ${sortField === 'timestamp' && sortOrder === 'asc' ? 'active' : ''}`}
        title={`Sort ${props.type} by date (oldest first)`}
        onClick={() => handleSortChange('timestamp', 'asc')}>
        <svg viewBox="0 0 16 16" width="14" height="14">
          <text x="1" y="11" fontSize="10" fill="currentColor" fontWeight="bold">📅</text>
          <path d="M13 6L13 13M13 6L10 9M13 6L16 9" stroke="currentColor" strokeWidth="1.5" fill="none" strokeLinecap="round" strokeLinejoin="round"/>
        </svg>
      </button>
      <button 
        className={`sort-btn ${sortField === 'timestamp' && sortOrder === 'desc' ? 'active' : ''}`}
        title={`Sort ${props.type} by date (newest first)`}
        onClick={() => handleSortChange('timestamp', 'desc')}>
        <svg viewBox="0 0 16 16" width="14" height="14">
          <text x="1" y="11" fontSize="10" fill="currentColor" fontWeight="bold">📅</text>
          <path d="M13 13L13 6M13 13L10 10M13 13L16 10" stroke="currentColor" strokeWidth="1.5" fill="none" strokeLinecap="round" strokeLinejoin="round"/>
        </svg>
      </button>
    </div>
  );
}

export type { SortField, SortOrder, SortType };