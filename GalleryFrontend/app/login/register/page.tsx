import { RegisterPage } from '../components/RegisterPage';
import { Suspense } from 'react';

export default function Register() {
  return (
    <Suspense fallback={<div>Loading...</div>}>
      <RegisterPage />
    </Suspense>
  );
}
