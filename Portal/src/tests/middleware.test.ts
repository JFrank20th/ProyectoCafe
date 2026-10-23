import assert from 'node:assert/strict';
import { afterEach, test } from 'node:test';

import { NextRequest } from 'next/server';

import { middleware } from '@/middleware';

const originalFetch = globalThis.fetch;

type SessionBody = {
  email?: string;
  name?: string;
  roles?: string[];
  permissions?: string[];
};

function request(path: string, cookie?: string) {
  return new NextRequest(new URL(path, 'http://localhost:3010'), {
    headers: cookie ? { cookie } : undefined,
  });
}

function mockMe(body: SessionBody = {}, status = 200) {
  let calls = 0;

  globalThis.fetch = (async () => {
    calls += 1;
    return new Response(JSON.stringify(body), {
      status,
      headers: { 'content-type': 'application/json' },
    });
  }) as typeof fetch;

  return () => calls;
}

afterEach(() => {
  globalThis.fetch = originalFetch;
});

test('public routes pass through without validating a session', async () => {
  const calls = mockMe();

  const response = await middleware(request('/login?returnUrl=%2Fdashboard'));

  assert.equal(response.status, 200);
  assert.equal(response.headers.get('x-middleware-next'), '1');
  assert.equal(calls(), 0);
});

test('unauthenticated page requests redirect to login preserving the full path', async () => {
  const calls = mockMe();

  const response = await middleware(request('/administracion/roles?page=2'));

  assert.equal(response.status, 307);
  assert.equal(
    response.headers.get('location'),
    'http://localhost:3010/login?returnUrl=%2Fadministracion%2Froles%3Fpage%3D2',
  );
  assert.equal(calls(), 0);
});

test('invalid session cookies redirect to login and are cleared', async () => {
  mockMe({}, 401);

  const response = await middleware(request('/dashboard', 'pc_at=expired-token'));

  assert.equal(response.status, 307);
  assert.equal(
    response.headers.get('location'),
    'http://localhost:3010/login?returnUrl=%2Fdashboard',
  );
  assert.match(response.headers.get('set-cookie') ?? '', /pc_at=;/);
});

test('authenticated users with the required permission reach the page', async () => {
  mockMe({
    email: 'admin@local',
    name: 'Admin',
    roles: ['Admin'],
    permissions: ['perm:Administracion.Usuarios:Ver'],
  });

  const response = await middleware(request('/administracion/usuarios', 'pc_at=valid-token'));

  assert.equal(response.status, 200);
  assert.equal(response.headers.get('x-middleware-next'), '1');
});

test('authenticated users without the required permission go to access denied', async () => {
  mockMe({
    email: 'user@local',
    name: 'User',
    roles: ['User'],
    permissions: ['perm:Administracion.Roles:Ver'],
  });

  const response = await middleware(request('/administracion/usuarios', 'pc_at=valid-token'));

  assert.equal(response.status, 307);
  assert.equal(
    response.headers.get('location'),
    'http://localhost:3010/acceso-denegado',
  );
});

test('Portal API routes return JSON 401 instead of redirecting', async () => {
  mockMe();

  const response = await middleware(request('/api/auth/session'));

  assert.equal(response.status, 401);
  assert.equal(response.headers.get('cache-control'), 'no-store');
  assert.deepEqual(await response.json(), {
    error: { code: 'UNAUTHORIZED', message: 'No autorizado.' },
  });
});

test('Portal API routes accept a valid session without cookie redirects', async () => {
  mockMe({
    email: 'admin@local',
    name: 'Admin',
    roles: ['Admin'],
    permissions: [],
  });

  const response = await middleware(request('/api/auth/session', 'pc_at=valid-token'));

  assert.equal(response.status, 200);
  assert.equal(response.headers.get('x-middleware-next'), '1');
});

test('Portal API routes clear invalid session cookies', async () => {
  mockMe({}, 401);

  const response = await middleware(request('/api/auth/session', 'pc_at=expired-token'));

  assert.equal(response.status, 401);
  assert.match(response.headers.get('set-cookie') ?? '', /pc_at=;/);
});
