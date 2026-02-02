'use client';

import { AlbumPage } from '../components/AlbumPage';
import { Suspense } from 'react';

export default function Page() {
  return (
    <Suspense fallback={<div>Loading...</div>}>
      <AlbumPage initialView="random" />
    </Suspense>
  );
}