'use client';

import React, { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react';
import type { Role, Session, SessionUser } from '@/lib/auth/types';
import { apiFetch } from '@/lib/api/client';

type AuthContextValue = {
  session: Session | null;
  user: SessionUser | null;
  roles: Role[];
  permissions: string[];
  can: (permission: string) => boolean;
  has: (modulo: string, accion: string) => boolean;
  isLoading: boolean;
  isAuthenticated: boolean;
  signInWithPassword: (email: string, password: string) => Promise<void>;
  signOut: () => void;
};

const AuthContext = createContext<AuthContextValue | null>(null);

function normalizeEmail(value: string) {
  return value.trim().toLowerCase();
}

function normalizePermission(value: string) {
  return value.trim().toLowerCase();
}

function buildPermission(modulo: string, accion: string) {
  return `perm:${modulo}:${accion}`;
}

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [session, setSession] = useState<Session | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    // Cookie-based auth: hydrate session via /me.
    let cancelled = false;

    void (async () => {
      setIsLoading(true);
      try {
        const me = await apiFetch<{ email: string; name: string; roles: string[]; permissions?: string[] }>(
          '/api/v1/auth/me'
        );
        const next: Session = {
          accessToken: null,
          user: {
            email: me.email,
            name: me.name,
            roles: (me.roles as Role[]) ?? [],
            permissions: me.permissions ?? [],
          },
        };
        if (!cancelled) setSession(next);
      } catch {
        if (!cancelled) setSession(null);
      } finally {
        if (!cancelled) setIsLoading(false);
      }
    })();

    return () => {
      cancelled = true;
    };
  }, []);

  const signInWithPassword = useCallback(async (emailRaw: string, passwordRaw: string) => {
    const email = normalizeEmail(emailRaw);
    const password = passwordRaw.trim();

    const result = await apiFetch<{
      accessToken?: string;
      user: { email: string; name: string; roles: string[]; permissions?: string[] };
    }>('/api/v1/auth/login', {
      method: 'POST',
      body: JSON.stringify({ email, password }),
    });

    const next: Session = {
      // Token now lives in HttpOnly cookie; keep null in client state.
      accessToken: null,
      user: {
        email: result.user.email,
        name: result.user.name,
        roles: (result.user.roles as Role[]) ?? [],
        permissions: result.user.permissions ?? [],
      },
    };
    setSession(next);
    setIsLoading(false);
  }, []);

  const signOut = useCallback(() => {
    void apiFetch('/api/v1/auth/logout', { method: 'POST' }).catch(() => {});
    setSession(null);
    setIsLoading(false);
  }, []);

  const value = useMemo<AuthContextValue>(() => {
    const user = session?.user ?? null;
    const roles = user?.roles ?? [];
    const permissions = user?.permissions ?? [];
    const permissionSet = new Set(permissions.map(normalizePermission));
    return {
      session,
      user,
      roles,
      permissions,
      can: (permission: string) => permissionSet.has(normalizePermission(permission)),
      has: (modulo: string, accion: string) => permissionSet.has(normalizePermission(buildPermission(modulo, accion))),
      isLoading,
      isAuthenticated: Boolean(user),
      signInWithPassword,
      signOut,
    };
  }, [session, isLoading, signInWithPassword, signOut]);

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used within AuthProvider');
  return ctx;
}
