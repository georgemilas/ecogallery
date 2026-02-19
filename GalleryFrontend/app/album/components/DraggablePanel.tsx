import React, { useState, useCallback, useEffect, useRef } from 'react';

export interface DraggablePanelProps {
  isOpen: boolean;
  onClose?: () => void;
  title: React.ReactNode;
  titleActions?: React.ReactNode;
  children: React.ReactNode;
  defaultPos?: { top: number; left: number };
  defaultWidth?: number;
  defaultHeight?: number;
  /** Controlled width — if provided, the panel uses this instead of internal state */
  width?: number;
  onWidthChange?: (width: number) => void;
  minWidth?: number;
  minHeight?: number;
  zIndex?: number;
}

export function DraggablePanel({
  isOpen,
  onClose,
  title,
  titleActions,
  children,
  defaultPos = { top: 80, left: 20 },
  defaultWidth = 380,
  defaultHeight,
  width: controlledWidth,
  onWidthChange,
  minWidth = 300,
  minHeight = 200,
  zIndex = 1000,
}: DraggablePanelProps): JSX.Element | null {
  const [internalWidth, setInternalWidth] = useState(defaultWidth);
  const [panelHeight, setPanelHeight] = useState(
    defaultHeight ?? Math.min(600, typeof window !== 'undefined' ? window.innerHeight - 120 : 600)
  );
  const [panelPos, setPanelPos] = useState(defaultPos);
  type ResizeDir = 'left' | 'right' | 'bottom' | 'bottom-left' | 'bottom-right';
  const [resizeDir, setResizeDir] = useState<ResizeDir | null>(null);
  const [isDragging, setIsDragging] = useState(false);
  const [isMinimized, setIsMinimized] = useState(false);
  const savedHeight = useRef<number | null>(null);
  const dragOffset = useRef({ x: 0, y: 0 });
  const panelRef = useRef<HTMLDivElement>(null);

  const panelWidth = controlledWidth ?? internalWidth;
  const setPanelWidth = onWidthChange ?? setInternalWidth;

  const toggleMinimize = useCallback(() => {
    setIsMinimized(prev => {
      if (!prev) {
        savedHeight.current = panelHeight;
      } else if (savedHeight.current !== null) {
        setPanelHeight(savedHeight.current);
      }
      return !prev;
    });
  }, [panelHeight]);

  // Resize
  const handleResizeStart = useCallback((dir: ResizeDir) => (e: React.MouseEvent) => {
    e.preventDefault();
    setResizeDir(dir);
  }, []);

  useEffect(() => {
    if (!resizeDir) return;

    const resizesRight = resizeDir === 'right' || resizeDir === 'bottom-right';
    const resizesLeft = resizeDir === 'left' || resizeDir === 'bottom-left';
    const resizesBottom = resizeDir === 'bottom' || resizeDir === 'bottom-left' || resizeDir === 'bottom-right';

    const handleMouseMove = (e: MouseEvent) => {
      if (!panelRef.current) return;
      const rect = panelRef.current.getBoundingClientRect();
      if (resizesRight) {
        setPanelWidth(Math.max(minWidth, Math.min(e.clientX - rect.left, window.innerWidth - 40)));
      }
      if (resizesLeft) {
        const newWidth = Math.max(minWidth, Math.min(rect.right - e.clientX, window.innerWidth - 40));
        setPanelWidth(newWidth);
        setPanelPos(prev => ({ ...prev, left: rect.right - newWidth }));
      }
      if (resizesBottom) {
        setPanelHeight(Math.max(minHeight, Math.min(e.clientY - rect.top, window.innerHeight - rect.top - 20)));
      }
    };

    const handleMouseUp = () => setResizeDir(null);

    document.addEventListener('mousemove', handleMouseMove);
    document.addEventListener('mouseup', handleMouseUp);
    return () => {
      document.removeEventListener('mousemove', handleMouseMove);
      document.removeEventListener('mouseup', handleMouseUp);
    };
  }, [resizeDir, minWidth, minHeight, setPanelWidth]);

  // Drag to move
  const handleDragStart = useCallback((e: React.MouseEvent) => {
    e.preventDefault();
    setIsDragging(true);
    dragOffset.current = { x: e.clientX - panelPos.left, y: e.clientY - panelPos.top };
  }, [panelPos]);

  useEffect(() => {
    if (!isDragging) return;

    const handleMouseMove = (e: MouseEvent) => {
      const newLeft = Math.max(0, Math.min(e.clientX - dragOffset.current.x, window.innerWidth - 100));
      const newTop = Math.max(0, Math.min(e.clientY - dragOffset.current.y, window.innerHeight - 100));
      setPanelPos({ top: newTop, left: newLeft });
    };

    const handleMouseUp = () => setIsDragging(false);

    document.addEventListener('mousemove', handleMouseMove);
    document.addEventListener('mouseup', handleMouseUp);
    return () => {
      document.removeEventListener('mousemove', handleMouseMove);
      document.removeEventListener('mouseup', handleMouseUp);
    };
  }, [isDragging]);

  if (!isOpen) return null;

  const resizeHandleStyle = (dir: ResizeDir): React.CSSProperties => {
    const base: React.CSSProperties = {
      position: 'absolute',
      transition: 'background-color 0.15s',
      backgroundColor: resizeDir === dir ? '#e8f09e' : 'transparent',
    };
    if (dir === 'left') return { ...base, left: 0, top: 0, bottom: '6px', width: '6px', cursor: 'ew-resize', borderRadius: '8px 0 0 0' };
    if (dir === 'right') return { ...base, right: 0, top: 0, bottom: '6px', width: '6px', cursor: 'ew-resize', borderRadius: '0 8px 0 0' };
    if (dir === 'bottom') return { ...base, bottom: 0, left: '6px', right: '6px', height: '6px', cursor: 'ns-resize' };
    if (dir === 'bottom-left') return { ...base, bottom: 0, left: 0, width: '6px', height: '6px', cursor: 'nesw-resize', borderRadius: '0 0 0 8px' };
    return { ...base, bottom: 0, right: 0, width: '6px', height: '6px', cursor: 'nwse-resize', borderRadius: '0 0 8px 0' };
  };

  const handleHover = (dir: ResizeDir) => ({
    onMouseEnter: (e: React.MouseEvent<HTMLDivElement>) => { if (!resizeDir) e.currentTarget.style.backgroundColor = 'rgba(232, 240, 158, 0.3)'; },
    onMouseLeave: (e: React.MouseEvent<HTMLDivElement>) => { if (!resizeDir) e.currentTarget.style.backgroundColor = 'transparent'; },
  });

  const titleBarButtonStyle: React.CSSProperties = {
    background: 'none',
    border: '1px solid #888',
    borderRadius: '4px',
    color: '#ddd',
    cursor: 'pointer',
    padding: '2px 8px',
    fontSize: '12px',
    height: '22px',
    boxSizing: 'border-box',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    flexShrink: 0,
  };

  return (
    <div
      ref={panelRef}
      style={{
        position: 'fixed',
        top: `${panelPos.top}px`,
        left: `${panelPos.left}px`,
        backgroundColor: '#2a2a2a',
        borderRadius: '8px',
        padding: '16px',
        width: `${panelWidth}px`,
        minWidth: `${minWidth}px`,
        height: isMinimized ? 'auto' : `${panelHeight}px`,
        maxHeight: isMinimized ? undefined : 'calc(100vh - 120px)',
        border: '1px solid #e8f09e',
        boxShadow: '0 4px 20px rgba(0, 0, 0, 0.5)',
        zIndex,
        display: 'flex',
        flexDirection: 'column',
        overflow: 'hidden',
      }}
    >
      <div onMouseDown={handleResizeStart('left')} style={resizeHandleStyle('left')} {...handleHover('left')} />
      <div onMouseDown={handleResizeStart('right')} style={resizeHandleStyle('right')} {...handleHover('right')} />
      {!isMinimized && <>
        <div onMouseDown={handleResizeStart('bottom')} style={resizeHandleStyle('bottom')} {...handleHover('bottom')} />
        <div onMouseDown={handleResizeStart('bottom-left')} style={resizeHandleStyle('bottom-left')} {...handleHover('bottom-left')} />
        <div onMouseDown={handleResizeStart('bottom-right')} style={resizeHandleStyle('bottom-right')} {...handleHover('bottom-right')} />
      </>}

      {/* Title bar — drag handle */}
      <div
        onMouseDown={handleDragStart}
        style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: isMinimized ? 0 : '10px', cursor: 'move', flexShrink: 0 }}
      >
        <span style={{ color: '#e8f09e', fontWeight: 'bold', fontSize: '14px' }}>{title}</span>
        <div style={{ display: 'flex', gap: '4px', alignItems: 'center' }}>
          {titleActions}
          {titleActions && <div style={{ width: '8px' }} />}
          <button
            onMouseDown={e => e.stopPropagation()}
            onClick={toggleMinimize}
            style={titleBarButtonStyle}
            title={isMinimized ? 'Restore' : 'Minimize'}
          >
            {isMinimized ? '□' : '_'}
          </button>
          {onClose && (
            <button
              onMouseDown={e => e.stopPropagation()}
              onClick={onClose}
              style={titleBarButtonStyle}
              title="Close"
            >
              ×
            </button>
          )}
        </div>
      </div>

      {!isMinimized && children}
    </div>
  );
}
