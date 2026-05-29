'use client';

import { useMemo } from 'react';

import { useI18n } from '@/providers/i18n-provider';

export function useTranslations(namespace: string = 'common') {
  const { t } = useI18n();
  return useMemo(() => t(namespace), [t, namespace]);
}

export function useLocale() {
  const { locale } = useI18n();
  return locale;
}
