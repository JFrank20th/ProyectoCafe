import assert from 'node:assert/strict';
import test from 'node:test';

import {
  HOME_ROUTE,
  LOGIN_ROUTE,
  PUBLIC_ROUTES,
  REGISTER_ROUTE,
  buildLoginRedirect,
  isPublicRoute,
} from '@/lib/routes';

test('login and register are public routes', () => {
  assert.deepEqual([...PUBLIC_ROUTES], [LOGIN_ROUTE, REGISTER_ROUTE]);
  assert.equal(isPublicRoute('/login'), true);
  assert.equal(isPublicRoute('/login/'), true);
  assert.equal(isPublicRoute('/register'), true);
  assert.equal(isPublicRoute('/register?returnUrl=%2Fdashboard'), true);
});

test('protected routes are not public', () => {
  assert.equal(isPublicRoute(HOME_ROUTE), false);
  assert.equal(isPublicRoute('/administracion/usuarios'), false);
  assert.equal(isPublicRoute('/login/extra'), false);
  assert.equal(isPublicRoute(null), false);
  assert.equal(isPublicRoute(undefined), false);
});

test('buildLoginRedirect preserves the full encoded location', () => {
  assert.equal(
    buildLoginRedirect('/administracion/roles?page=2&filter=activo'),
    '/login?returnUrl=%2Fadministracion%2Froles%3Fpage%3D2%26filter%3Dactivo',
  );
});

test('buildLoginRedirect normalizes paths without a leading slash', () => {
  assert.equal(buildLoginRedirect('dashboard'), '/login?returnUrl=%2Fdashboard');
});
