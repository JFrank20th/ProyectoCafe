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
  const newUsersTrend = getNewUsersTrend(summary, locale, t);

  return [
    {
      tone: 'production',
      title: t('kpiUsersLabel'),
      value: formatCount(locale, summary.users.newCount),
      trendLabel: newUsersTrend.label,
      trendIntent: newUsersTrend.intent,
      caption: t('kpiUsersCaption', {
        active: summary.users.activeCount,
        total: summary.users.totalCount,
      }),
      description: t('kpiUsersDescription'),
    },
    {
      tone: 'commercial',
      title: t('kpiRolesLabel'),
      value: formatCount(locale, summary.roles.activeCount),
      trendLabel: t('currentState'),
      trendIntent: 'neutral',
      caption: t('kpiRolesCaption', {
        total: summary.roles.totalCount,
        assignments: summary.roles.assignmentsCount,
      }),
      description: t('kpiRolesDescription'),
    },
    {
      tone: 'finance',
      title: t('kpiPermissionsLabel'),
      value: formatCount(locale, summary.permissions.distinctCount),
      trendLabel: t('currentState'),
      trendIntent: 'neutral',
      caption: t('kpiPermissionsCaption', {
        modules: summary.permissions.moduleCount,
        assignments: summary.permissions.assignmentsCount,
      }),
      description: t('kpiPermissionsDescription'),
    },
    {
      tone: 'traceability',
      title: t('kpiAccessLabel'),
      value: formatPercent(locale, summary.access.coveragePercent),
      trendLabel: t('currentState'),
      trendIntent: 'neutral',
      caption: t('kpiAccessCaption', {
        users: summary.access.usersWithRolesCount,
        active: summary.access.activeUsersCount,
      }),
      description: t('kpiAccessDescription'),
    },
  ];
}

type DashboardTrendViewModel = {
  label: string;
  intent: DashboardMetricTrendIntent;
};

function getNewUsersTrend(summary: DashboardSummary, locale: string, t: DashboardTranslator): DashboardTrendViewModel {
  const previousValue = summary.previousPeriod?.users.newCount;
  if (previousValue === undefined) {
    return createUnavailableTrend(t);
  }

  const delta = summary.users.newCount - previousValue;
  return createTrend(t, formatSignedCount(locale, delta), delta);
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

export function formatPercent(locale: string, value: number) {
  const formatter = new Intl.NumberFormat(locale, {
    minimumFractionDigits: Number.isInteger(value) ? 0 : 1,
    maximumFractionDigits: 1,
  });

  return `${formatter.format(value)}%`;
}

export function formatCount(locale: string, value: number) {
  return new Intl.NumberFormat(locale, {
    maximumFractionDigits: 0,
  }).format(value);
}

function formatSignedCount(locale: string, value: number) {
  const absolute = formatCount(locale, Math.abs(value));

  if (value > 0) {
    return `+${absolute}`;
  }

  if (value < 0) {
    return `-${absolute}`;
  }

  return absolute;
}
