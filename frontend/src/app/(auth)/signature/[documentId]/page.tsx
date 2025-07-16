import { Suspense } from 'react';
import SignatureClient from './SignatureClient';

export default function SignaturePage() {
  return (
    <Suspense fallback={<div>Loading...</div>}>
      <SignatureClient />
    </Suspense>
  );
}
