'use client';

import { createContext, useContext, useEffect, useState, type ReactNode } from 'react';
import { defaultLocale, type Locale } from '@/i18n/config';
import { getCurrentLocale } from '@/lib/locale-utils';

import esMessages from '../../messages/es.json';
import enMessages from '../../messages/en.json';
import ptMessages from '../../messages/pt.json';
import frMessages from '../../messages/fr.json';

type Messages = Record<string, Record<string, string>>;

const allMessages: Record<Locale, Messages> = {
  es: esMessages as Messages,
  en: enMessages as Messages,
  pt: ptMessages as Messages,
  fr: frMessages as Messages,
};

function translate(
  messages: Messages,
  namespace: string,
  key: string,
  params?: Record<string, string | number>
): string {
  const ns = messages[namespace];
  if (!ns) return `${namespace}.${key}`;
  const raw = ns[key];
  if (raw === undefined) return `${namespace}.${key}`;
  if (!params) return raw;
  return Object.entries(params).reduce((acc, [k, v]) => acc.replace(`{${k}}`, String(v)), raw);
}

type TFunc = (key: string, params?: Record<string, string | number>) => string;

type I18nContextType = {
  locale: Locale;
  t: (namespace: string) => TFunc;
};

const I18nContext = createContext<I18nContextType>({
  locale: defaultLocale,
  t: (namespace) => (key) => `${namespace}.${key}`,
});

export function I18nProvider({ children }: { children: ReactNode }) {
  const [locale, setLocale] = useState<Locale>(defaultLocale);

  useEffect(() => {
    setLocale(getCurrentLocale());
  }, []);

  const t = (namespace: string): TFunc => (key, params) =>
    translate(allMessages[locale], namespace, key, params);

  return <I18nContext.Provider value={{ locale, t }}>{children}</I18nContext.Provider>;
}

export function useI18n() {
  return useContext(I18nContext);
}
