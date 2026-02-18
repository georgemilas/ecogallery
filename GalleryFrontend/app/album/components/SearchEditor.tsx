import React, { useState } from 'react';
import { DraggablePanel } from './DraggablePanel';

export interface SearchEditorState {
  isOpen: boolean;
  setIsOpen: (open: boolean) => void;
  text: string;
  setText: (text: string) => void;
  error: string | null;
  clearError: () => void;
  panelWidth: number;
  setPanelWidth: (width: number) => void;
}

interface SearchEditorProps {
  searchEditor: SearchEditorState;
  onSearchSubmit: (expression: string, offset: number) => void;
  showSearch: boolean;
}

export function SearchEditor({ searchEditor, onSearchSubmit, showSearch }: SearchEditorProps): JSX.Element | null {
  const { isOpen, setIsOpen, text, setText, error, clearError, panelWidth, setPanelWidth } = searchEditor;

  const [defaultPos] = useState(() => ({
    top: 80,
    left: typeof window !== 'undefined' ? window.innerWidth - panelWidth - 20 : 500,
  }));

  const handleSearchSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (text.trim().length === 0) return;
    onSearchSubmit(text.trim(), 0);
  };

  const handleExpandedSearchSubmit = () => {
    if (text.trim().length === 0) return;
    onSearchSubmit(text.trim(), 0);
  };

  const handleExpandedSearchKeyDown = (e: React.KeyboardEvent<HTMLTextAreaElement>) => {
    if (e.key === 'Enter' && (e.ctrlKey || e.metaKey)) {
      e.preventDefault();
      handleExpandedSearchSubmit();
    }
    if (e.key === 'Escape') {
      setIsOpen(false);
    }
  };

  const renderSearchBar = () => {
    if (!showSearch) return <div></div>;

    return (
      <form className="searchbar" onSubmit={handleSearchSubmit} style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
        <div style={{ position: 'relative', display: 'flex', alignItems: 'center' }}>
          <input
            type="text"
            placeholder="Search expression..."
            value={text}
            onChange={(e) => setText(e.target.value)}
            style={{ paddingRight: '28px' }}
          />
          <button
            type="button"
            onClick={(e) => { e.preventDefault(); setIsOpen(!isOpen); }}
            title={isOpen ? "Close search editor" : "Expand search editor"}
            style={{
              position: 'absolute',
              right: '4px',
              top: '50%',
              transform: 'translateY(-50%)',
              background: 'none',
              border: 'none',
              cursor: 'pointer',
              padding: '2px',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              opacity: 0.7,
            }}
            onMouseEnter={(e) => { e.currentTarget.style.opacity = '1'; }}
            onMouseLeave={(e) => { e.currentTarget.style.opacity = '0.7'; }}
          >
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke={isOpen ? "#4CAF50" : "#080808"} strokeWidth="2.5" strokeLinecap="round">
              {isOpen ? (
                <line x1="5" y1="12" x2="19" y2="12"/>
              ) : (
                <>
                  <line x1="12" y1="5" x2="12" y2="19"/>
                  <line x1="5" y1="12" x2="19" y2="12"/>
                </>
              )}
            </svg>
          </button>
        </div>
        <button type="submit" className="search-button" title="Search">
          <svg viewBox="0 0 24 24" fill="none">
            <circle cx="10" cy="10" r="6" stroke="white" strokeWidth="2"/>
            <line x1="16.5" y1="16.5" x2="21" y2="21" stroke="white" strokeWidth="2"/>
          </svg>
        </button>
      </form>
    );
  };

  const closeButton = (
    <button
      onClick={() => setIsOpen(false)}
      style={{
        background: 'none',
        border: 'none',
        cursor: 'pointer',
        padding: '2px',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
      }}
      title="Close (Escape)"
    >
      <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="#e8f09e" strokeWidth="2" strokeLinecap="round">
        <line x1="18" y1="6" x2="6" y2="18"/>
        <line x1="6" y1="6" x2="18" y2="18"/>
      </svg>
    </button>
  );

  const renderExpandedSearch = () => (
    <DraggablePanel
      isOpen={isOpen}
      title="Search Expression"
      titleActions={closeButton}
      defaultPos={defaultPos}
      defaultHeight={400}
      width={panelWidth}
      onWidthChange={setPanelWidth}
    >
      <textarea
        value={text}
        onChange={(e) => setText(e.target.value)}
        onKeyDown={handleExpandedSearchKeyDown}
        placeholder="Enter your search expression..."
        autoFocus
        style={{
          width: '100%',
          flex: 1,
          minHeight: '100px',
          padding: '10px',
          borderRadius: '4px',
          border: '1px solid #555',
          backgroundColor: '#333',
          color: '#ddd',
          fontSize: '13px',
          fontFamily: 'monospace',
          resize: 'none',
          outline: 'none',
          boxSizing: 'border-box',
        }}
      />
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginTop: '10px' }}>
        <span style={{ color: '#666', fontSize: '11px' }}>Ctrl+Enter to search</span>
        <button
          onClick={handleExpandedSearchSubmit}
          style={{
            padding: '6px 12px',
            backgroundColor: '#e8f09e',
            color: '#333',
            border: 'none',
            borderRadius: '4px',
            cursor: 'pointer',
            fontWeight: 'bold',
            fontSize: '13px',
            display: 'flex',
            alignItems: 'center',
            gap: '5px',
          }}
        >
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none">
            <circle cx="10" cy="10" r="6" stroke="#333" strokeWidth="2"/>
            <line x1="16.5" y1="16.5" x2="21" y2="21" stroke="#333" strokeWidth="2"/>
          </svg>
          Search
        </button>
      </div>
    </DraggablePanel>
  );

  const renderSearchError = () => {
    if (!error) return null;

    return (
      <div
        style={{
          position: 'fixed',
          top: '80px',
          left: '50%',
          transform: 'translateX(-50%)',
          backgroundColor: '#dc3545',
          color: 'white',
          padding: '12px 20px',
          borderRadius: '8px',
          boxShadow: '0 4px 20px rgba(0, 0, 0, 0.5)',
          zIndex: 1001,
          maxWidth: '600px',
          display: 'flex',
          alignItems: 'flex-start',
          gap: '12px',
        }}
      >
        <svg width="20" height="20" viewBox="0 0 24 24" fill="none" style={{ flexShrink: 0, marginTop: '2px' }}>
          <circle cx="12" cy="12" r="10" stroke="white" strokeWidth="2"/>
          <line x1="12" y1="8" x2="12" y2="12" stroke="white" strokeWidth="2" strokeLinecap="round"/>
          <circle cx="12" cy="16" r="1" fill="white"/>
        </svg>
        <div style={{ flex: 1 }}>
          <div style={{ fontWeight: 'bold', marginBottom: '4px' }}>Search Error</div>
          <div style={{ fontSize: '13px', whiteSpace: 'pre-wrap', wordBreak: 'break-word' }}>{error}</div>
        </div>
        <button
          onClick={clearError}
          style={{
            background: 'rgba(255,255,255,0.2)',
            border: 'none',
            borderRadius: '4px',
            padding: '4px 8px',
            color: 'white',
            cursor: 'pointer',
            fontSize: '12px',
            flexShrink: 0,
          }}
          title="Dismiss"
        >
          Dismiss
        </button>
      </div>
    );
  };

  return (
    <>
      {renderSearchBar()}
      {renderExpandedSearch()}
      {renderSearchError()}
    </>
  );
}
