'use client';

import type { ReactNode } from 'react';
import { useEffect } from 'react';
import { usePathname, useRouter } from 'next/navigation';

import { SessionGuard } from '@/components/auth/SessionGuard';
import { MainLayout } from '@/components/layout/main-layout';
import { useTranslations } from '@/hooks/useTranslations';
import { AuthProvider, useAuth } from '@/lib/auth/AuthProvider';
import { isPublicRoute as isPublicPath, LOGIN_ROUTE } from '@/lib/routes';
import { I18nProvider } from '@/providers/i18n-provider';
import { UiConfigProvider } from '@/providers/ui-config-provider';
import { BerryThemeProvider } from '@/theme/BerryThemeProvider';

export function AppProviders({ children }: { children: ReactNode }) {
  return (
    <UiConfigProvider>
      <BerryThemeProvider>
        <I18nProvider>
          <AuthProvider>
            <SessionGuard>
              <AppShell>{children}</AppShell>
            </SessionGuard>
          </AuthProvider>
        </I18nProvider>
      </BerryThemeProvider>
    </UiConfigProvider>
  );
}

function AppShell({ children }: { children: ReactNode }) {
  const pathname = usePathname();
  const router = useRouter();
  const { isAuthenticated, isLoading, user, signOut } = useAuth();
  const t = useTranslations('common');

  const isPublicRoute = isPublicPath(pathname);

  useEffect(() => {
    if (isPublicRoute) return;
    if (isLoading) return;
    if (!isAuthenticated) {
      router.replace(LOGIN_ROUTE);
    }
  }, [isAuthenticated, isLoading, isPublicRoute, router]);

  if (isPublicRoute) {
    return children;
  }

  if (isLoading) {
    return <div className="p-4 text-sm text-muted-foreground">{t('loading')}</div>;
  }

  if (!isAuthenticated) {
    return null;
  }

  return (
    <MainLayout user={user ?? undefined} onLogout={signOut}>
      {children}
    </MainLayout>
  );
}
