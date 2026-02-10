import { CreateRolePage } from './CreateRolePage';
import { Suspense } from 'react';

export default function CreateRole() {
  return (
    <Suspense fallback={<div>Loading...</div>}>
      <CreateRolePage />
    </Suspense>
  );
}
