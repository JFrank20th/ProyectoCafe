import assert from 'node:assert/strict';
import { afterEach, test } from 'node:test';

import { ApiError, apiFetch } from '@/lib/api/client';

type FetchCall = {
  url: string;
  init: RequestInit;
};

const originalFetch = globalThis.fetch;
const originalWindow = (globalThis as { window?: unknown }).window;
const originalDocument = (globalThis as { document?: unknown }).document;

let calls: FetchCall[] = [];
let redirects: string[] = [];

function installBrowser(options: {
  pathname?: string;
  search?: string;
  cookie?: string;
} = {}) {
  calls = [];
  redirects = [];

  (globalThis as { window?: unknown }).window = {
    __RUNTIME_ENV__: {
      API_URL: 'https://api.test/',
      APP_NAME: 'ProyectoCafe',
      APP_VERSION: 'test',
    },
    location: {
      pathname: options.pathname ?? '/dashboard',
      search: options.search ?? '',
      replace: (url: string) => {
        redirects.push(url);
      },
    },
  };

  (globalThis as { document?: unknown }).document = {
    cookie: options.cookie ?? 'pc_csrf=csrf-token',
  };
}

function respondWith(body: BodyInit | null, init: ResponseInit): void {
  globalThis.fetch = async (input, requestInit) => {
    calls.push({ url: String(input), init: requestInit ?? {} });
    return new Response(body, init);
  };
}

afterEach(() => {
  globalThis.fetch = originalFetch;
  (globalThis as { window?: unknown }).window = originalWindow;
  (globalThis as { document?: unknown }).document = originalDocument;
});

test('apiFetch builds the URL, sends credentials and the CSRF header on mutations', async () => {
  installBrowser();
  respondWith(JSON.stringify({ ok: true }), {
    status: 200,
    headers: { 'content-type': 'application/json' },
  });

  const result = await apiFetch<{ ok: boolean }>('/api/v1/admin/roles', {
    method: 'POST',
    body: JSON.stringify({ nombre: 'Operador' }),
  });

  assert.deepEqual(result, { ok: true });
  assert.equal(calls.length, 1);
  assert.equal(calls[0].url, 'https://api.test/api/v1/admin/roles');
  assert.equal(calls[0].init.credentials, 'include');
  assert.equal(calls[0].init.cache, 'no-store');

  const headers = new Headers(calls[0].init.headers);
  assert.equal(headers.get('Content-Type'), 'application/json');
  assert.equal(headers.get('X-CSRF'), 'csrf-token');
});

test('apiFetch does not send CSRF on safe methods', async () => {
  installBrowser();
  respondWith(JSON.stringify({ ok: true }), {
    status: 200,
    headers: { 'content-type': 'application/json' },
  });

  await apiFetch('/api/v1/auth/me');

  const headers = new Headers(calls[0].init.headers);
  assert.equal(headers.get('X-CSRF'), null);
});

test('apiFetch resolves undefined for 204 responses', async () => {
  installBrowser();
  respondWith(null, { status: 204 });

  const result = await apiFetch<void>('/api/v1/admin/users/1/activate', { method: 'POST' });

  assert.equal(result, undefined);
});

test('apiFetch surfaces nested API error messages', async () => {
  installBrowser();
  respondWith(JSON.stringify({ error: { code: 'INVALID_CREDENTIALS', message: 'Credenciales inválidas.' } }), {
    status: 401,
    headers: { 'content-type': 'application/json' },
  });

  await assert.rejects(
    () => apiFetch('/api/v1/auth/login', { method: 'POST', body: '{}' }),
    (error: unknown) => {
      assert.ok(error instanceof ApiError);
      assert.equal(error.status, 401);
      assert.equal(error.message, 'No autorizado.');
      assert.match(error.bodyText ?? '', /INVALID_CREDENTIALS/);
      return true;
    },
  );
});

test('apiFetch redirects to login on 401 for protected routes', async () => {
  installBrowser({ pathname: '/administracion/roles', search: '?page=2' });
  respondWith(null, { status: 401 });

  await assert.rejects(() => apiFetch('/api/v1/admin/roles'));

  assert.deepEqual(redirects, ['/login?returnUrl=%2Fadministracion%2Froles%3Fpage%3D2']);
});

test('apiFetch does not redirect on 401 for public routes', async () => {
  installBrowser({ pathname: '/login', search: '?returnUrl=%2Fdashboard' });
  respondWith(null, { status: 401 });

  await assert.rejects(() => apiFetch('/api/v1/auth/login', { method: 'POST' }));

  assert.deepEqual(redirects, []);
});

test('apiFetch returns readable messages for validation problems and server errors', async () => {
  installBrowser();
  respondWith(JSON.stringify({ errors: { Email: ['Este email ya está registrado.'] } }), {
    status: 400,
    headers: { 'content-type': 'application/json' },
  });

  await assert.rejects(
    () => apiFetch('/api/v1/auth/register', { method: 'POST', body: '{}' }),
    (error: unknown) => {
      assert.ok(error instanceof ApiError);
      assert.equal(error.message, 'Este email ya está registrado.');
      return true;
    },
  );

  installBrowser();
  respondWith('boom', { status: 500, headers: { 'content-type': 'text/plain' } });

  await assert.rejects(
    () => apiFetch('/api/v1/health'),
    (error: unknown) => {
      assert.ok(error instanceof ApiError);
      assert.equal(error.message, 'Error HTTP 500.');
      return true;
    },
  );
});

test('apiFetch returns plain text when the response is not JSON', async () => {
  installBrowser();
  respondWith('ok', { status: 200, headers: { 'content-type': 'text/plain' } });

  const result = await apiFetch<string>('/health');

  assert.equal(result, 'ok');
});
