'use client';

import type { ReactNode } from 'react';
import { createContext, useCallback, useEffect, useMemo, useState } from 'react';

import { readLocalStorageJson, writeLocalStorageJson } from '@/lib/storage';
import { UI_CONFIG_DEFAULTS, UI_CONFIG_STORAGE_KEY, type UiConfigState } from '@/lib/ui-config/defaults';

export type UiConfigContextType = {
  state: UiConfigState;
  setField: <K extends keyof UiConfigState>(field: K, value: UiConfigState[K]) => void;
  resetState: () => void;
};

export const UiConfigContext = createContext<UiConfigContextType | null>(null);

function clampNumber(value: unknown, min: number, max: number, fallback: number) {
  const n = typeof value === 'number' ? value : Number(value);
  if (!Number.isFinite(n)) return fallback;
  return Math.min(max, Math.max(min, n));
}

function sanitizeState(input: Partial<UiConfigState> | null | undefined): UiConfigState {
  return {
    fontFamily: typeof input?.fontFamily === 'string' && input.fontFamily.trim() ? input.fontFamily : UI_CONFIG_DEFAULTS.fontFamily,
    borderRadius: clampNumber(input?.borderRadius, 4, 24, UI_CONFIG_DEFAULTS.borderRadius),
  };
}

export function UiConfigProvider({ children }: { children: ReactNode }) {
  const [state, setState] = useState<UiConfigState>(() => {
    const stored = readLocalStorageJson<Partial<UiConfigState>>(UI_CONFIG_STORAGE_KEY);
    return sanitizeState(stored);
  });

  useEffect(() => {
    writeLocalStorageJson(UI_CONFIG_STORAGE_KEY, state);
  }, [state]);

  const setField = useCallback(<K extends keyof UiConfigState>(field: K, value: UiConfigState[K]) => {
    setState((prev) => sanitizeState({ ...prev, [field]: value }));
  }, []);

  const resetState = useCallback(() => {
    setState(UI_CONFIG_DEFAULTS);
  }, []);

  const ctxValue = useMemo<UiConfigContextType>(() => ({ state, setField, resetState }), [state, setField, resetState]);

  return <UiConfigContext.Provider value={ctxValue}>{children}</UiConfigContext.Provider>;
}
