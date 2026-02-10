'use client';

import React, { useState, useEffect, useRef } from 'react';
import { apiFetch } from '@/app/utils/apiFetch';

interface LocationClusterSummary {
  representative_cluster_id: number;
  name: string | null;
  image_count: number;
}

interface LocationsMenuProps {
  onSearchByClusterId: (clusterId: number) => void;
  onSearchByClusterName: (name: string) => void;
}

const TIER_OPTIONS = [
  { label: 'Location', tierMeters: 300 },
  { label: 'Neighborhood', tierMeters: 2000 },
  { label: 'Area', tierMeters: 25000 },
];

export function LocationsMenu({ onSearchByClusterId, onSearchByClusterName }: LocationsMenuProps) {
  const [isOpen, setIsOpen] = useState(false);
  const [locations, setLocations] = useState<LocationClusterSummary[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [selectedTier, setSelectedTier] = useState(300);
  const menuRef = useRef<HTMLDivElement>(null);
  const dropdownRef = useRef<HTMLDivElement>(null);
  const [dropdownStyle, setDropdownStyle] = useState<React.CSSProperties>({});

  const fetchLocations = async (tierMeters: number) => {
    setLoading(true);
    setError(null);
    try {
      const response = await apiFetch(`/api/v1/locations/clusters/top?tierMeters=${tierMeters}&limit=50`);
      if (!response.ok) {
        throw new Error('Failed to fetch locations');
      }
      const data = await response.json();
      setLocations(data);
    } catch (e) {
      console.error('Error fetching locations:', e);
      setError('Failed to load locations');
    } finally {
      setLoading(false);
    }
  };

  // Fetch locations when dropdown opens or tier changes
  useEffect(() => {
    if (isOpen) {
      fetchLocations(selectedTier);
    }
  }, [isOpen, selectedTier]);

  // Close dropdown when clicking outside
  useEffect(() => {
    const handleClickOutside = (e: MouseEvent) => {
      if (menuRef.current && !menuRef.current.contains(e.target as Node)) {
        setIsOpen(false);
      }
    };
    if (isOpen) {
      document.addEventListener('mousedown', handleClickOutside);
    }
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, [isOpen]);

  // Adjust dropdown position to keep within viewport
  useEffect(() => {
    if (isOpen && dropdownRef.current && menuRef.current) {
      const dropdown = dropdownRef.current;
      const button = menuRef.current;
      const rect = dropdown.getBoundingClientRect();
      const buttonRect = button.getBoundingClientRect();
      const padding = 10;

      const style: React.CSSProperties = {
        position: 'absolute',
        top: '100%',
        left: '0',
      };

      // Adjust horizontal position if menu goes off right edge
      if (buttonRect.left + rect.width > window.innerWidth - padding) {
        style.left = 'auto';
        style.right = '0';
      }

      // Calculate available height and adjust maxHeight
      const availableHeight = window.innerHeight - buttonRect.bottom - padding;
      if (availableHeight < 400) {
        style.maxHeight = `${Math.max(availableHeight, 150)}px`;
      }

      setDropdownStyle(style);
    }
  }, [isOpen, locations]);

  const handleLocationClick = (location: LocationClusterSummary) => {
    if (location.name) {
      onSearchByClusterName(location.name);
    } else {
      onSearchByClusterId(location.representative_cluster_id);
    }
    setIsOpen(false);
  };

  return (
    <div ref={menuRef} style={{ position: 'relative' }}>
      <button
        onClick={() => setIsOpen(!isOpen)}
        className="page-button"
        style={{ width: '100%' }}
        title="Browse Locations"
      >
        <svg width="16" height="16" viewBox="0 0 16 16" fill="currentColor" style={{verticalAlign: 'middle', marginTop: '0px', marginLeft: '2px', marginBottom: '4px', marginRight: '4px'}}>
          <path d="M8 1C5.2 1 3 3.2 3 6c0 4 5 9 5 9s5-5 5-9c0-2.8-2.2-5-5-5zm0 7a2 2 0 1 1 0-4 2 2 0 0 1 0 4z"/>
        </svg>
        Locations
        <svg width="10" height="10" viewBox="0 0 10 10" fill="currentColor" style={{marginLeft: '4px', verticalAlign: 'middle'}}>
          <path d="M2 3l3 4 3-4"/>
        </svg>
      </button>

      {isOpen && (
        <div
          ref={dropdownRef}
          style={{
            position: 'absolute',
            top: '100%',
            left: '0',
            ...dropdownStyle,
            backgroundColor: '#2a2a2a',
            border: '1px solid #e8f09e',
            borderRadius: '8px',
            padding: '12px',
            zIndex: 2000,
            minWidth: '280px',
            maxWidth: '360px',
            maxHeight: '400px',
            overflowY: 'auto',
            boxShadow: '0 4px 12px rgba(0,0,0,0.5)',
          }}
        >
          <div style={{ marginBottom: '8px', borderBottom: '1px solid #444', paddingBottom: '8px' }}>
            <select
              value={selectedTier}
              onChange={(e) => setSelectedTier(Number(e.target.value))}
              style={{
                width: '100%',
                padding: '4px 8px',
                backgroundColor: '#333',
                color: '#e8f09e',
                border: '1px solid #555',
                borderRadius: '4px',
                fontSize: '13px',
              }}
            >
              {TIER_OPTIONS.map((opt) => (
                <option key={opt.tierMeters} value={opt.tierMeters}>
                  {opt.label}
                </option>
              ))}
            </select>
          </div>

          {loading && (
            <div style={{ color: '#888', textAlign: 'center', padding: '20px' }}>
              Loading...
            </div>
          )}

          {error && (
            <div style={{ color: '#ff6b6b', textAlign: 'center', padding: '20px' }}>
              {error}
            </div>
          )}

          {!loading && !error && locations.length === 0 && (
            <div style={{ color: '#888', textAlign: 'center', padding: '20px' }}>
              No locations found
            </div>
          )}

          {!loading && !error && locations.length > 0 && (
            <div style={{ display: 'flex', flexDirection: 'column', gap: '2px' }}>
              {locations.map((location) => (
                <button
                  key={location.name ?? `id-${location.representative_cluster_id}`}
                  onClick={() => handleLocationClick(location)}
                  style={{
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'space-between',
                    gap: '8px',
                    padding: '6px 8px',
                    backgroundColor: 'transparent',
                    border: '1px solid transparent',
                    borderRadius: '4px',
                    color: '#ddd',
                    cursor: 'pointer',
                    textAlign: 'left',
                    fontSize: '13px',
                    transition: 'background-color 0.15s, border-color 0.15s',
                  }}
                  onMouseEnter={(e) => {
                    e.currentTarget.style.backgroundColor = '#3a3a3a';
                    e.currentTarget.style.borderColor = '#555';
                  }}
                  onMouseLeave={(e) => {
                    e.currentTarget.style.backgroundColor = 'transparent';
                    e.currentTarget.style.borderColor = 'transparent';
                  }}
                  title={`${location.name ?? `#${location.representative_cluster_id}`} (${location.image_count} photos)`}
                >
                  <span style={{
                    overflow: 'hidden',
                    textOverflow: 'ellipsis',
                    whiteSpace: 'nowrap',
                    flex: 1,
                  }}>
                    {location.name ?? `#${location.representative_cluster_id}`}
                  </span>
                  <span style={{ color: '#888', fontSize: '11px', flexShrink: 0 }}>
                    {location.image_count}
                  </span>
                </button>
              ))}
            </div>
          )}
        </div>
      )}
    </div>
  );
}
