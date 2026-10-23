import assert from 'node:assert/strict';
import test from 'node:test';

import type { DashboardSummary } from '../api/dashboard';
import {
  buildDashboardMetrics,
  formatCount,
  formatPercent,
} from './summary-metrics';

const messages: Record<string, string> = {
  kpiUsersLabel: 'Nuevos usuarios',
  kpiUsersCaption: '{active} usuarios activos de {total}',
  kpiUsersDescription: 'Altas registradas en el periodo.',
  kpiRolesLabel: 'Roles activos',
  kpiRolesCaption: '{assignments} asignaciones sobre {total} roles',
  kpiRolesDescription: 'Roles disponibles para asignar permisos.',
  kpiPermissionsLabel: 'Permisos vigentes',
  kpiPermissionsCaption: '{modules} módulos, {assignments} asignaciones',
  kpiPermissionsDescription: 'Permisos efectivos registrados por rol.',
  kpiAccessLabel: 'Cobertura de acceso',
  kpiAccessCaption: '{users} de {active} usuarios activos con rol',
  kpiAccessDescription: 'Usuarios activos con al menos un rol vigente.',
  deltaVsPrevious: '{delta} vs periodo anterior',
  deltaUnavailable: 'Sin comparativo',
  currentState: 'Estado actual',
};

test('buildDashboardMetrics mapea los 4 KPI de administración con los campos correctos', () => {
  const summary: DashboardSummary = {
    window: {
      period: '30d',
      from: '2026-03-21',
      to: '2026-04-19',
      comparisonAvailable: true,
    },
    previousPeriod: {
      window: {
        period: '30d',
        from: '2026-02-19',
        to: '2026-03-20',
        comparisonAvailable: true,
      },
      users: {
        totalCount: 10,
        activeCount: 6,
        pendingCount: 2,
        inactiveCount: 2,
        newCount: 3,
      },
      roles: {
        totalCount: 4,
        activeCount: 3,
        assignmentsCount: 8,
      },
      permissions: {
        assignmentsCount: 12,
        distinctCount: 6,
        moduleCount: 3,
      },
      access: {
        activeUsersCount: 6,
        usersWithRolesCount: 5,
        coveragePercent: 83.3,
      },
    },
    users: {
      totalCount: 12,
      activeCount: 7,
      pendingCount: 3,
      inactiveCount: 2,
      newCount: 5,
    },
    roles: {
      totalCount: 5,
      activeCount: 4,
      assignmentsCount: 9,
    },
    permissions: {
      assignmentsCount: 14,
      distinctCount: 7,
      moduleCount: 3,
    },
    access: {
      activeUsersCount: 7,
      usersWithRolesCount: 6,
      coveragePercent: 85.7,
    },
    generatedAtUtc: '2026-04-19T12:00:00.000Z',
  };

  const metrics = buildDashboardMetrics(summary, 'es', translate);

  assert.equal(metrics.length, 4);

  assert.deepEqual(metrics[0], {
    tone: 'production',
    title: 'Nuevos usuarios',
    value: formatCount('es', 5),
    trendLabel: '+2 vs periodo anterior',
    trendIntent: 'positive',
    caption: '7 usuarios activos de 12',
    description: 'Altas registradas en el periodo.',
  });

  assert.equal(metrics[1].tone, 'commercial');
  assert.equal(metrics[1].value, formatCount('es', 4));
  assert.equal(metrics[1].trendLabel, 'Estado actual');
  assert.equal(metrics[1].trendIntent, 'neutral');
  assert.equal(metrics[1].caption, '9 asignaciones sobre 5 roles');

  assert.equal(metrics[2].tone, 'finance');
  assert.equal(metrics[2].value, formatCount('es', 7));
  assert.equal(metrics[2].caption, '3 módulos, 14 asignaciones');

  assert.equal(metrics[3].tone, 'traceability');
  assert.equal(metrics[3].value, formatPercent('es', 85.7));
  assert.equal(metrics[3].caption, '6 de 7 usuarios activos con rol');
});

test('buildDashboardMetrics usa el comparativo del periodo anterior solo para nuevos usuarios', () => {
  const summary = createSummary({
    previousPeriod: {
      window: {
        period: '30d',
        from: '2026-02-19',
        to: '2026-03-20',
        comparisonAvailable: true,
      },
      users: {
        totalCount: 6,
        activeCount: 4,
        pendingCount: 1,
        inactiveCount: 1,
        newCount: 8,
      },
      roles: {
        totalCount: 2,
        activeCount: 2,
        assignmentsCount: 3,
      },
      permissions: {
        assignmentsCount: 4,
        distinctCount: 4,
        moduleCount: 3,
      },
      access: {
        activeUsersCount: 4,
        usersWithRolesCount: 4,
        coveragePercent: 100,
      },
    },
  });

  const metrics = buildDashboardMetrics(summary, 'es', translate);

  assert.equal(metrics[0].trendLabel, '-3 vs periodo anterior');
  assert.equal(metrics[0].trendIntent, 'negative');
});

test('buildDashboardMetrics muestra estado neutral cuando no existe periodo anterior comparable', () => {
  const summary = createSummary({ previousPeriod: null });

  const metrics = buildDashboardMetrics(summary, 'es', translate);

  assert.equal(metrics[0].trendLabel, 'Sin comparativo');
  assert.equal(metrics[0].trendIntent, 'neutral');
  assert.equal(metrics[1].trendLabel, 'Estado actual');
  assert.equal(metrics[2].trendLabel, 'Estado actual');
  assert.equal(metrics[3].trendLabel, 'Estado actual');
});

function createSummary(overrides: Partial<DashboardSummary> = {}): DashboardSummary {
  return {
    window: {
      period: '30d',
      from: '2026-03-21',
      to: '2026-04-19',
      comparisonAvailable: true,
    },
    previousPeriod: null,
    users: {
      totalCount: 12,
      activeCount: 7,
      pendingCount: 3,
      inactiveCount: 2,
      newCount: 5,
    },
    roles: {
      totalCount: 5,
      activeCount: 4,
      assignmentsCount: 9,
    },
    permissions: {
      assignmentsCount: 14,
      distinctCount: 7,
      moduleCount: 3,
    },
    access: {
      activeUsersCount: 7,
      usersWithRolesCount: 6,
      coveragePercent: 85.7,
    },
    generatedAtUtc: '2026-04-19T12:00:00.000Z',
    ...overrides,
  };
}

function translate(key: string, params?: Record<string, string | number>) {
  const template = messages[key];
  assert.ok(template, `Missing message for ${key}`);

  if (!params) {
    return template;
  }

  return Object.entries(params).reduce(
    (result, [paramKey, value]) => result.replace(`{${paramKey}}`, String(value)),
    template
  );
}
