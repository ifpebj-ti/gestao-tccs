import type { Metadata } from 'next';
import { headers } from 'next/headers';
import { PublicEnvScript } from 'next-runtime-env';

import { ToastContainer } from 'react-toastify';
import 'react-toastify/dist/ReactToastify.css';

import { config } from '@fortawesome/fontawesome-svg-core';
import '@fortawesome/fontawesome-svg-core/styles.css';
config.autoAddCss = false;

import './globals.css';
import VersionDisplay from '@/components/VersionDisplay';

export const metadata: Metadata = {
  title: 'Gradus Flow - Gestão de TCCs',
  description: 'Feito por alunos do IFPE - Campus Belo Jardim 2025',
  icons: {
    icon: '/icon.svg',
    shortcut: '/icon.svg',
    apple: '/icon.svg'
  }
};

export default async function RootLayout({
  children
}: Readonly<{
  children: React.ReactNode;
}>) {
  const nonce = (await headers()).get('x-nonce') ?? undefined;
  return (
    <html lang="pt-br">
      <head>
        <PublicEnvScript nonce={nonce} />
      </head>
      <body className="antialiased">
        {children}
        <ToastContainer
          nonce={nonce}
          position="bottom-right"
          autoClose={3000}
          hideProgressBar={false}
          newestOnTop={false}
          closeOnClick
          rtl={false}
          pauseOnFocusLoss
          draggable
          pauseOnHover
        />
        <VersionDisplay />
      </body>
    </html>
  );
}
