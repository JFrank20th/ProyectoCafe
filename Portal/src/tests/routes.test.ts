import assert from 'node:assert/strict';
import test from 'node:test';

import {
  FORBIDDEN_ROUTE,
  HOME_ROUTE,
  LOGIN_ROUTE,
  PUBLIC_ROUTES,
  REGISTER_ROUTE,
  RETURN_URL_PARAM,
  buildLoginRedirect,
  isApiRoute,
  isPublicRoute,
  normalizePathname,
  sanitizeReturnUrl,
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
  assert.equal(isPublicRoute(FORBIDDEN_ROUTE), false);
});

test('normalizePathname produces stable route keys', () => {
  assert.equal(normalizePathname('/login/'), '/login');
  assert.equal(normalizePathname('login'), '/login');
  assert.equal(normalizePathname('//administracion//usuarios//'), '/administracion/usuarios');
  assert.equal(normalizePathname('/dashboard?tab=1'), '/dashboard');
  assert.equal(normalizePathname(''), '/');
  assert.equal(normalizePathname(null), '/');
});

test('isApiRoute only matches Portal API paths', () => {
  assert.equal(isApiRoute('/api'), true);
  assert.equal(isApiRoute('/api/auth/session'), true);
  assert.equal(isApiRoute('/api/'), true);
  assert.equal(isApiRoute('/apicultura'), false);
  assert.equal(isApiRoute('/dashboard'), false);
  assert.equal(isApiRoute(null), false);
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

test('sanitizeReturnUrl accepts only internal, non-public paths', () => {
  assert.equal(sanitizeReturnUrl('/administracion/roles?page=2'), '/administracion/roles?page=2');
  assert.equal(sanitizeReturnUrl('/dashboard#top'), '/dashboard#top');

  assert.equal(sanitizeReturnUrl('https://evil.example'), HOME_ROUTE);
  assert.equal(sanitizeReturnUrl('//evil.example'), HOME_ROUTE);
  assert.equal(sanitizeReturnUrl('/\\evil.example'), HOME_ROUTE);
  assert.equal(sanitizeReturnUrl('/login'), HOME_ROUTE);
  assert.equal(sanitizeReturnUrl('/register?returnUrl=%2Fdashboard'), HOME_ROUTE);
  assert.equal(sanitizeReturnUrl(''), HOME_ROUTE);
  assert.equal(sanitizeReturnUrl(null), HOME_ROUTE);
});

test('RETURN_URL_PARAM matches the login query parameter', () => {
  assert.equal(RETURN_URL_PARAM, 'returnUrl');
});
