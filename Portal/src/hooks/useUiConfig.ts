'use client';

import { useContext } from 'react';

import { UiConfigContext } from '@/providers/ui-config-provider';

export function useUiConfig() {
  const ctx = useContext(UiConfigContext);
  if (!ctx) throw new Error('useUiConfig must be used within UiConfigProvider');
  return ctx;
}
