'use client';

import { defaultLocale, locales, type Locale } from '@/i18n/config';

const LOCALE_COOKIE = 'NEXT_LOCALE';

export function getCurrentLocale(): Locale {
  if (typeof document === 'undefined') return defaultLocale;

  const match = document.cookie
    .split(';')
    .map((c) => c.trim())
    .find((c) => c.startsWith(`${LOCALE_COOKIE}=`));

  if (match) {
    const value = match.split('=')[1] as Locale;
    if (locales.includes(value)) return value;
  }

  return defaultLocale;
}

export function setLocale(locale: Locale): void {
  if (!locales.includes(locale)) return;
  const maxAge = 365 * 24 * 60 * 60; // 1 año
  document.cookie = `${LOCALE_COOKIE}=${locale}; path=/; max-age=${maxAge}; SameSite=Lax`;
  window.location.reload();
}
