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

export type DashboardUsersSummary = {
  totalCount: number;
  activeCount: number;
  pendingCount: number;
  inactiveCount: number;
  newCount: number;
};

export type DashboardRolesSummary = {
  totalCount: number;
  activeCount: number;
  assignmentsCount: number;
};

export type DashboardPermissionsSummary = {
  assignmentsCount: number;
  distinctCount: number;
  moduleCount: number;
};

export type DashboardAccessSummary = {
  activeUsersCount: number;
  usersWithRolesCount: number;
  coveragePercent: number;
};

export type DashboardAdminMetrics = {
  users: DashboardUsersSummary;
  roles: DashboardRolesSummary;
  permissions: DashboardPermissionsSummary;
  access: DashboardAccessSummary;
};

export type DashboardSummary = DashboardAdminMetrics & {
  window: DashboardSummaryWindow;
  previousPeriod: (DashboardAdminMetrics & { window: DashboardSummaryWindow }) | null;
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
