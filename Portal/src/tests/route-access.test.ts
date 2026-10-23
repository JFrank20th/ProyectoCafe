import assert from 'node:assert/strict';
import test from 'node:test';

import {
  canAccessRoute,
  getRouteAccess,
  hasRoutePermission,
  PERMISSION_ROUTES,
} from '@/lib/auth/route-access';
import { createPermissionSet } from '@/lib/auth/permissions';

test('public routes do not require a session', () => {
  assert.deepEqual(getRouteAccess('/login'), { access: 'public' });
  assert.deepEqual(getRouteAccess('/register/'), { access: 'public' });
});

test('protected routes require a session by default', () => {
  assert.deepEqual(getRouteAccess('/dashboard'), { access: 'protected' });
  assert.deepEqual(getRouteAccess('/'), { access: 'protected' });
  assert.deepEqual(getRouteAccess('/modulo/nuevo'), { access: 'protected' });
});

test('administration routes require their read permission', () => {
  const usuarios = getRouteAccess('/administracion/usuarios');
  const roles = getRouteAccess('/administracion/roles');

  assert.equal(usuarios.access, 'protected');
  assert.equal(usuarios.access === 'protected' ? usuarios.permission : null, 'perm:Administracion.Usuarios:Ver');

  assert.equal(roles.access, 'protected');
  assert.equal(roles.access === 'protected' ? roles.permission : null, 'perm:Administracion.Roles:Ver');

  assert.deepEqual(getRouteAccess('/administracion/usuarios/123'), usuarios);
  assert.deepEqual(getRouteAccess('/administracion/usuariosx'), { access: 'protected' });
});

test('permission routes stay in sync with the declared route map', () => {
  for (const route of PERMISSION_ROUTES) {
    const access = getRouteAccess(route.path);
    assert.equal(access.access, 'protected');
    assert.equal(access.access === 'protected' ? access.permission : null, route.permission);
  }
});

test('hasRoutePermission is case-insensitive and denies missing permissions', () => {
  const access = getRouteAccess('/administracion/usuarios');

  assert.equal(hasRoutePermission(createPermissionSet(['PERM:ADMINISTRACION.USUARIOS:VER']), access), true);
  assert.equal(hasRoutePermission(createPermissionSet(['perm:Administracion.Roles:Ver']), access), false);
  assert.equal(hasRoutePermission(createPermissionSet([]), access), false);
});

test('canAccessRoute combines session permissions with route policy', () => {
  assert.equal(canAccessRoute(['perm:Administracion.Roles:Ver'], '/administracion/roles'), true);
  assert.equal(canAccessRoute(['perm:Administracion.Roles:Ver'], '/administracion/usuarios'), false);
  assert.equal(canAccessRoute([], '/dashboard'), true);
  assert.equal(canAccessRoute([], '/login'), true);
});
