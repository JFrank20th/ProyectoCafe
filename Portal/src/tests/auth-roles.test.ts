import assert from 'node:assert/strict';
import test from 'node:test';

import { hasAnyRole, hasRole, normalizeRoleName, normalizeRoleNames } from '@/lib/auth/roles';

test('normalizeRoleName trims and compares role names case-insensitively', () => {
  assert.equal(normalizeRoleName('  Soporte Administrativo  '), 'soporte administrativo');
});

test('normalizeRoleNames accepts custom role names and removes duplicates', () => {
  assert.deepEqual(
    normalizeRoleNames([
      'Soporte administrativo',
      '  Admin  ',
      'soporte ADMINISTRATIVO',
      '   ',
      'Consulta',
    ]),
    ['Soporte administrativo', 'Admin', 'Consulta'],
  );
});

test('normalizeRoleNames tolerates null and empty collections', () => {
  assert.deepEqual(normalizeRoleNames(null), []);
  assert.deepEqual(normalizeRoleNames(undefined), []);
  assert.deepEqual(normalizeRoleNames([]), []);
});

test('hasRole and hasAnyRole support arbitrary backend role names', () => {
  const userRoles = ['Soporte administrativo', 'Consulta'];

  assert.equal(hasRole(userRoles, ' soporte administrativo '), true);
  assert.equal(hasRole(userRoles, 'Admin'), false);
  assert.equal(hasAnyRole(userRoles, ['Auditor', 'CONSULTA']), true);
  assert.equal(hasAnyRole(userRoles, ['Admin', 'User']), false);
  assert.equal(hasAnyRole(userRoles, []), false);
});
