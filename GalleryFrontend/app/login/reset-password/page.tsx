'use client';

import { ResetPasswordPage } from '../components/ResetPasswordPage';
import { Suspense } from 'react';

export default function ResetPassword() {
  return (
    <Suspense fallback={<div>Loading...</div>}>
        <ResetPasswordPage />
    </Suspense>
  );
}

