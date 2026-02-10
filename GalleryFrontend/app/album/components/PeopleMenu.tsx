'use client';

import React, { useState, useEffect, useRef, useCallback } from 'react';
import { apiFetch } from '@/app/utils/apiFetch';
import { useGallerySettings } from '@/app/contexts/GallerySettingsContext';

interface PersonWithImageCount {
  name: string;
  image_count: number;
  thumbnail_path: string | null;
  image_width: number | null;
  image_height: number | null;
  bounding_box_x: number | null;
  bounding_box_y: number | null;
  bounding_box_width: number | null;
  bounding_box_height: number | null;
}

// Calculate CSS styles to crop and position the image to show just the face
function getFaceCropStyles(person: PersonWithImageCount, containerSize: number): React.CSSProperties {
  // If we don't have bounding box data, fall back to simple cover
  if (
    person.bounding_box_x == null ||
    person.bounding_box_y == null ||
    person.bounding_box_width == null ||
    person.bounding_box_height == null ||
    person.image_width == null ||
    person.image_height == null
  ) {
    return {
      width: `${containerSize}px`,
      height: `${containerSize}px`,
      objectFit: 'cover',
    };
  }

  // Image aspect ratio
  const imageAspect = person.image_width / person.image_height;

  // Face bounding box is in pixel coordinates - convert to normalized (0-1)
  const faceX = person.bounding_box_x / person.image_width;
  const faceY = person.bounding_box_y / person.image_height;
  const faceW = person.bounding_box_width / person.image_width;
  const faceH = person.bounding_box_height / person.image_height;

  // Add some padding around the face (20%)
  const padding = 0.2;
  const paddedFaceW = faceW * (1 + padding);
  const paddedFaceH = faceH * (1 + padding);

  // Convert both dimensions to a common unit (fraction of image width)
  // faceW is already relative to image width
  // faceH needs to be converted: faceH_as_width = faceH / imageAspect
  const faceWNorm = paddedFaceW;
  const faceHNorm = paddedFaceH / imageAspect;

  // Use the larger normalized dimension to ensure the face fits in a square container
  const faceSize = Math.max(faceWNorm, faceHNorm);

  // Calculate display dimensions
  // displayWidth = containerSize / faceSize makes the face portion equal to containerSize
  const displayWidth = containerSize / faceSize;
  const displayHeight = displayWidth / imageAspect;

  // Calculate face center in normalized coordinates
  const faceCenterX = faceX + faceW / 2;
  const faceCenterY = faceY + faceH / 2;

  // Calculate face center in display pixels
  const faceCenterXPx = faceCenterX * displayWidth;
  const faceCenterYPx = faceCenterY * displayHeight;

  // Calculate offset to center the face in the container
  const offsetX = (containerSize / 2) - faceCenterXPx;
  const offsetY = (containerSize / 2) - faceCenterYPx;

  return {
    width: `${displayWidth}px`,
    height: `${displayHeight}px`,
    transform: `translate(${offsetX}px, ${offsetY}px)`,
  };
}

interface PeopleMenuProps {
  onPersonClick: (personName: string) => void;
}

export function PeopleMenu({ onPersonClick }: PeopleMenuProps) {
  const [isOpen, setIsOpen] = useState(false);
  const [people, setPeople] = useState<PersonWithImageCount[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const menuRef = useRef<HTMLDivElement>(null);
  const dropdownRef = useRef<HTMLDivElement>(null);
  const [dropdownStyle, setDropdownStyle] = useState<React.CSSProperties>({});
  const { settings: gallerySettings } = useGallerySettings();

  const fetchPeople = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const response = await apiFetch(`/api/v1/faces/persons/top?limit=${gallerySettings.peopleMenuLimit}`);
      if (!response.ok) {
        throw new Error('Failed to fetch people');
      }
      const data = await response.json();
      setPeople(data);
    } catch (e) {
      console.error('Error fetching people:', e);
      setError('Failed to load people');
    } finally {
      setLoading(false);
    }
  }, [gallerySettings.peopleMenuLimit]);

  // Fetch people when dropdown opens
  useEffect(() => {
    if (isOpen) {
      fetchPeople();
    }
  }, [isOpen, fetchPeople]);

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

  // Adjust dropdown position and size to keep it within viewport
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

      // Calculate available height and adjust maxHeight instead of repositioning
      const availableHeight = window.innerHeight - buttonRect.bottom - padding;
      if (availableHeight < 400) {
        style.maxHeight = `${Math.max(availableHeight, 150)}px`;
      }

      setDropdownStyle(style);
    }
  }, [isOpen, people]);

  const handlePersonClick = (person: PersonWithImageCount) => {
    onPersonClick(person.name);
    setIsOpen(false);
  };

  return (
    <div ref={menuRef} style={{ position: 'relative' }}>
      <button
        onClick={() => setIsOpen(!isOpen)}
        className="page-button"
        style={{ width: '100%' }}
        title="Browse People"
      >
        <svg width="16" height="16" viewBox="0 0 16 16" fill="currentColor" style={{verticalAlign: 'middle', marginTop: '0px', marginLeft: '2px', marginBottom: '4px', marginRight: '4px'}}>
          <circle cx="8" cy="5" r="3"/>
          <path d="M2 14c0-3.3 2.7-6 6-6s6 2.7 6 6"/>
        </svg>
        People
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
            minWidth: '320px',
            maxWidth: '400px',
            maxHeight: '400px',
            overflowY: 'auto',
            boxShadow: '0 4px 12px rgba(0,0,0,0.5)',
          }}
        >
          <div style={{ marginBottom: '8px', color: '#e8f09e', fontWeight: 'bold', fontSize: '12px', borderBottom: '1px solid #444', paddingBottom: '8px' }}>
            People ({people.length})
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

          {!loading && !error && people.length === 0 && (
            <div style={{ color: '#888', textAlign: 'center', padding: '20px' }}>
              No people found
            </div>
          )}

          {!loading && !error && people.length > 0 && (
            <div
              style={{
                display: 'grid',
                gridTemplateColumns: 'repeat(2, 1fr)',
                gap: '4px',
              }}
            >
              {people.map((person) => (
                <button
                  key={person.name}
                  onClick={() => handlePersonClick(person)}
                  style={{
                    display: 'flex',
                    alignItems: 'center',
                    gap: '6px',
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
                  title={`${person.name} (${person.image_count} photos)`}
                >
                  {person.thumbnail_path ? (
                    <div
                      style={{
                        width: '32px',
                        height: '32px',
                        borderRadius: '50%',
                        overflow: 'hidden',
                        flexShrink: 0,
                        position: 'relative',
                      }}
                    >
                      <img
                        src={person.thumbnail_path}
                        alt={person.name}
                        style={getFaceCropStyles(person, 32)}
                      />
                    </div>
                  ) : (
                    <svg width="32" height="32" viewBox="0 0 16 16" fill="#e8f09e" style={{ flexShrink: 0 }}>
                      <circle cx="8" cy="5" r="3"/>
                      <path d="M2 14c0-3.3 2.7-6 6-6s6 2.7 6 6"/>
                    </svg>
                  )}
                  <span style={{
                    overflow: 'hidden',
                    textOverflow: 'ellipsis',
                    whiteSpace: 'nowrap',
                    flex: 1,
                  }}>
                    {person.name}
                  </span>
                  {/* <span style={{ color: '#888', fontSize: '10px', flexShrink: 0 }}>
                    {person.image_count}
                  </span> */}
                </button>
              ))}
            </div>
          )}
        </div>
      )}
    </div>
  );
}