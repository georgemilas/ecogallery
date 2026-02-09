import React, { useState, useEffect } from 'react';
import { useGallerySettings } from '@/app/contexts/GallerySettingsContext';

interface SettingsModalProps {
  isOpen: boolean;
  onClose: () => void;
}

function useTextNumericField(value: number, setter: (v: number) => void, min: number, max: number, fallback: number) {
  const [text, setText] = useState(String(value));
  useEffect(() => { setText(String(value)); }, [value]);
  const onChange = (e: React.ChangeEvent<HTMLInputElement>) => setText(e.target.value);
  const onBlur = () => {
    const parsed = parseInt(text);
    if (isNaN(parsed)) { setter(fallback); setText(String(fallback)); }
    else { const clamped = Math.max(min, Math.min(max, parsed)); setter(clamped); setText(String(clamped)); }
  };
  return { value: text, onChange, onBlur };
}

export function SettingsModal({ isOpen, onClose }: SettingsModalProps): JSX.Element | null {
  const { settings, setShowFaceBoxes, setSearchPageSize, setPeopleMenuLimit, setUseFullResolution, setDesktopThumbnailHeight, setGalleryGap } = useGallerySettings();

  const searchPageSizeField = useTextNumericField(settings.searchPageSize, setSearchPageSize, 1, 10000, 2000);
  const peopleMenuLimitField = useTextNumericField(settings.peopleMenuLimit, setPeopleMenuLimit, 1, 100, 50);
  const thumbnailHeightField = useTextNumericField(settings.desktopThumbnailHeight, setDesktopThumbnailHeight, 100, 600, 300);
  const galleryGapField = useTextNumericField(settings.galleryGap, setGalleryGap, 0, 50, 8);

  if (!isOpen) return null;

  return (
    <div
      className="settings-modal-overlay"
      onClick={onClose}
      style={{
        position: 'fixed',
        top: 0,
        left: 0,
        right: 0,
        bottom: 0,
        backgroundColor: 'rgba(0, 0, 0, 0.7)',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        zIndex: 2000,
      }}
    >
      <div
        className="settings-modal"
        onClick={(e) => e.stopPropagation()}
        style={{
          backgroundColor: '#2a2a2a',
          borderRadius: '8px',
          padding: '24px',
          minWidth: '300px',
          maxWidth: '440px',
          border: '1px solid #e8f09e',
          position: 'relative',
        }}
      >
        <button
          onClick={onClose}
          style={{
            position: 'absolute',
            top: '12px',
            right: '12px',
            background: 'none',
            border: 'none',
            cursor: 'pointer',
            padding: '4px',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
          }}
          title="Close"
        >
          <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="#e8f09e" strokeWidth="2" strokeLinecap="round">
            <line x1="18" y1="6" x2="6" y2="18"/>
            <line x1="6" y1="6" x2="18" y2="18"/>
          </svg>
        </button>
        <h3 style={{ margin: '0 0 20px 0', color: '#e8f09e' }}>Gallery Settings</h3>

        <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '16px' }}>
          <label htmlFor="showFaces" style={{ color: '#ddd' }}>Show Face Boxes</label>
          <label className="toggle-switch" style={{ position: 'relative', display: 'inline-block', width: '50px', height: '26px' }}>
            <input
              type="checkbox"
              id="showFaces"
              checked={settings.showFaceBoxes}
              onChange={(e) => setShowFaceBoxes(e.target.checked)}
              style={{ opacity: 0, width: 0, height: 0 }}
            />
            <span
              style={{
                position: 'absolute',
                cursor: 'pointer',
                top: 0,
                left: 0,
                right: 0,
                bottom: 0,
                backgroundColor: settings.showFaceBoxes ? '#4CAF50' : '#555',
                transition: '0.3s',
                borderRadius: '26px',
              }}
            >
              <span style={{
                position: 'absolute',
                content: '""',
                height: '20px',
                width: '20px',
                left: settings.showFaceBoxes ? '26px' : '3px',
                bottom: '3px',
                backgroundColor: 'white',
                transition: '0.3s',
                borderRadius: '50%',
              }}/>
            </span>
          </label>
        </div>

        <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '16px' }}>
          <label htmlFor="fullResolution" style={{ color: '#ddd' }}>Full Image Resolution</label>
          <label className="toggle-switch" style={{ position: 'relative', display: 'inline-block', width: '50px', height: '26px' }}>
            <input
              type="checkbox"
              id="fullResolution"
              checked={settings.useFullResolution}
              onChange={(e) => setUseFullResolution(e.target.checked)}
              style={{ opacity: 0, width: 0, height: 0 }}
            />
            <span
              style={{
                position: 'absolute',
                cursor: 'pointer',
                top: 0,
                left: 0,
                right: 0,
                bottom: 0,
                backgroundColor: settings.useFullResolution ? '#4CAF50' : '#555',
                transition: '0.3s',
                borderRadius: '26px',
              }}
            >
              <span style={{
                position: 'absolute',
                content: '""',
                height: '20px',
                width: '20px',
                left: settings.useFullResolution ? '26px' : '3px',
                bottom: '3px',
                backgroundColor: 'white',
                transition: '0.3s',
                borderRadius: '50%',
              }}/>
            </span>
          </label>
        </div>

        <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '16px' }}>
          <label htmlFor="searchPageSize" style={{ color: '#ddd' }}>Search Page Size</label>
          <input
            type="text"
            id="searchPageSize"
            title="Range: 1 – 10000"
            {...searchPageSizeField}
            style={{
              width: '80px',
              padding: '6px 10px',
              borderRadius: '4px',
              border: '1px solid #555',
              backgroundColor: '#333',
              color: '#ddd',
              textAlign: 'right',
            }}
          />
        </div>

        <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '16px' }}>
          <label htmlFor="peopleMenuLimit" style={{ color: '#ddd' }}>People Menu Limit</label>
          <input
            type="text"
            id="peopleMenuLimit"
            title="Range: 1 – 100"
            {...peopleMenuLimitField}
            style={{
              width: '80px',
              padding: '6px 10px',
              borderRadius: '4px',
              border: '1px solid #555',
              backgroundColor: '#333',
              color: '#ddd',
              textAlign: 'right',
            }}
          />
        </div>

        <div style={{
          border: '1px solid #555',
          borderRadius: '6px',
          padding: '14px',
          marginBottom: '16px',
        }}>
          <label style={{ color: '#e8f09e', fontSize: '13px', marginBottom: '10px', display: 'block' }}>Desktop Gallery Layout</label>
          <div style={{ display: 'flex', gap: '16px', alignItems: 'center' }}>
            <div style={{ flex: 1 }}>
              <label htmlFor="desktopThumbnailHeight" style={{ color: '#ddd', fontSize: '12px', marginBottom: '4px', display: 'block' }}>Thumbnail Size</label>
              <input
                type="text"
                id="desktopThumbnailHeight"
                title="Range: 100 – 600"
                {...thumbnailHeightField}
                style={{
                  width: '100%',
                  padding: '6px 10px',
                  borderRadius: '4px',
                  border: '1px solid #555',
                  backgroundColor: '#333',
                  color: '#ddd',
                  textAlign: 'right',
                  boxSizing: 'border-box',
                }}
              />
            </div>
            <div style={{ flex: 1 }}>
              <label htmlFor="galleryGap" style={{ color: '#ddd', fontSize: '12px', marginBottom: '4px', display: 'block' }}>Gap</label>
              <input
                type="text"
                id="galleryGap"
                title="Range: 0 – 50"
                {...galleryGapField}
                style={{
                  width: '100%',
                  padding: '6px 10px',
                  borderRadius: '4px',
                  border: '1px solid #555',
                  backgroundColor: '#333',
                  color: '#ddd',
                  textAlign: 'right',
                  boxSizing: 'border-box',
                }}
              />
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
