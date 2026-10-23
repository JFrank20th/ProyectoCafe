import assert from 'node:assert/strict';
import test from 'node:test';

import {
  PERMISSION_PREFIX,
  buildPermission,
  createPermissionSet,
  hasModuleAction,
  hasPermission,
  normalizePermission,
} from '@/lib/auth/permissions';

test('normalizePermission trim and lowercases values', () => {
  assert.equal(normalizePermission('  PERM:Administracion.Usuarios:Ver  '), 'perm:administracion.usuarios:ver');
});

test('buildPermission creates the backend-compatible key', () => {
  assert.equal(
    buildPermission(' Administracion.Roles ', ' Editar '),
    `${PERMISSION_PREFIX}Administracion.Roles:Editar`,
  );
});

test('createPermissionSet is case-insensitive', () => {
  const set = createPermissionSet(['perm:Administracion.Usuarios:Ver', ' PERM:Administracion.Roles:Crear ']);

  assert.equal(hasPermission(set, 'perm:administracion.usuarios:ver'), true);
  assert.equal(hasPermission(set, 'PERM:ADMINISTRACION.ROLES:CREAR'), true);
  assert.equal(hasPermission(set, 'perm:Administracion.Permisos:Editar'), false);
});

test('hasModuleAction checks module and action independently of case', () => {
  const set = createPermissionSet(['perm:Administracion.Permisos:Editar']);

  assert.equal(hasModuleAction(set, 'administracion.permisos', 'editar'), true);
  assert.equal(hasModuleAction(set, 'Administracion.Permisos', 'Ver'), false);
});

test('createPermissionSet tolerates null and empty collections', () => {
  assert.equal(createPermissionSet(null).size, 0);
  assert.equal(createPermissionSet(undefined).size, 0);
  assert.equal(createPermissionSet([]).size, 0);
});
