'use client';

import { useEffect, useState, type ReactNode } from 'react';

import Alert from '@mui/material/Alert';
import Button from '@mui/material/Button';
import Box from '@mui/material/Box';
import Card from '@mui/material/Card';
import CardContent from '@mui/material/CardContent';
import Chip from '@mui/material/Chip';
import Divider from '@mui/material/Divider';
import Skeleton from '@mui/material/Skeleton';
import Stack from '@mui/material/Stack';
import TextField from '@mui/material/TextField';
import Typography from '@mui/material/Typography';
import { useTheme } from '@mui/material/styles';

import { IconBuildingStore, IconCoins, IconPackage, IconReceipt, IconScale } from '@tabler/icons-react';

import { KpiMetricCard, resolveKpiTonePalette, type KpiTone } from '@/components/dashboard/kpi-metric-card';
import { useLocale, useTranslations } from '@/hooks/useTranslations';
import {
  dashboardPeriods,
  fetchDashboardSummary,
  type DashboardPeriod,
  type DashboardSummary,
  type DashboardSummaryFilter,
  type DashboardSummaryWindow,
} from '@/lib/api/dashboard';
import { buildDashboardMetrics } from '@/lib/dashboard/summary-metrics';
import { getRuntimeConfig } from '@/lib/runtime-config';
import { withAlpha } from '@/theme/berry/color-utils';

export default function DashboardPage() {
  const t = useTranslations('dashboard');
  const theme = useTheme();
  const locale = useLocale();
  const apiUrl = getRuntimeConfig().API_URL;
  const [filterMode, setFilterMode] = useState<DashboardPeriod>('30d');
  const [activeFilter, setActiveFilter] = useState<DashboardSummaryFilter>({ period: '30d' });
  const [customRangeDraft, setCustomRangeDraft] = useState<CustomRangeDraft>(createDefaultCustomRange);
  const [summary, setSummary] = useState<DashboardSummary | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;

    async function loadSummary() {
      setLoading(true);
      setError(null);
      setSummary(null);

      try {
        const nextSummary = await fetchDashboardSummary(activeFilter);
        if (!cancelled) {
          setSummary(nextSummary);
        }
      } catch (err) {
        if (!cancelled) {
          setError(err instanceof Error ? err.message : 'Error');
        }
      } finally {
        if (!cancelled) {
          setLoading(false);
        }
      }
    }

    void loadSummary();

    return () => {
      cancelled = true;
    };
  }, [activeFilter.from, activeFilter.period, activeFilter.to]);

  const periodLabels: Record<DashboardPeriod, string> = {
    '7d': t('period7d'),
    '30d': t('period30d'),
    '90d': t('period90d'),
    '365d': t('period365d'),
    all: t('periodAll'),
    custom: t('periodCustom'),
  };

  const customRangeError = getCustomRangeValidationMessage(customRangeDraft, t);
  const activeWindowLabel = getDashboardWindowLabel(locale, summary?.window ?? toWindow(activeFilter), periodLabels);
  const previousWindowLabel = summary?.previousPeriod
    ? getDashboardWindowLabel(locale, summary.previousPeriod.window, periodLabels)
    : null;

  const metrics = summary
    ? buildDashboardMetrics(summary, locale, t).map((metric) => ({
        ...metric,
        icon: getDashboardMetricIcon(metric.tone),
      }))
    : [];

  const guideItems: Array<{ tone: KpiTone; label: string; description: string }> = [
    { tone: 'production', label: t('guideProductionLabel'), description: t('guideProductionDescription') },
    { tone: 'commercial', label: t('guideCommercialLabel'), description: t('guideCommercialDescription') },
    { tone: 'finance', label: t('guideFinanceLabel'), description: t('guideFinanceDescription') },
    { tone: 'traceability', label: t('guideTraceabilityLabel'), description: t('guideTraceabilityDescription') },
    { tone: 'risk', label: t('guideRiskLabel'), description: t('guideRiskDescription') },
  ];

  const statusChips: Array<{ label: string; color: 'success' | 'info' | 'warning' | 'default' }> = [
    { label: t('statusReady'), color: 'success' },
    { label: t('statusTraceable'), color: 'info' },
    { label: t('statusAttention'), color: 'warning' },
    { label: t('statusInactive'), color: 'default' },
  ];

  const primary = resolveKpiTonePalette(theme, 'production');
  const commercial = resolveKpiTonePalette(theme, 'commercial');

  function handleSelectPeriod(period: DashboardPeriod) {
    if (period === 'custom') {
      setFilterMode('custom');

      const window = summary?.window ?? (activeFilter.period === 'custom' ? toWindow(activeFilter) : null);
      if (window?.from && window.to) {
        setCustomRangeDraft({ from: window.from, to: window.to });
      }

      return;
    }

    setFilterMode(period);
    setActiveFilter({ period });
  }

  function handleApplyCustomRange() {
    if (customRangeError) {
      return;
    }

    setFilterMode('custom');
    setActiveFilter({
      period: 'custom',
      from: customRangeDraft.from,
      to: customRangeDraft.to,
    });
  }

  return (
    <Stack sx={{ gap: 3 }}>
      <Card
        variant="outlined"
        sx={{
          position: 'relative',
          overflow: 'hidden',
          borderColor: 'divider',
          backgroundColor: 'background.paper',
          backgroundImage: `radial-gradient(circle at top right, ${withAlpha(primary.main, 0.16)} 0%, transparent 38%), radial-gradient(circle at bottom left, ${withAlpha(commercial.main, 0.12)} 0%, transparent 42%)`,
        }}
      >
        <CardContent sx={{ p: { xs: 3, md: 4 }, display: 'grid', gap: 2.5 }}>
          <Chip
            size="small"
            label={t('heroEyebrow')}
            sx={{
              width: 'fit-content',
              bgcolor: withAlpha(primary.main, 0.12),
              color: primary.dark,
              border: `1px solid ${withAlpha(primary.main, 0.18)}`,
              fontWeight: 600,
            }}
          />

          <Box sx={{ maxWidth: 860 }}>
            <Typography variant="h3" sx={{ mb: 1.25, fontWeight: 700 }}>
              {t('heroTitle')}
            </Typography>
            <Typography variant="body1" sx={{ color: 'text.secondary', lineHeight: 1.8 }}>
              {t('heroBody')}
            </Typography>
          </Box>

          <Box sx={{ display: 'grid', gap: 1 }}>
            <Typography variant="overline" sx={{ color: 'text.secondary', fontWeight: 700, letterSpacing: '0.08em' }}>
              {t('periodLabel')}
            </Typography>
            <Stack direction="row" sx={{ flexWrap: 'wrap', gap: 1 }}>
              {dashboardPeriods.map((period) => {
                const isActive = period === filterMode;

                return (
                  <Chip
                    key={period}
                    clickable
                    size="small"
                    label={periodLabels[period]}
                    color={isActive ? 'primary' : 'default'}
                    variant={isActive ? 'filled' : 'outlined'}
                    onClick={() => handleSelectPeriod(period)}
                    sx={{ fontWeight: isActive ? 700 : 600 }}
                  />
                );
              })}
            </Stack>
            {filterMode === 'custom' ? (
              <Stack direction={{ xs: 'column', md: 'row' }} sx={{ gap: 1.5, alignItems: { md: 'flex-end' } }}>
                <TextField
                  size="small"
                  type="date"
                  label={t('customFromLabel')}
                  value={customRangeDraft.from}
                  onChange={(event) => setCustomRangeDraft((current) => ({ ...current, from: event.target.value }))}
                  InputLabelProps={{ shrink: true }}
                />
                <TextField
                  size="small"
                  type="date"
                  label={t('customToLabel')}
                  value={customRangeDraft.to}
                  onChange={(event) => setCustomRangeDraft((current) => ({ ...current, to: event.target.value }))}
                  InputLabelProps={{ shrink: true }}
                />
                <Button
                  variant="contained"
                  color="primary"
                  onClick={handleApplyCustomRange}
                  disabled={loading || Boolean(customRangeError)}
                >
                  {t('applyCustomRange')}
                </Button>
              </Stack>
            ) : null}
            {filterMode === 'custom' && customRangeError ? (
              <Typography variant="caption" sx={{ color: 'warning.dark' }}>
                {customRangeError}
              </Typography>
            ) : null}
            <Typography variant="body2" sx={{ color: 'text.secondary' }}>
              {t('periodHint')}
            </Typography>
            {summary ? (
              <Typography variant="caption" sx={{ color: 'text.secondary' }}>
                {previousWindowLabel
                  ? t('comparisonHint', { period: previousWindowLabel })
                  : t('comparisonUnavailableHint')}
              </Typography>
            ) : null}
          </Box>

          <Stack
            direction={{ xs: 'column', md: 'row' }}
            sx={{ alignItems: { xs: 'flex-start', md: 'center' }, justifyContent: 'space-between', gap: 2 }}
          >
            <Typography variant="body2" sx={{ color: 'text.secondary' }}>
              {t('heroSupport', { period: activeWindowLabel })}
            </Typography>
            <Stack direction="row" sx={{ flexWrap: 'wrap', gap: 1 }}>
              <Chip size="small" label={`${t('apiBaseLabel')} ${apiUrl}`} variant="outlined" color="info" />
              <Chip
                size="small"
                label={loading ? t('liveDataLoading') : summary ? t('liveDataReady') : t('liveDataUnavailable')}
                color={summary ? 'success' : error ? 'warning' : 'default'}
              />
            </Stack>
          </Stack>
        </CardContent>
      </Card>

      {error ? <Alert severity="warning">{t('summaryLoadError', { reason: error })}</Alert> : null}

      <Box
        sx={{
          display: 'grid',
          gridTemplateColumns: { xs: '1fr', md: 'repeat(2, minmax(0, 1fr))', xl: 'repeat(4, minmax(0, 1fr))' },
          gap: 2.5,
        }}
      >
        {loading
          ? Array.from({ length: 4 }).map((_, index) => (
              <Card key={index} variant="outlined" sx={{ height: '100%' }}>
                <CardContent sx={{ p: 3, display: 'grid', gap: 2.25 }}>
                  <Stack direction="row" sx={{ alignItems: 'flex-start', justifyContent: 'space-between', gap: 2 }}>
                    <Box sx={{ flexGrow: 1 }}>
                      <Skeleton variant="text" width="38%" height={24} />
                      <Skeleton variant="text" width="62%" height={54} sx={{ mt: 0.5 }} />
                    </Box>
                    <Skeleton variant="rounded" width={52} height={52} />
                  </Stack>
                  <Stack direction="row" sx={{ gap: 1, alignItems: 'center' }}>
                    <Skeleton variant="rounded" width={120} height={28} />
                    <Skeleton variant="text" width={108} height={20} />
                  </Stack>
                  <Skeleton variant="text" width="100%" height={20} />
                  <Skeleton variant="text" width="90%" height={20} />
                </CardContent>
              </Card>
            ))
          : metrics.map((metric) => <KpiMetricCard key={metric.title} {...metric} />)}
      </Box>

      <Box
        sx={{
          display: 'grid',
          gridTemplateColumns: { xs: '1fr', xl: 'minmax(0, 1.15fr) minmax(0, 0.95fr)' },
          gap: 2.5,
        }}
      >
        <Card variant="outlined">
          <CardContent sx={{ p: 3, display: 'grid', gap: 2.5 }}>
            <Box>
              <Typography variant="h5" sx={{ fontWeight: 700 }}>
                {t('focusTitle')}
              </Typography>
              <Typography variant="body2" sx={{ mt: 1, color: 'text.secondary', lineHeight: 1.7 }}>
                {t('focusBody')}
              </Typography>
            </Box>

            <Stack sx={{ gap: 1.5 }}>
              <Stack direction="row" sx={{ alignItems: 'flex-start', gap: 1.25 }}>
                <Chip size="small" label={t('guideProductionLabel')} color="primary" />
                <Typography variant="body2" sx={{ color: 'text.secondary' }}>
                  {t('focusProduction')}
                </Typography>
              </Stack>
              <Stack direction="row" sx={{ alignItems: 'flex-start', gap: 1.25 }}>
                <Chip size="small" label={t('guideCommercialLabel')} color="secondary" />
                <Typography variant="body2" sx={{ color: 'text.secondary' }}>
                  {t('focusCommercial')}
                </Typography>
              </Stack>
              <Stack direction="row" sx={{ alignItems: 'flex-start', gap: 1.25 }}>
                <Chip size="small" label={t('guideTraceabilityLabel')} color="info" />
                <Typography variant="body2" sx={{ color: 'text.secondary' }}>
                  {t('focusTraceability')}
                </Typography>
              </Stack>
            </Stack>
          </CardContent>
        </Card>

        <Card variant="outlined">
          <CardContent sx={{ p: 3, display: 'grid', gap: 2.5 }}>
            <Box>
              <Typography variant="h5" sx={{ fontWeight: 700 }}>
                {t('guideTitle')}
              </Typography>
              <Typography variant="body2" sx={{ mt: 1, color: 'text.secondary', lineHeight: 1.7 }}>
                {t('guideBody')}
              </Typography>
            </Box>

            <Stack sx={{ gap: 1.5 }}>
              {guideItems.map((item) => {
                const palette = resolveKpiTonePalette(theme, item.tone);

                return (
                  <Stack key={item.label} direction="row" sx={{ gap: 1.25, alignItems: 'flex-start' }}>
                    <Chip
                      size="small"
                      label={item.label}
                      sx={{
                        bgcolor: withAlpha(palette.main, 0.12),
                        color: palette.dark,
                        border: `1px solid ${withAlpha(palette.main, 0.18)}`,
                        fontWeight: 600,
                        minWidth: 118,
                        justifyContent: 'center',
                      }}
                    />
                    <Typography variant="body2" sx={{ color: 'text.secondary' }}>
                      {item.description}
                    </Typography>
                  </Stack>
                );
              })}
            </Stack>
          </CardContent>
        </Card>
      </Box>

      <Card variant="outlined">
        <CardContent sx={{ p: 3, display: 'grid', gap: 2.5 }}>
          <Box>
            <Typography variant="h5" sx={{ fontWeight: 700 }}>
              {t('statusTitle')}
            </Typography>
            <Typography variant="body2" sx={{ mt: 1, color: 'text.secondary', lineHeight: 1.7 }}>
              {t('statusBody')}
            </Typography>
          </Box>

          <Stack direction="row" sx={{ flexWrap: 'wrap', gap: 1 }}>
            {statusChips.map((chip) => (
              <Chip key={chip.label} size="small" color={chip.color} label={chip.label} />
            ))}
          </Stack>

          <Divider />

          <Box
            sx={{
              display: 'grid',
              gridTemplateColumns: { xs: '1fr', md: 'repeat(3, minmax(0, 1fr))' },
              gap: 1.5,
            }}
          >
            <Alert severity="success">{t('alertSuccess')}</Alert>
            <Alert severity="info">{t('alertInfo')}</Alert>
            <Alert severity="warning" icon={<IconScale size={18} stroke={1.8} />}>
              {t('alertWarning')}
            </Alert>
          </Box>
        </CardContent>
      </Card>
    </Stack>
  );
}

type CustomRangeDraft = {
  from: string;
  to: string;
};

function createDefaultCustomRange(): CustomRangeDraft {
  const today = new Date();
  const from = new Date(today);
  from.setDate(from.getDate() - 29);

  return {
    from: formatDateInputValue(from),
    to: formatDateInputValue(today),
  };
}

function formatDateInputValue(date: Date) {
  return date.toISOString().slice(0, 10);
}

function toWindow(filter: DashboardSummaryFilter): DashboardSummaryWindow {
  return {
    period: filter.period,
    from: filter.from ?? null,
    to: filter.to ?? null,
    comparisonAvailable: filter.period !== 'all',
  };
}

function getCustomRangeValidationMessage(range: CustomRangeDraft, t: (key: string, params?: Record<string, string | number>) => string) {
  if (!range.from || !range.to) {
    return t('customRangeErrorRequired');
  }

  if (range.from > range.to) {
    return t('customRangeErrorOrder');
  }

  return null;
}

function getDashboardWindowLabel(
  locale: string,
  window: DashboardSummaryWindow,
  periodLabels: Record<DashboardPeriod, string>
) {
  if (window.from && window.to) {
    return formatDateRange(locale, window.from, window.to);
  }

  return periodLabels[window.period] ?? window.period;
}

function formatDateRange(locale: string, from: string, to: string) {
  const formatter = new Intl.DateTimeFormat(locale, { dateStyle: 'medium' });
  const fromDate = new Date(`${from}T12:00:00`);
  const toDate = new Date(`${to}T12:00:00`);

  return `${formatter.format(fromDate)} - ${formatter.format(toDate)}`;
}

function getDashboardMetricIcon(tone: KpiTone): ReactNode {
  switch (tone) {
    case 'commercial':
      return <IconReceipt size={24} stroke={1.8} />;
    case 'finance':
      return <IconCoins size={24} stroke={1.8} />;
    case 'traceability':
      return <IconPackage size={24} stroke={1.8} />;
    case 'production':
    default:
      return <IconBuildingStore size={24} stroke={1.8} />;
  }
}
