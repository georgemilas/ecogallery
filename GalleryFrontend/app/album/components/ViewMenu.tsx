'use client';

import React, { useState, useEffect, useRef } from 'react';
import { PeopleMenu } from './PeopleMenu';
import { LocationsMenu } from './LocationsMenu';

interface ViewMenuProps {
  onPersonClick?: (personName: string) => void;
  onRandomClick: () => void;
  onRecentClick: () => void;
  showPeopleMenu?: boolean;
  showLocationsMenu?: boolean;
  onSearchByClusterId?: (clusterId: number) => void;
  onSearchByClusterName?: (name: string) => void;
}

export function ViewMenu({ onPersonClick, onRandomClick, onRecentClick, showPeopleMenu = true, showLocationsMenu = true, onSearchByClusterId, onSearchByClusterName }: ViewMenuProps) {
  const [isOpen, setIsOpen] = useState(false);
  const menuRef = useRef<HTMLDivElement>(null);
  const dropdownRef = useRef<HTMLDivElement>(null);
  const [dropdownStyle, setDropdownStyle] = useState<React.CSSProperties>({});

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
      const buttonRect = menuRef.current.getBoundingClientRect();
      const rect = dropdown.getBoundingClientRect();
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
        // If it still goes off the left edge, use fixed positioning
        if (buttonRect.right - rect.width < padding) {
          style.right = 'auto';
          style.left = `${padding - buttonRect.left}px`;
        }
      }

      // Calculate available height and adjust maxHeight
      const availableHeight = window.innerHeight - buttonRect.bottom - padding;
      if (availableHeight < 300) {
        style.maxHeight = `${Math.max(availableHeight, 150)}px`;
        style.overflowY = 'auto';
      }

      setDropdownStyle(style);
    }
  }, [isOpen]);

  return (
    <div ref={menuRef} style={{ position: 'relative', display: 'inline-block', verticalAlign: 'middle' }}>
      <button
        onClick={() => setIsOpen(!isOpen)}
        className="page-button"
        title="View Options"
      >
        View
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
            padding: '8px',
            zIndex: 2000,
            minWidth: '160px',
            boxShadow: '0 4px 12px rgba(0,0,0,0.5)',
            display: 'flex',
            flexDirection: 'column',
            gap: '4px',
          }}
        >
          {showPeopleMenu && <PeopleMenu onPersonClick={(name) => { onPersonClick?.(name); setIsOpen(false); }} />}
          {showLocationsMenu && onSearchByClusterId && onSearchByClusterName && (
            <LocationsMenu
              onSearchByClusterId={(id) => { onSearchByClusterId(id); setIsOpen(false); }}
              onSearchByClusterName={(name) => { onSearchByClusterName(name); setIsOpen(false); }}
            />
          )}
          <button
            className="page-button"
            onClick={() => { onRandomClick(); setIsOpen(false); }}
            title="Random Images"
          >
            Random
          </button>
          <button
            className="page-button"
            onClick={() => { onRecentClick(); setIsOpen(false); }}
            title="Recent Images"
          >
            Recent
          </button>
        </div>
      )}
    </div>
  );
}
