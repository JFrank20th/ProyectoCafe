import assert from 'node:assert/strict';
import test from 'node:test';

import type { DashboardSummary } from '../api/dashboard';
import {
  buildDashboardMetrics,
  formatCount,
  formatCurrency,
  formatPoints,
  formatPercent,
  formatProductionValue,
} from './summary-metrics';

const messages: Record<string, string> = {
  kpiProductionLabel: 'Produccion disponible',
  kpiProductionCaption: '{count} lotes con stock, {processes} procesos activos',
  kpiProductionDescription: 'Volumen util del periodo.',
  kpiCommercialLabel: 'Ventas comprometidas',
  kpiCommercialCaption: '{count} ventas, {customers} clientes activos',
  kpiCommercialDescription: 'Demanda y clientes del periodo.',
  kpiFinanceLabel: 'Margen bruto esperado',
  kpiFinanceCaption: '{amount} costos activos',
  kpiFinanceDescription: 'Rentabilidad del periodo.',
  kpiTraceabilityLabel: 'Lotes con trazabilidad',
  kpiTraceabilityCaption: '{count} lotes trazables de {total} activos',
  kpiTraceabilityDescription: 'Cobertura documental del periodo.',
  deltaVsPrevious: '{delta} vs periodo anterior',
  deltaUnavailable: 'Sin comparativo',
};

test('buildDashboardMetrics mapea los 4 KPI usando los campos correctos del resumen', () => {
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
      production: {
        activeLotsCount: 4,
        lotsWithStockCount: 1,
        availableQuantity: 80,
        availableUnit: 'kg',
        processRecordsCount: 5,
      },
      commercial: {
        activeSalesCount: 3,
        activeSalesTotal: 84000,
        activeCustomersCount: 2,
      },
      finance: {
        grossMarginAmount: 14000,
        grossMarginPercent: 15.2,
        activeCostsTotal: 51000,
      },
      traceability: {
        activeLotsCount: 4,
        traceableLotsCount: 3,
        coveragePercent: 75.6,
      },
    },
    production: {
      activeLotsCount: 5,
      lotsWithStockCount: 2,
      availableQuantity: 120,
      availableUnit: 'kg',
      processRecordsCount: 6,
    },
    commercial: {
      activeSalesCount: 4,
      activeSalesTotal: 92000,
      activeCustomersCount: 3,
    },
    finance: {
      grossMarginAmount: 18000,
      grossMarginPercent: 19.6,
      activeCostsTotal: 54000,
    },
    traceability: {
      activeLotsCount: 5,
      traceableLotsCount: 4,
      coveragePercent: 80,
    },
    generatedAtUtc: '2026-04-19T12:00:00.000Z',
  };

  const metrics = buildDashboardMetrics(summary, 'es', translate);

  assert.equal(metrics.length, 4);

  assert.deepEqual(metrics[0], {
    tone: 'production',
    title: 'Produccion disponible',
    value: formatProductionValue('es', summary.production),
    trendLabel: '+40 kg vs periodo anterior',
    trendIntent: 'positive',
    caption: '2 lotes con stock, 6 procesos activos',
    description: 'Volumen util del periodo.',
  });

  assert.equal(metrics[1].tone, 'commercial');
  assert.equal(metrics[1].value, formatCurrency('es', summary.commercial.activeSalesTotal));
  assert.equal(metrics[1].trendLabel, `+${formatCurrency('es', 8000)} vs periodo anterior`);
  assert.equal(metrics[1].trendIntent, 'positive');
  assert.equal(metrics[1].caption, '4 ventas, 3 clientes activos');

  assert.equal(metrics[2].tone, 'finance');
  assert.equal(metrics[2].value, formatPercent('es', summary.finance.grossMarginPercent));
  assert.equal(metrics[2].trendLabel, `+${formatPoints('es', 4.4)} vs periodo anterior`);
  assert.equal(metrics[2].trendIntent, 'positive');
  assert.equal(metrics[2].caption, `${formatCurrency('es', summary.finance.activeCostsTotal)} costos activos`);

  assert.equal(metrics[3].tone, 'traceability');
  assert.equal(metrics[3].value, formatPercent('es', summary.traceability.coveragePercent));
  assert.equal(metrics[3].trendLabel, `+${formatPoints('es', 4.4)} vs periodo anterior`);
  assert.equal(metrics[3].trendIntent, 'positive');
  assert.equal(metrics[3].caption, '4 lotes trazables de 5 activos');
});

test('formatProductionValue usa el conteo de lotes cuando no hay cantidad o unidad disponible', () => {
  const value = formatProductionValue('es', {
    activeLotsCount: 3,
    lotsWithStockCount: 3,
    availableQuantity: 0,
    availableUnit: null,
    processRecordsCount: 1,
  });

  assert.equal(value, formatCount('es', 3));
});

test('buildDashboardMetrics muestra estado neutral cuando no existe periodo anterior comparable', () => {
  const summary: DashboardSummary = {
    window: {
      period: 'all',
      from: null,
      to: null,
      comparisonAvailable: false,
    },
    previousPeriod: null,
    production: {
      activeLotsCount: 3,
      lotsWithStockCount: 3,
      availableQuantity: 0,
      availableUnit: null,
      processRecordsCount: 2,
    },
    commercial: {
      activeSalesCount: 1,
      activeSalesTotal: 1000,
      activeCustomersCount: 1,
    },
    finance: {
      grossMarginAmount: 200,
      grossMarginPercent: 20,
      activeCostsTotal: 800,
    },
    traceability: {
      activeLotsCount: 3,
      traceableLotsCount: 2,
      coveragePercent: 66.7,
    },
    generatedAtUtc: '2026-04-19T12:00:00.000Z',
  };

  const metrics = buildDashboardMetrics(summary, 'es', translate);

  assert.equal(metrics[0].trendLabel, 'Sin comparativo');
  assert.equal(metrics[0].trendIntent, 'neutral');
  assert.equal(metrics[1].trendLabel, 'Sin comparativo');
  assert.equal(metrics[2].trendLabel, 'Sin comparativo');
  assert.equal(metrics[3].trendLabel, 'Sin comparativo');
});

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