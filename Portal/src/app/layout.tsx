import type { Metadata } from 'next';
import './globals.css';
import '@fontsource/inter/400.css';
import '@fontsource/inter/500.css';
import '@fontsource/inter/600.css';
import '@fontsource/inter/700.css';
import '@fontsource/poppins/400.css';
import '@fontsource/poppins/500.css';
import '@fontsource/poppins/600.css';
import '@fontsource/poppins/700.css';
import '@fontsource/roboto/300.css';
import '@fontsource/roboto/400.css';
import '@fontsource/roboto/500.css';
import '@fontsource/roboto/700.css';

import { AppRouterCacheProvider } from '@mui/material-nextjs/v15-appRouter';
import { AppProviders } from './providers';

export const dynamic = 'force-dynamic';
export const revalidate = 0;

export const metadata: Metadata = {
  title: process.env.NEXT_PUBLIC_APP_NAME ?? 'ProyectoCafe',
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  const runtimeConfig = {
    API_URL:
      process.env['PROYECTOCAFE_API_URL'] ||
      process.env['NEXT_PUBLIC_PROYECTOCAFE_API_URL'] ||
      'http://localhost:5000',
    APP_NAME: process.env['APP_NAME'] || process.env['NEXT_PUBLIC_APP_NAME'] || 'ProyectoCafe',
    APP_VERSION:
      process.env['NEXT_PUBLIC_APP_VERSION'] || process.env['APP_VERSION'] || '0.1.0',
  };

  return (
    <html lang="es" suppressHydrationWarning>
      <head>
        <script
          dangerouslySetInnerHTML={{
            __html: `window.__RUNTIME_ENV__ = ${JSON.stringify(runtimeConfig)}`,
          }}
        />
      </head>
      <body>
        <AppRouterCacheProvider options={{ key: 'mui' }}>
          <AppProviders>{children}</AppProviders>
        </AppRouterCacheProvider>
      </body>
    </html>
  );
}
