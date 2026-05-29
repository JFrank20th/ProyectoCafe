import { getRuntimeConfig } from '@/lib/runtime-config';

function joinUrl(base: string, path: string) {
  const trimmedBase = base.replace(/\/+$/, '');
  const trimmedPath = path.startsWith('/') ? path : `/${path}`;
  return `${trimmedBase}${trimmedPath}`;
}

export class ApiError extends Error {
  status: number;
  bodyText?: string;

  constructor(message: string, status: number, bodyText?: string) {
    super(message);
    this.status = status;
    this.bodyText = bodyText;
  }
}

export async function apiFetch<T>(path: string, init?: RequestInit): Promise<T> {
  const { API_URL } = getRuntimeConfig();
  const url = joinUrl(API_URL, path);

  const headers = new Headers(init?.headers);
  if (!headers.has('Content-Type') && init?.body) {
    headers.set('Content-Type', 'application/json');
  }

  // Cookie-based auth: token is sent automatically by browser.
  // CSRF protection: for non-idempotent methods, send X-CSRF header.
  const method = (init?.method ?? 'GET').toUpperCase();
  const isUnsafe = !(method === 'GET' || method === 'HEAD' || method === 'OPTIONS');
  if (isUnsafe && !headers.has('X-CSRF')) {
    const csrf = readCookie('pc_csrf');
    if (csrf) headers.set('X-CSRF', csrf);
  }

  const response = await fetch(url, {
    ...init,
    headers,
    cache: 'no-store',
    credentials: 'include',
  });

  if (response.status === 204) {
    return undefined as unknown as T;
  }

  const contentType = response.headers.get('content-type') ?? '';
  const isJson = contentType.includes('application/json');

  if (!response.ok) {
    const bodyText = await response.text().catch(() => '');
    const message = response.status === 401
      ? 'No autorizado.'
      : response.status === 403
        ? 'No tienes permisos.'
        : `Error HTTP ${response.status}.`;

    if (response.status === 401) {
      if (typeof window !== 'undefined') {
        const pathname = window.location.pathname;
        const current = pathname + window.location.search;
        const isPublicRoute = pathname === '/login' || pathname === '/register';
        if (!isPublicRoute) {
          window.location.replace(`/login?returnUrl=${encodeURIComponent(current)}`);
        }
      }
    }

    throw new ApiError(message, response.status, bodyText);
  }

  if (isJson) {
    return (await response.json()) as T;
  }

  // Fallback for unexpected non-json
  return (await response.text()) as unknown as T;
}

function readCookie(name: string): string | null {
  if (typeof document === 'undefined') return null;
  const match = document.cookie.match(new RegExp(`(?:^|; )${escapeRegExp(name)}=([^;]*)`));
  return match ? decodeURIComponent(match[1]) : null;
}

function escapeRegExp(value: string) {
  return value.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}
