import React, { useState, useRef, useEffect } from 'react';
import { ImageLocationCluster, LocationHandlers } from './AlbumHierarchyProps';
import { apiFetch } from '@/app/utils/apiFetch';
import { useAuth } from '@/app/contexts/AuthContext';

interface LocationContextMenuProps {
  clusters: ImageLocationCluster[];
  position: { x: number; y: number };
  onClose: () => void;
  locationHandlers: LocationHandlers;
}

export function LocationContextMenu({ clusters, position, onClose, locationHandlers }: LocationContextMenuProps) {
  const { user } = useAuth();
  const menuRef = useRef<HTMLDivElement>(null);
  const [adjustedPosition, setAdjustedPosition] = useState(position);
  const [names, setNames] = useState<Record<number, string>>(() => {
    const initial: Record<number, string> = {};
    clusters.forEach(c => {
      initial[c.cluster_id] = locationHandlers.clusterNames[c.cluster_id] ?? c.name ?? '';
    });
    return initial;
  });
  const [savingId, setSavingId] = useState<number | null>(null);

  // Sort clusters by tier_meters ascending (smallest tier first)
  const sortedClusters = [...clusters].sort((a, b) => a.tier_meters - b.tier_meters);

  // Adjust position to keep menu within viewport
  useEffect(() => {
    if (menuRef.current) {
      const rect = menuRef.current.getBoundingClientRect();
      const padding = 10;
      let newX = position.x;
      let newY = position.y;

      if (position.x + rect.width > window.innerWidth - padding) {
        newX = window.innerWidth - rect.width - padding;
      }
      if (newX < padding) newX = padding;
      if (position.y + rect.height > window.innerHeight - padding) {
        newY = window.innerHeight - rect.height - padding;
      }
      if (newY < padding) newY = padding;

      if (newX !== position.x || newY !== position.y) {
        setAdjustedPosition({ x: newX, y: newY });
      }
    }
  }, [position]);

  // Close on click outside
  useEffect(() => {
    const handleClickOutside = (e: MouseEvent) => {
      if (menuRef.current && !menuRef.current.contains(e.target as Node)) {
        onClose();
      }
    };
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, [onClose]);

  const handleSave = async (cluster: ImageLocationCluster) => {
    setSavingId(cluster.cluster_id);
    try {
      const newName = names[cluster.cluster_id] || null;
      const response = await apiFetch(`/api/v1/locations/cluster/${cluster.cluster_id}/name`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ name: newName }),
      });
      if (response.ok) {
        locationHandlers.onClusterNameUpdate(cluster.cluster_id, names[cluster.cluster_id]);
      }
    } catch (e) {
      console.error('Failed to update cluster name:', e);
    } finally {
      setSavingId(null);
    }
  };

  const handleSearchByClusterId = (clusterId: number) => {
    if (locationHandlers.onSearchByClusterId) {
      locationHandlers.onSearchByClusterId(clusterId);
      onClose();
    }
  };

  const handleSearchByClusterName = (name: string) => {
    if (name.trim() && locationHandlers.onSearchByClusterName) {
      locationHandlers.onSearchByClusterName(name.trim());
      onClose();
    }
  };

  return (
    <div
      ref={menuRef}
      style={{
        position: 'fixed',
        left: adjustedPosition.x,
        top: adjustedPosition.y,
        backgroundColor: '#2a2a2a',
        border: '1px solid #e8f09e',
        borderRadius: '8px',
        padding: '12px',
        zIndex: 3000,
        minWidth: '240px',
        maxWidth: '320px',
        boxShadow: '0 4px 12px rgba(0,0,0,0.5)',
      }}
      onClick={(e) => e.stopPropagation()}
    >
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '12px' }}>
        <div style={{ color: '#e8f09e', fontWeight: 'bold', fontSize: '12px' }}>
          Location Clusters
        </div>
        <button
          onClick={onClose}
          style={{
            background: 'none',
            border: 'none',
            color: '#888',
            cursor: 'pointer',
            padding: '0',
            lineHeight: '1',
          }}
          title="Close"
        >
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
            <path d="M18 6L6 18M6 6l12 12" />
          </svg>
        </button>
      </div>

      {sortedClusters.length > 0 && (() => {
        const lat = sortedClusters[0].centroid_latitude;
        const lng = sortedClusters[0].centroid_longitude;
        return (
          <div style={{ marginBottom: '10px' }}>
            <iframe
              width="100%"
              height="150"
              style={{ border: 0, borderRadius: '4px' }}
              loading="lazy"
              referrerPolicy="no-referrer-when-downgrade"
              src={`https://maps.google.com/maps?q=${lat},${lng}&z=14&output=embed`}
            />
            <a
              href={`https://www.google.com/maps?q=${lat},${lng}`}
              target="_blank"
              rel="noopener noreferrer"
              style={{ color: '#e8f09e', fontSize: '11px', textDecoration: 'none' }}
            >
              Open in Google Maps
            </a>
          </div>
        );
      })()}

      {sortedClusters.map((cluster) => (
        <div
          key={cluster.cluster_id}
          style={{
            marginBottom: '10px',
            paddingBottom: '10px',
            borderBottom: '1px solid #444',
          }}
        >
          <div style={{ display: 'flex', alignItems: 'center', marginBottom: '6px' }}>
            <span style={{ color: '#e8f09e', fontSize: '11px', fontWeight: 'bold' }}>
              {cluster.tier_name}
            </span>
            <span style={{ color: '#666', fontSize: '10px', marginLeft: '6px' }}>
              #{cluster.cluster_id}
            </span>
            {locationHandlers.onSearchByClusterId && (
              <button
                onClick={() => handleSearchByClusterId(cluster.cluster_id)}
                style={{
                  background: 'none',
                  border: 'none',
                  color: '#888',
                  cursor: 'pointer',
                  padding: '2px 4px',
                }}
                title="Search by Cluster ID"
              >
                <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                  <circle cx="10" cy="10" r="6" />
                  <line x1="16" y1="16" x2="21" y2="21" />
                </svg>
              </button>
            )}
          </div>

          {user?.is_admin ? (
            <div style={{ display: 'flex', gap: '4px' }}>
              <input
                type="text"
                value={names[cluster.cluster_id] ?? ''}
                onChange={(e) => setNames(prev => ({ ...prev, [cluster.cluster_id]: e.target.value }))}
                placeholder="Enter name..."
                style={{
                  flex: 1,
                  padding: '4px 6px',
                  backgroundColor: '#1a1a1a',
                  border: '1px solid #555',
                  borderRadius: '4px',
                  color: 'white',
                  fontSize: '11px',
                }}
                onKeyDown={(e) => {
                  if (e.key === 'Enter') handleSave(cluster);
                  if (e.key === 'Escape') onClose();
                }}
              />
              {locationHandlers.onSearchByClusterName && (
                <button
                  onClick={() => handleSearchByClusterName(names[cluster.cluster_id] ?? '')}
                  disabled={!(names[cluster.cluster_id] ?? '').trim()}
                  style={{
                    background: 'none',
                    border: '1px solid #555',
                    borderRadius: '4px',
                    color: (names[cluster.cluster_id] ?? '').trim() ? '#e8f09e' : '#555',
                    cursor: (names[cluster.cluster_id] ?? '').trim() ? 'pointer' : 'not-allowed',
                    padding: '2px 4px',
                  }}
                  title="Search by Name"
                >
                  <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                    <circle cx="10" cy="10" r="6" />
                    <line x1="16" y1="16" x2="21" y2="21" />
                  </svg>
                </button>
              )}
              <button
                onClick={() => handleSave(cluster)}
                disabled={savingId === cluster.cluster_id}
                style={{
                  padding: '2px 8px',
                  backgroundColor: '#4CAF50',
                  color: 'white',
                  border: 'none',
                  borderRadius: '4px',
                  cursor: savingId === cluster.cluster_id ? 'not-allowed' : 'pointer',
                  opacity: savingId === cluster.cluster_id ? 0.5 : 1,
                  fontSize: '11px',
                }}
              >
                {savingId === cluster.cluster_id ? '...' : 'Save'}
              </button>
            </div>
          ) : (
            <div style={{ display: 'flex', alignItems: 'center', gap: '4px' }}>
              <span style={{ color: '#ddd', fontSize: '12px' }}>
                {locationHandlers.clusterNames[cluster.cluster_id] ?? cluster.name ?? 'Unnamed'}
              </span>
              {locationHandlers.onSearchByClusterName && (cluster.name || locationHandlers.clusterNames[cluster.cluster_id]) && (
                <button
                  onClick={() => handleSearchByClusterName(locationHandlers.clusterNames[cluster.cluster_id] ?? cluster.name ?? '')}
                  style={{
                    background: 'none',
                    border: 'none',
                    color: '#e8f09e',
                    cursor: 'pointer',
                    padding: '2px 4px',
                  }}
                  title="Search by Name"
                >
                  <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                    <circle cx="10" cy="10" r="6" />
                    <line x1="16" y1="16" x2="21" y2="21" />
                  </svg>
                </button>
              )}
            </div>
          )}
        </div>
      ))}
    </div>
  );
}
