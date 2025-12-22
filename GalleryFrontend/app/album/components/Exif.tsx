import React from 'react';
import { ImageItemContent, AlbumItemHierarchy, ImageExif, VideoMetadata } from './AlbumHierarchyProps';
import './exif.css';

interface ExifPanelProps {
  exif: ImageExif | null;
  videoMetadata: VideoMetadata | null;
  album: AlbumItemHierarchy;
  image: ImageItemContent;
  onClose: () => void;
}

function getThumbRelativePath(fullPath: string): string {
  const thumbPath = fullPath.replace(/\\/g, '/').split('/').slice(0, -1);
  return thumbPath.splice(thumbPath.indexOf('400')+1).join('/');
}

function formatDuration(durationStr: string | null): string {
  if (!durationStr) return '';
  // Parse ISO 8601 duration (e.g., "PT1M20S" or "PT1H30M45S")
  const match = durationStr.match(/PT(?:(\d+)H)?(?:(\d+)M)?(?:(\d+(?:\.\d+)?)S)?/);
  if (!match) return durationStr;
  
  const hours = parseInt(match[1] || '0');
  const minutes = parseInt(match[2] || '0');
  const seconds = Math.floor(parseFloat(match[3] || '0'));
  
  if (hours > 0) {
    return `${hours}:${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}`;
  }
  return `${minutes}:${seconds.toString().padStart(2, '0')}`;
}


export function ExifPanel({ exif, videoMetadata, album, image, onClose }: ExifPanelProps): JSX.Element {
  const isVideo = image.is_movie;
  
  return (
    <div className="exif-panel">
      <div className="exif-header">
        <h3>{isVideo ? 'Video Metadata' : 'Image Metadata'}</h3>
        <button onClick={onClose} className="exif-close" title="Close">
          <svg viewBox="0 0 24 24" fill="none">
            <path d="M6 6L18 18M18 6L6 18" stroke="white" strokeWidth="2" strokeLinecap="round"/>
          </svg>
        </button>
      </div>
      <div className="exif-content">
        <div className="exif-row">
          <span className="exif-label">Album:</span>
          <span className="exif-value">{album.name.replace(/\\/g, '/')}</span>
        </div> 
        <div className="exif-row">
          <span className="exif-label">File Name:</span>
          <span className="exif-value">{image.name.replace(/\\/g, '/').split('/').pop()}</span>
        </div> 
        <div className="exif-row">
          <span className="exif-label">Raw path:</span>
          <span className="exif-value">{getThumbRelativePath(image.thumbnail_path)}</span>
        </div> 
        
        {isVideo && videoMetadata ? (
          <>
            {videoMetadata.file_size_bytes && (
              <div className="exif-row">
                <span className="exif-label">File Size:</span>
                <span className="exif-value">{(videoMetadata.file_size_bytes / 1024 / 1024).toFixed(2)} MB</span>
              </div>
            )}
            {videoMetadata.duration && (
              <div className="exif-row">
                <span className="exif-label">Duration:</span>
                <span className="exif-value">{formatDuration(videoMetadata.duration)}</span>
              </div>
            )}
            {videoMetadata.video_width && videoMetadata.video_height && (
              <div className="exif-row">
                <span className="exif-label">Dimensions:</span>
                <span className="exif-value">{videoMetadata.video_width} × {videoMetadata.video_height}</span>
              </div>
            )}
            {videoMetadata.video_codec && (
              <div className="exif-row">
                <span className="exif-label">Video Codec:</span>
                <span className="exif-value">{videoMetadata.video_codec}</span>
              </div>
            )}
            {videoMetadata.audio_codec && (
              <div className="exif-row">
                <span className="exif-label">Audio Codec:</span>
                <span className="exif-value">{videoMetadata.audio_codec}</span>
              </div>
            )}
            {videoMetadata.frame_rate && (
              <div className="exif-row">
                <span className="exif-label">Frame Rate:</span>
                <span className="exif-value">{videoMetadata.frame_rate.toFixed(2)} fps</span>
              </div>
            )}
            {videoMetadata.video_bit_rate && (
              <div className="exif-row">
                <span className="exif-label">Video Bit Rate:</span>
                <span className="exif-value">{(videoMetadata.video_bit_rate / 1000000).toFixed(2)} Mbps</span>
              </div>
            )}
            {videoMetadata.audio_sample_rate && (
              <div className="exif-row">
                <span className="exif-label">Audio Sample Rate:</span>
                <span className="exif-value">{(videoMetadata.audio_sample_rate / 1000).toFixed(1)} kHz</span>
              </div>
            )}
            {videoMetadata.audio_channels && (
              <div className="exif-row">
                <span className="exif-label">Audio Channels:</span>
                <span className="exif-value">{videoMetadata.audio_channels === 1 ? 'Mono' : videoMetadata.audio_channels === 2 ? 'Stereo' : `${videoMetadata.audio_channels} channels`}</span>
              </div>
            )}
            {videoMetadata.audio_bit_rate && (
              <div className="exif-row">
                <span className="exif-label">Audio Bit Rate:</span>
                <span className="exif-value">{(videoMetadata.audio_bit_rate / 1000).toFixed(0)} kbps</span>
              </div>
            )}
            {videoMetadata.pixel_format && (
              <div className="exif-row">
                <span className="exif-label">Pixel Format:</span>
                <span className="exif-value">{videoMetadata.pixel_format}</span>
              </div>
            )}
            {videoMetadata.camera && (
              <div className="exif-row">
                <span className="exif-label">Camera:</span>
                <span className="exif-value">{videoMetadata.camera}</span>
              </div>
            )}
            {videoMetadata.format_name && (
              <div className="exif-row">
                <span className="exif-label">Format:</span>
                <span className="exif-value">{videoMetadata.format_name}</span>
              </div>
            )}
            {videoMetadata.date_taken && (
              <div className="exif-row">
                <span className="exif-label">Date Taken:</span>
                <span className="exif-value">{new Date(videoMetadata.date_taken).toLocaleString()}</span>
              </div>
            )}
            {videoMetadata.date_modified && (
              <div className="exif-row">
                <span className="exif-label">Date Modified:</span>
                <span className="exif-value">{new Date(videoMetadata.date_modified).toLocaleString()}</span>
              </div>
            )}
            {videoMetadata.software && (
              <div className="exif-row">
                <span className="exif-label">Software:</span>
                <span className="exif-value">{videoMetadata.software}</span>
              </div>
            )}
          </>
        ) : exif ? (
          <>
            {exif.file_size_bytes && (
              <div className="exif-row">
                <span className="exif-label">File Size:</span>
                <span className="exif-value">{(exif.file_size_bytes / 1024 / 1024).toFixed(2)} MB</span>
              </div>
            )}
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
            {exif.exposure_program && (
              <div className="exif-row">
                <span className="exif-label">Exposure Program:</span>
                <span className="exif-value">{exif.exposure_program}</span>
              </div>
            )}
            {exif.exposure_bias && (
              <div className="exif-row">
                <span className="exif-label">Exposure Bias:</span>
                <span className="exif-value">{exif.exposure_bias}</span>
              </div>
            )}
            {exif.scene_capture_type && (
              <div className="exif-row">
                <span className="exif-label">Scene Capture Type:</span>
                <span className="exif-value">{exif.scene_capture_type}</span>
              </div>
            )}
            {exif.color_space && (
              <div className="exif-row">
                <span className="exif-label">Color Space:</span>
                <span className="exif-value">{exif.color_space}</span>
              </div>
            )}
            {exif.image_width && exif.image_height && (
              <div className="exif-row">
                <span className="exif-label">Dimensions:</span>
                <span className="exif-value">{exif.image_width} × {exif.image_height}</span>
              </div>
            )}
            {exif.software && (
              <div className="exif-row">
                <span className="exif-label">Software:</span>
                <span className="exif-value">{exif.software}</span>
              </div>
            )}
          </>
        ) : null}
      </div>
    </div>
  );
}
