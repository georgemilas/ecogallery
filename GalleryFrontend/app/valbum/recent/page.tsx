'use client';

import { VirtualAlbumPage } from '../components/VirtualAlbumPage';
import { Suspense } from 'react';

export default function Page() {
  return (
    <Suspense fallback={<div>Loading...</div>}>
      <VirtualAlbumPage initialView="recent" />
    </Suspense>
  );
}