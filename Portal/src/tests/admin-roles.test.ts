import assert from 'node:assert/strict';
import test from 'node:test';

import {
  buildRolePayload,
  hasRoleValidationErrors,
  isRoleVisible,
  normalizePermissionKey,
  ROLE_DESCRIPTION_MAX_LENGTH,
  ROLE_NAME_MAX_LENGTH,
  roleEstadoColor,
  roleEstadoKind,
  toPermissionMap,
  toPermissionPayload,
  toggleSelectedPermission,
  validateRoleInput,
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

test('buildRolePayload keeps custom role names intact', () => {
  assert.deepEqual(
    buildRolePayload({
      nombre: '  Soporte administrativo  ',
      descripcion: '  Consulta y reportería  ',
      estado: 0,
    }),
    {
      nombre: 'Soporte administrativo',
      descripcion: 'Consulta y reportería',
      estado: 0,
    },
  );
});

test('validateRoleInput accepts arbitrary custom role names', () => {
  assert.deepEqual(
    validateRoleInput({ nombre: 'Soporte administrativo', descripcion: 'Consulta y reportería' }),
    {},
  );
  assert.deepEqual(validateRoleInput({ nombre: '  Auditoría  ', descripcion: '' }), {});
});

test('validateRoleInput mirrors backend required and length rules', () => {
  const required = validateRoleInput({ nombre: '   ', descripcion: '' });
  assert.deepEqual(required, { nombre: 'required' });
  assert.equal(hasRoleValidationErrors(required), true);

  const tooLongName = validateRoleInput({
    nombre: 'A'.repeat(ROLE_NAME_MAX_LENGTH + 1),
    descripcion: '',
  });
  assert.deepEqual(tooLongName, { nombre: 'maxLength' });

  const tooLongDescription = validateRoleInput({
    nombre: 'Operador',
    descripcion: 'B'.repeat(ROLE_DESCRIPTION_MAX_LENGTH + 1),
  });
  assert.deepEqual(tooLongDescription, { descripcion: 'maxLength' });

  assert.equal(hasRoleValidationErrors({}), false);
});
