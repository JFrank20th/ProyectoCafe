import assert from 'node:assert/strict';
import test from 'node:test';

import {
  activeRoleNames,
  estadoColor,
  formatDateTime,
  type RolListItem,
  type UsuarioEstado,
} from '@/lib/admin/users';

test('estadoColor maps every user state to a stable chip color', () => {
  const expected: Record<UsuarioEstado, string> = {
    0: 'info',
    1: 'info',
    2: 'success',
    3: 'error',
    4: 'default',
  };

  for (const [estado, color] of Object.entries(expected)) {
    assert.equal(estadoColor(Number(estado) as UsuarioEstado), color);
  }
});

test('formatDateTime handles missing and invalid values', () => {
  assert.equal(formatDateTime('es-ES', null), '—');
  assert.equal(formatDateTime('es-ES', undefined), '—');
  assert.equal(formatDateTime('es-ES', ''), '—');
  assert.equal(formatDateTime('es-ES', 'not-a-date'), 'not-a-date');
});

test('formatDateTime formats valid ISO timestamps with the requested locale', () => {
  const formatted = formatDateTime('en-US', '2026-01-15T10:30:00Z');

  assert.notEqual(formatted, '—');
  assert.notEqual(formatted, '2026-01-15T10:30:00Z');
  assert.match(formatted, /2026|26/);
});

test('activeRoleNames keeps only active roles, sorted alphabetically', () => {
  const roles: RolListItem[] = [
    { rolId: 1, nombre: 'Zeta', estado: 0 },
    { rolId: 2, nombre: 'Inactivo', estado: 1 },
    { rolId: 3, nombre: 'Alfa', estado: 0 },
    { rolId: 4, nombre: 'Eliminado', estado: 2 },
  ];

  assert.deepEqual(activeRoleNames(roles), ['Alfa', 'Zeta']);
});
