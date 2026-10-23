import assert from 'node:assert/strict';
import test from 'node:test';

import {
  buildRolePayload,
  isRoleVisible,
  normalizePermissionKey,
  roleEstadoColor,
  roleEstadoKind,
  toPermissionMap,
  toPermissionPayload,
  toggleSelectedPermission,
} from '@/lib/admin/roles';

test('normalizePermissionKey trims and lowercases both segments', () => {
  assert.equal(
    normalizePermissionKey(' Administracion.Roles ', ' EDITAR '),
    'administracion.roles:editar',
  );
});

test('toPermissionMap ignores empty entries and deduplicates keys', () => {
  const map = toPermissionMap([
    { modulo: 'Administracion.Roles', accion: 'Ver' },
    { modulo: ' administracion.roles ', accion: 'ver' },
    { modulo: '', accion: 'Ver' },
    { modulo: 'Administracion.Usuarios', accion: '   ' },
  ]);

  assert.deepEqual(map, {
    'administracion.roles:ver': { modulo: 'Administracion.Roles', accion: 'Ver' },
  });
});

test('toggleSelectedPermission adds and removes permissions', () => {
  const selected = toggleSelectedPermission({}, 'Administracion.Roles', 'Ver');
  assert.deepEqual(selected, {
    'administracion.roles:ver': { modulo: 'Administracion.Roles', accion: 'Ver' },
  });

  const removed = toggleSelectedPermission(selected, 'administracion.roles', 'VER');
  assert.deepEqual(removed, {});
});

test('toPermissionPayload returns the selected values', () => {
  const selected = toPermissionMap([
    { modulo: 'Administracion.Roles', accion: 'Ver' },
    { modulo: 'Administracion.Permisos', accion: 'Editar' },
  ]);

  assert.deepEqual(toPermissionPayload(selected), [
    { modulo: 'Administracion.Roles', accion: 'Ver' },
    { modulo: 'Administracion.Permisos', accion: 'Editar' },
  ]);
});

test('role status helpers cover active, inactive and deleted states', () => {
  assert.equal(roleEstadoColor(0), 'success');
  assert.equal(roleEstadoColor(1), 'warning');
  assert.equal(roleEstadoColor(2), 'error');

  assert.equal(roleEstadoKind(0), 'active');
  assert.equal(roleEstadoKind(1), 'inactive');
  assert.equal(roleEstadoKind(2), 'deleted');

  assert.equal(isRoleVisible({ rolId: 1, nombre: 'Activo', estado: 0 }), true);
  assert.equal(isRoleVisible({ rolId: 2, nombre: 'Eliminado', estado: 2 }), false);
});

test('buildRolePayload trims values and converts empty descriptions to null', () => {
  assert.deepEqual(
    buildRolePayload({ nombre: '  Operador  ', descripcion: '  Turno noche  ', estado: 0 }),
    { nombre: 'Operador', descripcion: 'Turno noche', estado: 0 },
  );

  assert.deepEqual(
    buildRolePayload({ nombre: 'Operador', descripcion: '   ', estado: 1 }),
    { nombre: 'Operador', descripcion: null, estado: 1 },
  );
});
