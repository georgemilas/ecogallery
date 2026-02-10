import React from 'react';
import { useAuth } from '@/app/contexts/AuthContext';
import { AppRouterInstance } from 'next/dist/shared/lib/app-router-context.shared-runtime';

interface MenuPanelProps {
  isOpen: boolean;
  onClose: () => void;
  onSettingsClick: () => void;
  router: AppRouterInstance;
}

export function MenuPanel({ isOpen, onClose, onSettingsClick, router }: MenuPanelProps): JSX.Element | null {
  const { user, logout } = useAuth();

  if (!isOpen) return null;

  const menuButtonStyle: React.CSSProperties = {
    background: 'none',
    border: 'none',
    cursor: 'pointer',
    padding: '14px 20px',
    display: 'flex',
    alignItems: 'center',
    gap: '12px',
    color: '#ddd',
    fontSize: '15px',
    width: '100%',
    textAlign: 'left',
  };

  return (
    <div
      onClick={onClose}
      style={{
        position: 'fixed',
        top: 0,
        left: 0,
        right: 0,
        bottom: 0,
        backgroundColor: 'rgba(0, 0, 0, 0.7)',
        zIndex: 2000,
      }}
    >
      <div
        onClick={(e) => e.stopPropagation()}
        style={{
          position: 'absolute',
          top: 0,
          left: 0,
          bottom: 0,
          width: '250px',
          backgroundColor: '#2a2a2a',
          borderRight: '1px solid #e8f09e',
          display: 'flex',
          flexDirection: 'column',
          padding: '24px 0',
        }}
      >
        <h3 style={{ margin: '0 0 24px 0', padding: '0 20px', color: '#e8f09e' }}>Menu</h3>

        <button
          onClick={() => {
            onClose();
            onSettingsClick();
          }}
          style={menuButtonStyle}
          onMouseEnter={(e) => (e.currentTarget.style.backgroundColor = '#3a3a3a')}
          onMouseLeave={(e) => (e.currentTarget.style.backgroundColor = 'transparent')}
        >
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="#e8f09e" strokeWidth="1.5">
            <path d="M12 15a3 3 0 1 0 0-6 3 3 0 0 0 0 6z"/>
            <path d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 0 1-2.83 2.83l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-4 0v-.09a1.65 1.65 0 0 0-1.08-1.51 1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 0 1-2.83-2.83l.06-.06a1.65 1.65 0 0 0 .33-1.82 1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1 0-4h.09a1.65 1.65 0 0 0 1.51-1.08 1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 0 1 2.83-2.83l.06.06a1.65 1.65 0 0 0 1.82.33h.08a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 4 0v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 0 1 2.83 2.83l-.06.06a1.65 1.65 0 0 0-.33 1.82v.08a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 0 4h-.09a1.65 1.65 0 0 0-1.51 1z"/>
          </svg>
          Settings
        </button>

        {user?.roles?.includes('user_admin') && (
          <button
            onClick={() => {
              onClose();
              router.push('/manage-users');
            }}
            style={menuButtonStyle}
            onMouseEnter={(e) => (e.currentTarget.style.backgroundColor = '#3a3a3a')}
            onMouseLeave={(e) => (e.currentTarget.style.backgroundColor = 'transparent')}
          >
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="#e8f09e" strokeWidth="1.5">
              <path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"/>
              <circle cx="9" cy="7" r="4"/>
              <path d="M23 21v-2a4 4 0 0 0-3-3.87"/>
              <path d="M16 3.13a4 4 0 0 1 0 7.75"/>
            </svg>
            Manage Users
          </button>
        )}

        <button
          onClick={() => {
            onClose();
            logout();
          }}
          style={menuButtonStyle}
          onMouseEnter={(e) => (e.currentTarget.style.backgroundColor = '#3a3a3a')}
          onMouseLeave={(e) => (e.currentTarget.style.backgroundColor = 'transparent')}
        >
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="#dc3545" strokeWidth="1.5">
            <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4"/>
            <polyline points="16 17 21 12 16 7"/>
            <line x1="21" y1="12" x2="9" y2="12"/>
          </svg>
          Logout
        </button>
      </div>
    </div>
  );
}
