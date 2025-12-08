import React from 'react';
import { ImageExif } from './Album';
import './exif.css';

interface ExifPanelProps {
  exif: ImageExif;
  onClose: () => void;
}

export function ExifPanel({ exif, onClose }: ExifPanelProps) {
  return (
    <div className="exif-panel">
      <div className="exif-header">
        <h3>EXIF Data</h3>
        <button onClick={onClose} className="exif-close" title="Close">
          <svg viewBox="0 0 24 24" fill="none">
            <path d="M6 6L18 18M18 6L6 18" stroke="white" strokeWidth="2" strokeLinecap="round"/>
          </svg>
        </button>
      </div>
      <div className="exif-content">
        {exif.camera && (
          <div className="exif-row">
            <span className="exif-label">Camera:</span>
            <span className="exif-value">{exif.camera}</span>
          </div>
        )}
        {exif.lens && (
          <div className="exif-row">
            <span className="exif-label">Lens:</span>
            <span className="exif-value">{exif.lens}</span>
          </div>
        )}
        {exif.focal_length && (
          <div className="exif-row">
            <span className="exif-label">Focal Length:</span>
            <span className="exif-value">{exif.focal_length}</span>
          </div>
        )}
        {exif.aperture && (
          <div className="exif-row">
            <span className="exif-label">Aperture:</span>
            <span className="exif-value">{exif.aperture}</span>
          </div>
        )}
        {exif.exposure_time && (
          <div className="exif-row">
            <span className="exif-label">Shutter Speed:</span>
            <span className="exif-value">{exif.exposure_time}</span>
          </div>
        )}
        {exif.iso && (
          <div className="exif-row">
            <span className="exif-label">ISO:</span>
            <span className="exif-value">{exif.iso}</span>
          </div>
        )}
        {exif.date_taken && (
          <div className="exif-row">
            <span className="exif-label">Date Taken:</span>
            <span className="exif-value">{new Date(exif.date_taken).toLocaleString()}</span>
          </div>
        )}
        {exif.flash && (
          <div className="exif-row">
            <span className="exif-label">Flash:</span>
            <span className="exif-value">{exif.flash}</span>
          </div>
        )}
        {exif.white_balance && (
          <div className="exif-row">
            <span className="exif-label">White Balance:</span>
            <span className="exif-value">{exif.white_balance}</span>
          </div>
        )}
        {exif.image_width && exif.image_height && (
          <div className="exif-row">
            <span className="exif-label">Dimensions:</span>
            <span className="exif-value">{exif.image_width} × {exif.image_height}</span>
          </div>
        )}
        {exif.file_size_bytes && (
          <div className="exif-row">
            <span className="exif-label">File Size:</span>
            <span className="exif-value">{(exif.file_size_bytes / 1024 / 1024).toFixed(2)} MB</span>
          </div>
        )}
        {exif.software && (
          <div className="exif-row">
            <span className="exif-label">Software:</span>
            <span className="exif-value">{exif.software}</span>
          </div>
        )}
      </div>
    </div>
  );
}
