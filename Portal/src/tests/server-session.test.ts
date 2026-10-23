import assert from 'node:assert/strict';
import test from 'node:test';

import { fetchSessionFromCookie, type ServerSession } from '@/lib/auth/server-session';

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
