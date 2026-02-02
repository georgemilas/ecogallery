'use client';

import React, { createContext, useContext, useState, useEffect } from 'react';

interface GallerySettings {
  showFaceBoxes: boolean;
  searchPageSize: number;
}

interface GallerySettingsContextType {
  settings: GallerySettings;
  setShowFaceBoxes: (show: boolean) => void;
  setSearchPageSize: (size: number) => void;
}

const defaultSettings: GallerySettings = {
  showFaceBoxes: false,
  searchPageSize: 2000,
};

const GallerySettingsContext = createContext<GallerySettingsContextType | undefined>(undefined);

const STORAGE_KEY = 'gallerySettings';

export function GallerySettingsProvider({ children }: { children: React.ReactNode }) {
  const [settings, setSettings] = useState<GallerySettings>(defaultSettings);
  const [isHydrated, setIsHydrated] = useState(false);

  // Load settings from localStorage on mount
  useEffect(() => {
    try {
      const stored = localStorage.getItem(STORAGE_KEY);
      if (stored) {
        const parsed = JSON.parse(stored);
        setSettings({ ...defaultSettings, ...parsed });
      }
    } catch (e) {
      console.error('Error loading gallery settings:', e);
    }
    setIsHydrated(true);
  }, []);

  // Save settings to localStorage when they change
  useEffect(() => {
    if (isHydrated) {
      try {
        localStorage.setItem(STORAGE_KEY, JSON.stringify(settings));
      } catch (e) {
        console.error('Error saving gallery settings:', e);
      }
    }
  }, [settings, isHydrated]);

  const setShowFaceBoxes = (show: boolean) => {
    setSettings(prev => ({ ...prev, showFaceBoxes: show }));
  };

  const setSearchPageSize = (size: number) => {
    setSettings(prev => ({ ...prev, searchPageSize: size }));
  };

  return (
    <GallerySettingsContext.Provider value={{ settings, setShowFaceBoxes, setSearchPageSize }}>
      {children}
    </GallerySettingsContext.Provider>
  );
}

export function useGallerySettings() {
  const context = useContext(GallerySettingsContext);
  if (context === undefined) {
    throw new Error('useGallerySettings must be used within a GallerySettingsProvider');
  }
  return context;
}