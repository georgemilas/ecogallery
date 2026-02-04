'use client';

import React, { useState, useEffect, useRef } from 'react';
import { PeopleMenu } from './PeopleMenu';

interface ViewMenuProps {
  onPersonClick?: (personName: string) => void;
  onRandomClick: () => void;
  onRecentClick: () => void;
  showPeopleMenu?: boolean;
}

export function ViewMenu({ onPersonClick, onRandomClick, onRecentClick, showPeopleMenu = true }: ViewMenuProps) {
  const [isOpen, setIsOpen] = useState(false);
  const menuRef = useRef<HTMLDivElement>(null);

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
          style={{
            position: 'absolute',
            top: '100%',
            left: '0',
            backgroundColor: '#2a2a2a',
            border: '1px solid #e8f09e',
            borderRadius: '8px',
            padding: '8px',
            zIndex: 2000,
            minWidth: '120px',
            boxShadow: '0 4px 12px rgba(0,0,0,0.5)',
            display: 'flex',
            flexDirection: 'column',
            gap: '4px',
          }}
        >
          {showPeopleMenu && <PeopleMenu onPersonClick={(name) => { onPersonClick?.(name); setIsOpen(false); }} />}
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