import { ManageUsersPage } from './ManageUsersPage';
import { Suspense } from 'react';

export default function ManageUsers() {
  return (
    <Suspense fallback={<div>Loading...</div>}>
      <ManageUsersPage />
    </Suspense>
  );
}
