
import { SetNewPasswordPage } from '../components/SetNewPasswordPage';
import { Suspense } from 'react';

export default function SetNewPassword() {
    return (
        <Suspense fallback={<div>Loading...</div>}>
            <SetNewPasswordPage />
        </Suspense>
   );
}
