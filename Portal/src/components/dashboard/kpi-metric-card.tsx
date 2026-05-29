'use client';

import type { ReactNode } from 'react';

import Box from '@mui/material/Box';
import Card from '@mui/material/Card';
import CardContent from '@mui/material/CardContent';
import Chip from '@mui/material/Chip';
import Stack from '@mui/material/Stack';
import Typography from '@mui/material/Typography';
import { useTheme, type Theme } from '@mui/material/styles';

import { withAlpha } from '@/theme/berry/color-utils';

export type KpiTone = 'production' | 'commercial' | 'finance' | 'traceability' | 'quality' | 'risk';

export type KpiTrendIntent = 'positive' | 'negative' | 'neutral';

type TonePalette = Theme['palette']['primary'];

export function resolveKpiTonePalette(theme: Theme, tone: KpiTone): TonePalette {
  const vars = theme.vars ?? { palette: theme.palette };

  switch (tone) {
    case 'commercial':
      return vars.palette.secondary;
    case 'finance':
      return vars.palette.orange;
    case 'traceability':
      return vars.palette.info;
    case 'quality':
      return vars.palette.success;
    case 'risk':
      return vars.palette.warning;
    case 'production':
    default:
      return vars.palette.primary;
  }
}

export type KpiMetricCardProps = {
  tone: KpiTone;
  title: string;
  value: string;
  description: string;
  trendLabel?: string;
  trendIntent?: KpiTrendIntent;
  caption?: string;
  icon?: ReactNode;
};

export function KpiMetricCard({
  tone,
  title,
  value,
  description,
  trendLabel,
  trendIntent,
  caption,
  icon,
}: KpiMetricCardProps) {
  const theme = useTheme();
  const vars = theme.vars ?? { palette: theme.palette };
  const palette = resolveKpiTonePalette(theme, tone);
  const trendPalette = trendIntent === 'positive'
    ? vars.palette.success
    : trendIntent === 'negative'
      ? vars.palette.warning
      : palette;

  return (
    <Card
      variant="outlined"
      sx={{
        position: 'relative',
        height: '100%',
        overflow: 'hidden',
        borderColor: withAlpha(palette.main, 0.18),
        backgroundColor: 'background.paper',
      }}
    >
      <Box
        sx={{
          position: 'absolute',
          left: 0,
          right: 0,
          top: 0,
          height: 4,
          backgroundColor: palette.main,
        }}
      />

      <CardContent sx={{ p: 3, display: 'grid', gap: 2.25 }}>
        <Stack direction="row" sx={{ alignItems: 'flex-start', justifyContent: 'space-between', gap: 2 }}>
          <Box sx={{ minWidth: 0 }}>
            <Typography
              variant="overline"
              sx={{ color: palette.dark, fontWeight: 700, letterSpacing: '0.08em' }}
            >
              {title}
            </Typography>
            <Typography variant="h3" sx={{ mt: 0.75, fontWeight: 700 }}>
              {value}
            </Typography>
          </Box>

          {icon ? (
            <Box
              sx={{
                width: 52,
                height: 52,
                flexShrink: 0,
                display: 'grid',
                placeItems: 'center',
                borderRadius: '18px',
                color: palette.dark,
                backgroundColor: withAlpha(palette.main, 0.12),
                border: `1px solid ${withAlpha(palette.main, 0.16)}`,
              }}
            >
              {icon}
            </Box>
          ) : null}
        </Stack>

        {(trendLabel || caption) ? (
          <Stack direction="row" sx={{ flexWrap: 'wrap', gap: 1, alignItems: 'center' }}>
            {trendLabel ? (
              <Chip
                size="small"
                label={trendLabel}
                sx={{
                  color: trendPalette.dark,
                  backgroundColor: withAlpha(trendPalette.main, 0.12),
                  border: `1px solid ${withAlpha(trendPalette.main, 0.18)}`,
                  fontWeight: 600,
                }}
              />
            ) : null}

            {caption ? (
              <Typography variant="caption" sx={{ color: 'text.secondary' }}>
                {caption}
              </Typography>
            ) : null}
          </Stack>
        ) : null}

        <Typography variant="body2" sx={{ color: 'text.secondary', lineHeight: 1.7 }}>
          {description}
        </Typography>
      </CardContent>
    </Card>
  );
}