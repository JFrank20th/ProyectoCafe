import { apiFetch } from '@/lib/api/client';

export const dashboardPresetPeriods = ['7d', '30d', '90d', '365d', 'all'] as const;

export const dashboardPeriods = [...dashboardPresetPeriods, 'custom'] as const;

export type DashboardPresetPeriod = (typeof dashboardPresetPeriods)[number];

export type DashboardPeriod = (typeof dashboardPeriods)[number];

export type DashboardSummaryFilter = {
  period: DashboardPeriod;
  from?: string;
  to?: string;
};

export type DashboardSummaryWindow = {
  period: DashboardPeriod;
  from: string | null;
  to: string | null;
  comparisonAvailable: boolean;
};

export type DashboardSummary = {
  window: DashboardSummaryWindow;
  previousPeriod: {
    window: DashboardSummaryWindow;
    production: {
      activeLotsCount: number;
      lotsWithStockCount: number;
      availableQuantity: number;
      availableUnit: string | null;
      processRecordsCount: number;
    };
    commercial: {
      activeSalesCount: number;
      activeSalesTotal: number;
      activeCustomersCount: number;
    };
    finance: {
      grossMarginAmount: number;
      grossMarginPercent: number;
      activeCostsTotal: number;
    };
    traceability: {
      activeLotsCount: number;
      traceableLotsCount: number;
      coveragePercent: number;
    };
  } | null;
  production: {
    activeLotsCount: number;
    lotsWithStockCount: number;
    availableQuantity: number;
    availableUnit: string | null;
    processRecordsCount: number;
  };
  commercial: {
    activeSalesCount: number;
    activeSalesTotal: number;
    activeCustomersCount: number;
  };
  finance: {
    grossMarginAmount: number;
    grossMarginPercent: number;
    activeCostsTotal: number;
  };
  traceability: {
    activeLotsCount: number;
    traceableLotsCount: number;
    coveragePercent: number;
  };
  generatedAtUtc: string;
};

export function fetchDashboardSummary(filter: DashboardSummaryFilter = { period: '30d' }) {
  const params = new URLSearchParams({ period: filter.period });
  if (filter.from) {
    params.set('from', filter.from);
  }

  if (filter.to) {
    params.set('to', filter.to);
  }

  return apiFetch<DashboardSummary>(`/api/v1/dashboard/summary?${params.toString()}`);
}