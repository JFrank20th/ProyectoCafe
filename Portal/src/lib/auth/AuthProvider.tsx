'use client';

import React, { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react';
import type { Role, Session, SessionUser } from '@/lib/auth/types';
import { createPermissionSet, hasModuleAction, hasPermission } from '@/lib/auth/permissions';
import { normalizeRoleNames } from '@/lib/auth/roles';
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
  refreshSession: () => Promise<void>;
  signOut: () => Promise<void>;
};

const AuthContext = createContext<AuthContextValue | null>(null);

type UserPayload = {
  email: string;
  name: string;
  roles: string[];
  permissions?: string[];
};

function normalizeEmail(value: string) {
  return value.trim().toLowerCase();
}

function toSession(user: UserPayload): Session {
  return {
    user: {
      email: user.email,
      name: user.name,
      roles: normalizeRoleNames(user.roles),
      permissions: user.permissions ?? [],
    },
  };
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
        const me = await apiFetch<UserPayload>('/api/v1/auth/me');
        if (!cancelled) setSession(toSession(me));
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

    // La API responde solo con los datos del usuario; el JWT queda en la
    // cookie HttpOnly y el navegador la envía automáticamente.
    const result = await apiFetch<{ user: UserPayload }>('/api/v1/auth/login', {
      method: 'POST',
      body: JSON.stringify({ email, password }),
    });

    setSession(toSession(result.user));
    setIsLoading(false);
  }, []);

  const refreshSession = useCallback(async () => {
    // Renueva el JWT y el token CSRF sin exponerlos al JavaScript del cliente.
    const result = await apiFetch<{ user: UserPayload }>('/api/v1/auth/refresh', {
      method: 'POST',
    });

    setSession(toSession(result.user));
    setIsLoading(false);
  }, []);

  const signOut = useCallback(async () => {
    try {
      await apiFetch('/api/v1/auth/logout', { method: 'POST' });
    } catch {
      // La sesión local se limpia igualmente; la API es la autoridad final.
    }
    setSession(null);
    setIsLoading(false);
  }, []);

  const value = useMemo<AuthContextValue>(() => {
    const user = session?.user ?? null;
    const roles = user?.roles ?? [];
    const permissions = user?.permissions ?? [];
    const permissionSet = createPermissionSet(permissions);
    return {
      session,
      user,
      roles,
      permissions,
      can: (permission: string) => hasPermission(permissionSet, permission),
      has: (modulo: string, accion: string) => hasModuleAction(permissionSet, modulo, accion),
      isLoading,
      isAuthenticated: Boolean(user),
      signInWithPassword,
      refreshSession,
      signOut,
    };
  }, [session, isLoading, signInWithPassword, refreshSession, signOut]);

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used within AuthProvider');
  return ctx;
}
