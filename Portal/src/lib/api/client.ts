import { getRuntimeConfig } from '@/lib/runtime-config';
import { buildLoginRedirect, isPublicRoute } from '@/lib/routes';

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

function isRecord(value: unknown): value is Record<string, unknown> {
  return Boolean(value) && typeof value === 'object' && !Array.isArray(value);
}

function describeErrorBody(bodyText: string | undefined, fallback: string) {
  if (!bodyText) {
    return fallback;
  }

  try {
    const data: unknown = JSON.parse(bodyText);
    if (!isRecord(data)) {
      return fallback;
    }

    const maybeError = data.error;
    if (isRecord(maybeError)) {
      const message = maybeError.message;
      if (typeof message === 'string' && message.trim()) {
        return message;
      }
    }

    const maybeMessage = data.message;
    if (typeof maybeMessage === 'string' && maybeMessage.trim()) {
      return maybeMessage;
    }

    const maybeErrors = data.errors;
    if (isRecord(maybeErrors)) {
      for (const value of Object.values(maybeErrors)) {
        if (!Array.isArray(value)) {
          continue;
        }

        const first = value.find((entry) => typeof entry === 'string' && entry.trim());
        if (typeof first === 'string') {
          return first;
        }
      }
    }

    const maybeDetail = data.detail;
    if (typeof maybeDetail === 'string' && maybeDetail.trim()) {
      return maybeDetail;
    }

    const maybeTitle = data.title;
    if (typeof maybeTitle === 'string' && maybeTitle.trim()) {
      return maybeTitle;
    }
  } catch {
    return fallback;
  }

  return fallback;
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
    const fallbackMessage = response.status === 401
      ? 'No autorizado.'
      : response.status === 403
        ? 'No tienes permisos.'
        : `Error HTTP ${response.status}.`;
    const message = response.status === 401 || response.status === 403
      ? fallbackMessage
      : describeErrorBody(bodyText, fallbackMessage);

    if (response.status === 401) {
      if (typeof window !== 'undefined') {
        const pathname = window.location.pathname;
        const current = pathname + window.location.search;
        if (!isPublicRoute(pathname)) {
          window.location.replace(buildLoginRedirect(current));
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
