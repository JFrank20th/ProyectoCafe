'use client';

import Link from 'next/link';
import { ShieldAlert } from 'lucide-react';

import { useTranslations } from '@/hooks/useTranslations';
import { HOME_ROUTE } from '@/lib/routes';

export default function AccessDeniedPage() {
  const tp = useTranslations('pages');
  const tn = useTranslations('navigation');

  return (
    <div className="flex min-h-[60vh] flex-col items-center justify-center gap-4 p-6 text-center">
      <ShieldAlert className="h-12 w-12 text-amber-500" aria-hidden="true" />
      <h1 className="text-2xl font-semibold">{tp('accessDenied')}</h1>
      <p className="max-w-md text-sm text-muted-foreground">{tp('noPermission')}</p>
      <Link
        href={HOME_ROUTE}
        className="rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground"
      >
        {tn('dashboard')}
      </Link>
    </div>
  );
}
