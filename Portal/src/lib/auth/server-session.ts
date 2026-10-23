import type { SessionUser } from '@/lib/auth/types';
import { normalizePermission } from '@/lib/auth/permissions';

export const AUTH_COOKIE_NAME = process.env['PROYECTOCAFE_AUTH_COOKIE_NAME'] || 'pc_at';

export type ServerSession = {
  user: SessionUser;
};

type FetchSessionOptions = {
  apiUrl?: string;
  cookieName?: string;
  timeoutMs?: number;
  fetchImpl?: typeof fetch;
};

function getApiBaseUrl(): string {
  return (
    process.env['PROYECTOCAFE_API_URL'] ||
    process.env['NEXT_PUBLIC_PROYECTOCAFE_API_URL'] ||
    'http://localhost:5000'
  );
}

function joinUrl(base: string, path: string): string {
  return `${base.replace(/\/+$/, '')}${path.startsWith('/') ? path : `/${path}`}`;
}

function toStringArray(value: unknown): string[] {
  if (!Array.isArray(value)) return [];
  return value.filter((item): item is string => typeof item === 'string' && item.trim().length > 0);
}

function toSession(body: unknown): ServerSession | null {
  if (!body || typeof body !== 'object' || Array.isArray(body)) return null;

  const data = body as Record<string, unknown>;
  const email = data['email'];
  const name = data['name'];

  if (typeof email !== 'string' || email.trim().length === 0) return null;

  const roles = toStringArray(data['roles']) as SessionUser['roles'];
  const permissions = toStringArray(data['permissions']).map(normalizePermission);

  return {
    user: {
      email,
      name: typeof name === 'string' && name.trim().length > 0 ? name : email,
      roles,
      permissions,
    },
  };
}

/**
 * Valida el JWT contra la API usando la cookie HttpOnly.
 *
 * La API es la fuente de verdad: comprueba firma, expiración y que el usuario
 * siga activo. Cualquier error de red o respuesta no válida se trata como
 * sesión inexistente (fail closed).
 */
export async function fetchSessionFromCookie(
  token: string | null | undefined,
  options: FetchSessionOptions = {},
): Promise<ServerSession | null> {
  if (!token) return null;

  const apiUrl = options.apiUrl ?? getApiBaseUrl();
  const cookieName = options.cookieName ?? AUTH_COOKIE_NAME;
  const fetchImpl = options.fetchImpl ?? fetch;
  const timeoutMs = options.timeoutMs ?? 5000;

  const controller = new AbortController();
  const timeout = setTimeout(() => controller.abort(), timeoutMs);

  try {
    const response = await fetchImpl(joinUrl(apiUrl, '/api/v1/auth/me'), {
      method: 'GET',
      headers: {
        accept: 'application/json',
        cookie: `${cookieName}=${token}`,
      },
      cache: 'no-store',
      signal: controller.signal,
    });

    if (!response.ok) return null;

    return toSession(await response.json());
  } catch {
    return null;
  } finally {
    clearTimeout(timeout);
  }
}
