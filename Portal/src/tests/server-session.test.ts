import assert from 'node:assert/strict';
import test from 'node:test';

import { fetchSessionFromCookie, getServerApiBaseUrl, type ServerSession } from '@/lib/auth/server-session';

const API_URL_ENV_KEYS = [
  'PROYECTOCAFE_API_URL_INTERNAL',
  'PROYECTOCAFE_API_URL',
  'NEXT_PUBLIC_PROYECTOCAFE_API_URL',
] as const;

type ApiUrlEnvKey = (typeof API_URL_ENV_KEYS)[number];

function snapshotApiUrlEnv(): Partial<Record<ApiUrlEnvKey, string>> {
  const snapshot: Partial<Record<ApiUrlEnvKey, string>> = {};

  for (const key of API_URL_ENV_KEYS) {
    const value = process.env[key];
    if (value !== undefined) snapshot[key] = value;
    delete process.env[key];
  }

  return snapshot;
}

function restoreApiUrlEnv(snapshot: Partial<Record<ApiUrlEnvKey, string>>): void {
  for (const key of API_URL_ENV_KEYS) {
    const value = snapshot[key];
    if (value === undefined) delete process.env[key];
    else process.env[key] = value;
  }
}

type FetchCall = {
  url: string;
  init: RequestInit;
};

function jsonResponse(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'content-type': 'application/json' },
  });
}

function fakeFetch(response: Response | (() => Promise<Response>)) {
  const calls: FetchCall[] = [];

  const fetchImpl = (async (input: RequestInfo | URL, init?: RequestInit) => {
    calls.push({ url: String(input), init: init ?? {} });
    return typeof response === 'function' ? response() : response;
  }) as typeof fetch;

  return { calls, fetchImpl };
}

test('getServerApiBaseUrl prefers the internal URL over the public one', () => {
  const snapshot = snapshotApiUrlEnv();

  try {
    process.env['PROYECTOCAFE_API_URL'] = 'http://localhost:5000';
    assert.equal(getServerApiBaseUrl(), 'http://localhost:5000');

    process.env['PROYECTOCAFE_API_URL_INTERNAL'] = 'http://api:8080';
    assert.equal(getServerApiBaseUrl(), 'http://api:8080');
  } finally {
    restoreApiUrlEnv(snapshot);
  }
});

test('getServerApiBaseUrl keeps the public fallback chain when the internal URL is absent', () => {
  const snapshot = snapshotApiUrlEnv();

  try {
    process.env['NEXT_PUBLIC_PROYECTOCAFE_API_URL'] = 'https://public.example';
    assert.equal(getServerApiBaseUrl(), 'https://public.example');

    process.env['PROYECTOCAFE_API_URL'] = 'https://runtime.example';
    assert.equal(getServerApiBaseUrl(), 'https://runtime.example');

    delete process.env['PROYECTOCAFE_API_URL'];
    delete process.env['NEXT_PUBLIC_PROYECTOCAFE_API_URL'];
    assert.equal(getServerApiBaseUrl(), 'http://localhost:5000');
  } finally {
    restoreApiUrlEnv(snapshot);
  }
});

test('fetchSessionFromCookie validates the session against the internal URL', async () => {
  const snapshot = snapshotApiUrlEnv();

  try {
    process.env['PROYECTOCAFE_API_URL'] = 'http://localhost:5000';
    process.env['PROYECTOCAFE_API_URL_INTERNAL'] = 'http://api:8080/';

    const { calls, fetchImpl } = fakeFetch(
      jsonResponse({ email: 'admin@local', roles: [], permissions: [] }),
    );

    await fetchSessionFromCookie('token', { fetchImpl });

    assert.equal(calls[0].url, 'http://api:8080/api/v1/auth/me');
  } finally {
    restoreApiUrlEnv(snapshot);
  }
});

test('fetchSessionFromCookie validates the session against /auth/me', async () => {
  const { calls, fetchImpl } = fakeFetch(
    jsonResponse({
      email: 'admin@local',
      name: 'Admin',
      roles: ['Admin'],
      permissions: ['PERM:Administracion.Usuarios:Ver'],
    }),
  );

  const session = await fetchSessionFromCookie('token-123', {
    apiUrl: 'https://api.test/',
    cookieName: 'pc_at',
    fetchImpl,
  });

  assert.ok(session);
  assert.equal(session.user.email, 'admin@local');
  assert.equal(session.user.name, 'Admin');
  assert.deepEqual(session.user.roles, ['Admin']);
  assert.deepEqual(session.user.permissions, ['perm:administracion.usuarios:ver']);

  assert.equal(calls.length, 1);
  assert.equal(calls[0].url, 'https://api.test/api/v1/auth/me');
  assert.equal(calls[0].init.method, 'GET');
  assert.equal(calls[0].init.cache, 'no-store');

  const headers = new Headers(calls[0].init.headers);
  assert.equal(headers.get('cookie'), 'pc_at=token-123');
  assert.equal(headers.get('accept'), 'application/json');
});

test('fetchSessionFromCookie keeps dynamic backend role names', async () => {
  const { fetchImpl } = fakeFetch(
    jsonResponse({
      email: 'soporte@local',
      name: 'Soporte',
      roles: ['  Soporte administrativo  ', 'Auditoría', 'soporte ADMINISTRATIVO'],
      permissions: ['perm:Administracion.Usuarios:Ver'],
    }),
  );

  const session = await fetchSessionFromCookie('token', { fetchImpl });

  assert.deepEqual(session?.user.roles, ['Soporte administrativo', 'Auditoría']);
});

test('fetchSessionFromCookie falls back to email when name is missing', async () => {
  const { fetchImpl } = fakeFetch(jsonResponse({ email: 'user@local', roles: [], permissions: [] }));

  const session = await fetchSessionFromCookie('token', { fetchImpl });

  assert.equal(session?.user.name, 'user@local');
});

test('fetchSessionFromCookie returns null without a token', async () => {
  const { calls, fetchImpl } = fakeFetch(jsonResponse({}));

  assert.equal(await fetchSessionFromCookie(null, { fetchImpl }), null);
  assert.equal(await fetchSessionFromCookie('', { fetchImpl }), null);
  assert.equal(calls.length, 0);
});

test('fetchSessionFromCookie treats 401/403/500 as an invalid session', async () => {
  for (const status of [401, 403, 500]) {
    const { fetchImpl } = fakeFetch(jsonResponse({ error: 'nope' }, status));
    assert.equal(await fetchSessionFromCookie('token', { fetchImpl }), null);
  }
});

test('fetchSessionFromCookie fail-closes on network or parsing errors', async () => {
  const network = fakeFetch(() => Promise.reject(new Error('offline')));
  assert.equal(await fetchSessionFromCookie('token', { fetchImpl: network.fetchImpl }), null);

  const invalidJson = fakeFetch(new Response('not-json', { status: 200 }));
  assert.equal(await fetchSessionFromCookie('token', { fetchImpl: invalidJson.fetchImpl }), null);

  const missingEmail = fakeFetch(jsonResponse({ roles: [], permissions: [] }));
  assert.equal(await fetchSessionFromCookie('token', { fetchImpl: missingEmail.fetchImpl }), null);
});

test('ServerSession keeps the expected shape', () => {
  const session: ServerSession = {
    user: {
      email: 'admin@local',
      name: 'Admin',
      roles: ['Admin'],
      permissions: ['perm:Administracion.Usuarios:Ver'],
    },
  };

  assert.equal(session.user.permissions.length, 1);
});
