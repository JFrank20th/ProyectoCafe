import assert from 'node:assert/strict';
import { afterEach, test } from 'node:test';

import { NextRequest } from 'next/server';

import { requireApiPermission, requireApiSession } from '@/lib/auth/api-guard';

const originalFetch = globalThis.fetch;

function request(cookie?: string) {
  return new NextRequest(new URL('/api/admin/test', 'http://localhost:3010'), {
    headers: cookie ? { cookie } : undefined,
  });
}

function mockMe(permissions: string[], status = 200) {
  globalThis.fetch = (async () =>
    new Response(
      JSON.stringify({
        email: 'admin@local',
        name: 'Admin',
        roles: ['Admin'],
        permissions,
      }),
      { status, headers: { 'content-type': 'application/json' } },
    )) as typeof fetch;
}

afterEach(() => {
  globalThis.fetch = originalFetch;
});

test('requireApiSession rejects requests without a session', async () => {
  const result = await requireApiSession(request());

  assert.equal(result.session, null);
  assert.equal(result.response?.status, 401);
  assert.deepEqual(await result.response?.json(), {
    error: { code: 'UNAUTHORIZED', message: 'No autorizado.' },
  });
});

test('requireApiSession rejects sessions the API does not recognize', async () => {
  mockMe([], 401);

  const result = await requireApiSession(request('pc_at=expired-token'));

  assert.equal(result.session, null);
  assert.equal(result.response?.status, 401);
});

test('requireApiSession returns the validated session', async () => {
  mockMe(['perm:Administracion.Usuarios:Ver']);

  const result = await requireApiSession(request('pc_at=valid-token'));

  assert.equal(result.response, null);
  assert.equal(result.session?.user.email, 'admin@local');
});

test('requireApiPermission returns 403 when the role lacks the permission', async () => {
  mockMe(['perm:Administracion.Roles:Ver']);

  const result = await requireApiPermission(
    request('pc_at=valid-token'),
    'Administracion.Usuarios',
    'Ver',
  );

  assert.equal(result.session, null);
  assert.equal(result.response?.status, 403);
  assert.deepEqual(await result.response?.json(), {
    error: { code: 'FORBIDDEN', message: 'No tienes permisos.' },
  });
});

test('requireApiPermission allows operations with the required permission', async () => {
  mockMe(['perm:Administracion.Usuarios:Ver']);

  const result = await requireApiPermission(
    request('pc_at=valid-token'),
    'Administracion.Usuarios',
    'Ver',
  );

  assert.equal(result.response, null);
  assert.ok(result.session);
});
