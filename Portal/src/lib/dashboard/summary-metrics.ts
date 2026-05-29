import type { KpiTone } from '../../components/dashboard/kpi-metric-card';
import type { DashboardSummary } from '../api/dashboard';

export type DashboardMetricTrendIntent = 'positive' | 'negative' | 'neutral';

export type DashboardMetricViewModel = {
  tone: KpiTone;
  title: string;
  value: string;
  trendLabel: string;
  trendIntent: DashboardMetricTrendIntent;
  caption: string;
  description: string;
};

export type DashboardTranslator = (key: string, params?: Record<string, string | number>) => string;

export function buildDashboardMetrics(
  summary: DashboardSummary,
  locale: string,
  t: DashboardTranslator
): DashboardMetricViewModel[] {
  const productionTrend = getProductionTrend(summary, locale, t);
  const commercialTrend = getMonetaryTrend(summary.commercial.activeSalesTotal, summary.previousPeriod?.commercial.activeSalesTotal, locale, t);
  const financeTrend = getPointsTrend(summary.finance.grossMarginPercent, summary.previousPeriod?.finance.grossMarginPercent, locale, t);
  const traceabilityTrend = getPointsTrend(summary.traceability.coveragePercent, summary.previousPeriod?.traceability.coveragePercent, locale, t);

  return [
    {
      tone: 'production',
      title: t('kpiProductionLabel'),
      value: formatProductionValue(locale, summary.production),
      trendLabel: productionTrend.label,
      trendIntent: productionTrend.intent,
      caption: t('kpiProductionCaption', {
        count: summary.production.lotsWithStockCount,
        processes: summary.production.processRecordsCount,
      }),
      description: t('kpiProductionDescription'),
    },
    {
      tone: 'commercial',
      title: t('kpiCommercialLabel'),
      value: formatCurrency(locale, summary.commercial.activeSalesTotal),
      trendLabel: commercialTrend.label,
      trendIntent: commercialTrend.intent,
      caption: t('kpiCommercialCaption', {
        count: summary.commercial.activeSalesCount,
        customers: summary.commercial.activeCustomersCount,
      }),
      description: t('kpiCommercialDescription'),
    },
    {
      tone: 'finance',
      title: t('kpiFinanceLabel'),
      value: formatPercent(locale, summary.finance.grossMarginPercent),
      trendLabel: financeTrend.label,
      trendIntent: financeTrend.intent,
      caption: t('kpiFinanceCaption', { amount: formatCurrency(locale, summary.finance.activeCostsTotal) }),
      description: t('kpiFinanceDescription'),
    },
    {
      tone: 'traceability',
      title: t('kpiTraceabilityLabel'),
      value: formatPercent(locale, summary.traceability.coveragePercent),
      trendLabel: traceabilityTrend.label,
      trendIntent: traceabilityTrend.intent,
      caption: t('kpiTraceabilityCaption', {
        count: summary.traceability.traceableLotsCount,
        total: summary.traceability.activeLotsCount,
      }),
      description: t('kpiTraceabilityDescription'),
    },
  ];
}

type DashboardTrendViewModel = {
  label: string;
  intent: DashboardMetricTrendIntent;
};

function getProductionTrend(summary: DashboardSummary, locale: string, t: DashboardTranslator): DashboardTrendViewModel {
  const previous = summary.previousPeriod?.production;
  if (!previous) {
    return createUnavailableTrend(t);
  }

  const currentUnit = summary.production.availableUnit?.trim().toLowerCase();
  const previousUnit = previous.availableUnit?.trim().toLowerCase();
  const compareQuantity = Boolean(currentUnit && previousUnit && currentUnit === previousUnit);

  if (compareQuantity) {
    const delta = summary.production.availableQuantity - previous.availableQuantity;
    return createTrend(t, formatSignedQuantity(locale, delta, summary.production.availableUnit), delta);
  }

  const delta = summary.production.lotsWithStockCount - previous.lotsWithStockCount;
  return createTrend(t, formatSignedCount(locale, delta), delta);
}

function getMonetaryTrend(
  currentValue: number,
  previousValue: number | undefined,
  locale: string,
  t: DashboardTranslator
): DashboardTrendViewModel {
  if (previousValue === undefined) {
    return createUnavailableTrend(t);
  }

  const delta = currentValue - previousValue;
  return createTrend(t, formatSignedCurrency(locale, delta), delta);
}

function getPointsTrend(
  currentValue: number,
  previousValue: number | undefined,
  locale: string,
  t: DashboardTranslator
): DashboardTrendViewModel {
  if (previousValue === undefined) {
    return createUnavailableTrend(t);
  }

  const delta = currentValue - previousValue;
  return createTrend(t, formatSignedPoints(locale, delta), delta);
}

function createTrend(t: DashboardTranslator, delta: string, value: number): DashboardTrendViewModel {
  return {
    label: t('deltaVsPrevious', { delta }),
    intent: resolveTrendIntent(value),
  };
}

function createUnavailableTrend(t: DashboardTranslator): DashboardTrendViewModel {
  return {
    label: t('deltaUnavailable'),
    intent: 'neutral',
  };
}

function resolveTrendIntent(value: number): DashboardMetricTrendIntent {
  if (value > 0) {
    return 'positive';
  }

  if (value < 0) {
    return 'negative';
  }

  return 'neutral';
}

export function formatCurrency(locale: string, value: number) {
  const formatter = new Intl.NumberFormat(locale, {
    notation: Math.abs(value) >= 100000 ? 'compact' : 'standard',
    maximumFractionDigits: Math.abs(value) >= 100000 ? 1 : 0,
  });

  return `$${formatter.format(value)}`;
}

export function formatPercent(locale: string, value: number) {
  const formatter = new Intl.NumberFormat(locale, {
    minimumFractionDigits: Number.isInteger(value) ? 0 : 1,
    maximumFractionDigits: 1,
  });

  return `${formatter.format(value)}%`;
}

export function formatQuantity(locale: string, value: number) {
  return new Intl.NumberFormat(locale, {
    minimumFractionDigits: 0,
    maximumFractionDigits: Math.abs(value) >= 100 ? 0 : 1,
  }).format(value);
}

export function formatCount(locale: string, value: number) {
  return new Intl.NumberFormat(locale, {
    maximumFractionDigits: 0,
  }).format(value);
}

export function formatPoints(locale: string, value: number) {
  const formatter = new Intl.NumberFormat(locale, {
    minimumFractionDigits: Number.isInteger(value) ? 0 : 1,
    maximumFractionDigits: 1,
  });

  return `${formatter.format(value)} pp`;
}

export function formatProductionValue(locale: string, production: DashboardSummary['production']) {
  if (production.availableUnit && production.availableQuantity > 0) {
    return `${formatQuantity(locale, production.availableQuantity)} ${production.availableUnit}`;
  }

  return formatCount(locale, production.lotsWithStockCount);
}

function formatSignedCurrency(locale: string, value: number) {
  return formatSignedValue(value, formatCurrency(locale, Math.abs(value)));
}

function formatSignedCount(locale: string, value: number) {
  return formatSignedValue(value, formatCount(locale, Math.abs(value)));
}

function formatSignedQuantity(locale: string, value: number, unit?: string | null) {
  const base = `${formatQuantity(locale, Math.abs(value))}${unit ? ` ${unit}` : ''}`;
  return formatSignedValue(value, base);
}

function formatSignedPoints(locale: string, value: number) {
  return formatSignedValue(value, formatPoints(locale, Math.abs(value)));
}

function formatSignedValue(value: number, formattedAbsoluteValue: string) {
  if (value > 0) {
    return `+${formattedAbsoluteValue}`;
  }

  if (value < 0) {
    return `-${formattedAbsoluteValue}`;
  }

  return formattedAbsoluteValue;
}