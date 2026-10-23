import assert from 'node:assert/strict';
import test from 'node:test';

import DashboardPage from '@/app/dashboard/page';
import { NAV, filterVisibleNavItems } from '@/lib/navigation';
import { DASHBOARD_ROUTE, HOME_ROUTE } from '@/lib/routes';

test('dashboard page remains an empty placeholder inside the authenticated shell', () => {
  assert.equal(DashboardPage(), null);
});

test('dashboard is the authenticated landing route', () => {
  assert.equal(HOME_ROUTE, DASHBOARD_ROUTE);

  const main = NAV.find((section) => section.labelKey === 'main');
  assert.ok(main);

  const visible = filterVisibleNavItems([], [], main.items);
  assert.deepEqual(visible, [{ labelKey: 'dashboard', href: DASHBOARD_ROUTE }]);
});

test('dashboard does not require administration permissions', () => {
  const allItems = NAV.flatMap((section) => filterVisibleNavItems([], [], section.items));
  const dashboardItems = allItems.filter((item) => item.href === DASHBOARD_ROUTE);

  assert.equal(dashboardItems.length, 1);
});
