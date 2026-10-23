import assert from 'node:assert/strict';
import test from 'node:test';

import {
  NAV,
  canSeeItem,
  filterVisibleNavItems,
  type NavItem,
} from '@/lib/navigation';
import { DASHBOARD_ROUTE, HOME_ROUTE } from '@/lib/routes';

test('dashboard is visible for authenticated users without permissions', () => {
  const main = NAV.find((section) => section.labelKey === 'main');
  assert.ok(main);

  const visible = filterVisibleNavItems([], [], main.items);

  assert.deepEqual(visible, [{ labelKey: 'dashboard', href: DASHBOARD_ROUTE }]);
});

test('administration section is hidden when the user has no administration permissions', () => {
  const administration = NAV.find((section) => section.labelKey === 'administration');
  assert.ok(administration);

  const visible = filterVisibleNavItems([], [], administration.items);

  assert.deepEqual(visible, []);
});

test('users and roles are shown independently according to permissions', () => {
  const administration = NAV.find((section) => section.labelKey === 'administration');
  assert.ok(administration);

  const onlyUsers = filterVisibleNavItems(
    [],
    ['perm:Administracion.Usuarios:Ver'],
    administration.items,
  );

  assert.equal(onlyUsers.length, 1);
  assert.deepEqual(onlyUsers[0].items?.map((item) => item.labelKey), ['usuarios']);

  const onlyRoles = filterVisibleNavItems(
    [],
    ['PERM:ADMINISTRACION.ROLES:VER'],
    administration.items,
  );

  assert.deepEqual(onlyRoles[0].items?.map((item) => item.labelKey), ['roles']);
});

test('navigation filters by role when an item declares one', () => {
  const item: NavItem = { labelKey: 'adminOnly', href: '/admin', roles: ['Admin'] };
  const permissionSet = new Set<string>();

  assert.equal(canSeeItem(['Admin'], permissionSet, item), true);
  assert.equal(canSeeItem(['User'], permissionSet, item), false);
});

test('navigation role filters accept custom roles and ignore casing', () => {
  const item: NavItem = {
    labelKey: 'support',
    href: '/soporte',
    roles: ['Soporte administrativo'],
  };
  const permissionSet = new Set<string>();

  assert.equal(canSeeItem([' soporte ADMINISTRATIVO '], permissionSet, item), true);
  assert.equal(canSeeItem(['Consulta'], permissionSet, item), false);
});

test('role matches never bypass backend permission requirements', () => {
  const item: NavItem = {
    labelKey: 'roles',
    href: '/administracion/roles',
    roles: ['Soporte administrativo'],
    permissions: ['perm:Administracion.Roles:Ver'],
  };

  assert.equal(canSeeItem(['Soporte administrativo'], new Set<string>(), item), false);
  assert.equal(
    canSeeItem(
      ['Soporte administrativo'],
      new Set(['perm:administracion.roles:ver']),
      item,
    ),
    true,
  );
});

test('home route points to the dashboard', () => {
  assert.equal(HOME_ROUTE, '/dashboard');
  assert.equal(DASHBOARD_ROUTE, HOME_ROUTE);
});
