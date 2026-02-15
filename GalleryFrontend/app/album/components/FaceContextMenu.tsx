import React, { useState, useRef, useEffect } from 'react';
import { FaceBox } from './AlbumHierarchyProps';
import { apiFetch } from '@/app/utils/apiFetch';
import { useAuth } from '@/app/contexts/AuthContext';

export interface FaceContextMenuProps {
  face: FaceBox;
  position: { x: number; y: number };
  onClose: () => void;
  onNameUpdate: (personId: number, newName: string) => void;
  onFaceSearch: (personId: number, personName: string | null) => void;
  onFaceDelete?: (faceId: number) => void;
  onPersonDelete?: (personId: number) => void;
  onSearchByName?: (name: string) => void;
  onSearchByPersonId?: (personId: number) => void;
}

export function FaceContextMenu({ face, position, onClose, onNameUpdate, onFaceSearch, onFaceDelete, onPersonDelete, onSearchByName, onSearchByPersonId }: FaceContextMenuProps) {
  const { user } = useAuth();
  const [name, setName] = useState(face.person_name || '');
  const [saving, setSaving] = useState(false);
  const [deleting, setDeleting] = useState(false);
  const [showDeletePersonConfirm, setShowDeletePersonConfirm] = useState(false);
  const menuRef = useRef<HTMLDivElement>(null);
  const [adjustedPosition, setAdjustedPosition] = useState(position);

  // Adjust position to keep menu within viewport
  useEffect(() => {
    if (menuRef.current) {
      const rect = menuRef.current.getBoundingClientRect();
      const padding = 10; // Minimum distance from edge
      let newX = position.x;
      let newY = position.y;

      // Adjust horizontal position if menu goes off right edge
      if (position.x + rect.width > window.innerWidth - padding) {
        newX = window.innerWidth - rect.width - padding;
      }
      // Adjust if menu goes off left edge
      if (newX < padding) {
        newX = padding;
      }

      // Adjust vertical position if menu goes off bottom edge
      if (position.y + rect.height > window.innerHeight - padding) {
        newY = window.innerHeight - rect.height - padding;
      }
      // Adjust if menu goes off top edge
      if (newY < padding) {
        newY = padding;
      }

      if (newX !== position.x || newY !== position.y) {
        setAdjustedPosition({ x: newX, y: newY });
      }
    }
  }, [position]);

  useEffect(() => {
    const handleClickOutside = (e: MouseEvent) => {
      if (menuRef.current && !menuRef.current.contains(e.target as Node)) {
        onClose();
      }
    };
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, [onClose]);

  const handleSave = async () => {
    if (!face.person_id) return;
    setSaving(true);
    try {
      const response = await apiFetch(`/api/v1/faces/person/${face.person_id}/name`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ name: name || null }),
      });
      if (response.ok) {
        onNameUpdate(face.person_id, name);
        onClose();
      }
    } catch (e) {
      console.error('Failed to update name:', e);
    } finally {
      setSaving(false);
    }
  };

  const handleFindAll = () => {
    if (face.person_id) {
      onFaceSearch(face.person_id, face.person_name);
    }
    onClose();
  };

  const handleSearchByName = () => {
    if (name.trim() && onSearchByName) {
      onSearchByName(name.trim());
      onClose();
    }
  };

  const handleSearchByPersonId = () => {
    if (face.person_id && onSearchByPersonId) {
      onSearchByPersonId(face.person_id);
      onClose();
    }
  };

  const handleDeleteFace = async () => {
    if (!face.face_id || !onFaceDelete) return;
    setDeleting(true);
    try {
      const response = await apiFetch(`/api/v1/faces/${face.face_id}`, {
        method: 'DELETE',
      });
      if (response.ok) {
        onFaceDelete(face.face_id);
        onClose();
      }
    } catch (e) {
      console.error('Failed to delete face:', e);
    } finally {
      setDeleting(false);
    }
  };

  const handleDeletePerson = async () => {
    if (!face.person_id || !onPersonDelete) return;
    setDeleting(true);
    try {
      const response = await apiFetch(`/api/v1/faces/person/${face.person_id}`, {
        method: 'DELETE',
      });
      if (response.ok) {
        onPersonDelete(face.person_id);
        onClose();
      }
    } catch (e) {
      console.error('Failed to delete person:', e);
    } finally {
      setDeleting(false);
      setShowDeletePersonConfirm(false);
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
        minWidth: '200px',
        boxShadow: '0 4px 12px rgba(0,0,0,0.5)',
      }}
      onClick={(e) => e.stopPropagation()}
    >
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: '12px' }}>
        <div style={{ color: '#e8f09e', fontWeight: 'bold', fontSize: '12px' }}>
          Face #{face.face_id}
          {face.person_id && (
            <span style={{ color: '#888', fontWeight: 'normal' }}>
              {' '}(Person #{face.person_id})
              {user?.is_admin && onSearchByPersonId && (
                <button
                  onClick={handleSearchByPersonId}
                  style={{
                    background: 'none',
                    border: 'none',
                    color: '#888',
                    cursor: 'pointer',
                    padding: '0 0 0 4px',
                    verticalAlign: 'middle',
                  }}
                  title="Search by Person ID"
                >
                  <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                    <circle cx="10" cy="10" r="6" />
                    <line x1="16" y1="16" x2="21" y2="21" />
                  </svg>
                </button>
              )}
            </span>
          )}
        </div>
        <button
          onClick={onClose}
          style={{
            background: 'none',
            border: 'none',
            color: '#888',
            cursor: 'pointer',
            padding: '0',
            marginLeft: '8px',
            lineHeight: '1',
          }}
          title="Close"
        >
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
            <path d="M18 6L6 18M6 6l12 12" />
          </svg>
        </button>
      </div>

      {user?.is_admin && (
        <>
          <div style={{ marginBottom: '8px', display: 'flex', gap: '4px' }}>
            <input
              type="text"
              value={name}
              onChange={(e) => setName(e.target.value)}
              placeholder="Enter name..."
              style={{
                flex: 1,
                padding: '6px 8px',
                backgroundColor: '#1a1a1a',
                border: '1px solid #555',
                borderRadius: '4px',
                color: 'white',
                fontSize: '12px',
              }}
              onKeyDown={(e) => {
                if (e.key === 'Enter') handleSave();
                if (e.key === 'Escape') onClose();
              }}
              autoFocus
            />
            {onSearchByName && (
              <button
                onClick={handleSearchByName}
                disabled={!name.trim()}
                style={{
                  background: 'none',
                  // border: '1px solid #555',
                  borderRadius: '4px',
                  color: name.trim() ? '#e8f09e' : '#555',
                  cursor: name.trim() ? 'pointer' : 'not-allowed',
                  padding: '4px 6px',
                }}
                title="Search by Name"
              >
                <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                  <circle cx="10" cy="10" r="6" />
                  <line x1="16" y1="16" x2="21" y2="21" />
                </svg>
              </button>
            )}
          </div>

          <button
            onClick={handleSave}
            disabled={saving || !face.person_id}
            style={{
              padding: '6px 12px',
              backgroundColor: '#4CAF50',
              color: 'white',
              border: 'none',
              borderRadius: '4px',
              cursor: saving || !face.person_id ? 'not-allowed' : 'pointer',
              opacity: saving || !face.person_id ? 0.5 : 1,
              fontSize: '12px',
              marginBottom: '8px',
              width: '100%',
            }}
          >
            {saving ? 'Saving...' : 'Save Name'}
          </button>
        </>
      )}

      <button
        onClick={handleFindAll}
        disabled={!face.person_id}
        style={{
          padding: '6px 12px',
          backgroundColor: '#e8f09e',
          color: '#333',
          border: 'none',
          borderRadius: '4px',
          cursor: !face.person_id ? 'not-allowed' : 'pointer',
          opacity: !face.person_id ? 0.5 : 1,
          fontSize: '12px',
          width: '100%',
          marginBottom: user?.is_admin ? '8px' : '0',
        }}
      >
        Find All Photos
      </button>

      {user?.is_admin && (
        <>
          {!showDeletePersonConfirm ? (
            <div style={{ display: 'flex', gap: '8px', marginTop: '8px', borderTop: '1px solid #444', paddingTop: '8px' }}>
              <button
                onClick={handleDeleteFace}
                disabled={deleting}
                style={{
                  flex: 1,
                  padding: '6px 8px',
                  backgroundColor: '#dc3545',
                  color: 'white',
                  border: 'none',
                  borderRadius: '4px',
                  cursor: deleting ? 'not-allowed' : 'pointer',
                  opacity: deleting ? 0.5 : 1,
                  fontSize: '11px',
                }}
                title="Delete this face record"
              >
                {deleting ? '...' : 'Delete Face'}
              </button>
              <button
                onClick={() => setShowDeletePersonConfirm(true)}
                disabled={!face.person_id || deleting}
                style={{
                  flex: 1,
                  padding: '6px 8px',
                  backgroundColor: '#dc3545',
                  color: 'white',
                  border: 'none',
                  borderRadius: '4px',
                  cursor: !face.person_id || deleting ? 'not-allowed' : 'pointer',
                  opacity: !face.person_id || deleting ? 0.5 : 1,
                  fontSize: '11px',
                }}
                title="Delete entire person and all associated faces"
              >
                Delete Person
              </button>
            </div>
          ) : (
            <div style={{ marginTop: '8px', borderTop: '1px solid #444', paddingTop: '8px' }}>
              <div style={{ color: '#ff6b6b', fontSize: '11px', marginBottom: '8px' }}>
                Delete person #{face.person_id} and ALL associated faces?
              </div>
              <div style={{ display: 'flex', gap: '8px' }}>
                <button
                  onClick={handleDeletePerson}
                  disabled={deleting}
                  style={{
                    flex: 1,
                    padding: '6px 8px',
                    backgroundColor: '#dc3545',
                    color: 'white',
                    border: 'none',
                    borderRadius: '4px',
                    cursor: deleting ? 'not-allowed' : 'pointer',
                    opacity: deleting ? 0.5 : 1,
                    fontSize: '11px',
                  }}
                >
                  {deleting ? 'Deleting...' : 'Yes, Delete'}
                </button>
                <button
                  onClick={() => setShowDeletePersonConfirm(false)}
                  style={{
                    flex: 1,
                    padding: '6px 8px',
                    backgroundColor: '#555',
                    color: 'white',
                    border: 'none',
                    borderRadius: '4px',
                    cursor: 'pointer',
                    fontSize: '11px',
                  }}
                >
                  Cancel
                </button>
              </div>
            </div>
          )}
        </>
      )}
    </div>
  );
}
