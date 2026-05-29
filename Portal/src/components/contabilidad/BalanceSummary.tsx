'use client';

import { useEffect, useState } from 'react';

import Alert from '@mui/material/Alert';
import Box from '@mui/material/Box';
import Card from '@mui/material/Card';
import CardContent from '@mui/material/CardContent';
import CircularProgress from '@mui/material/CircularProgress';
import Divider from '@mui/material/Divider';
import Grid from '@mui/material/Grid';
import Stack from '@mui/material/Stack';
import Typography from '@mui/material/Typography';

import { useLocale, useTranslations } from '@/hooks/useTranslations';
import { apiFetch } from '@/lib/api/client';
import { useAuth } from '@/lib/auth/AuthProvider';
import { formatCurrency } from '@/lib/dashboard/summary-metrics';

interface CostoResponse {
  costoId: number;
  nombre: string;
  tipoCosto: string;
  fuenteModulo?: string | null;
  monto: number;
  fecha?: string | null;
  estado: boolean;
}

interface GastoResponse {
  gastoId: number;
  nombre: string;
  monto: number;
  fecha?: string | null;
  estado: boolean;
}

interface PasivoResponse {
  pasivoId: number;
  nombre: string;
  monto: number;
  fecha?: string | null;
  estado: boolean;
}

function sumActivos<T extends { monto: number; estado: boolean }>(items: T[]) {
  return items.filter((x) => x.estado).reduce((acc, x) => acc + x.monto, 0);
}

interface SummaryCardProps {
  title: string;
  amount: number;
  subtitle: string;
  color?: 'primary' | 'error' | 'warning';
  locale: string;
}

function SummaryCard({ title, amount, subtitle, color = 'primary', locale }: SummaryCardProps) {
  const colorMap = {
    primary: 'primary.main',
    error: 'error.main',
    warning: 'warning.main',
  };

  return (
    <Card variant="outlined" sx={{ height: '100%' }}>
      <CardContent>
        <Typography variant="subtitle2" color="text.secondary" gutterBottom>
          {title}
        </Typography>
        <Typography variant="h5" fontWeight="bold" color={colorMap[color]}>
          {formatCurrency(locale, amount)}
        </Typography>
        <Typography variant="caption" color="text.secondary">
          {subtitle}
        </Typography>
      </CardContent>
    </Card>
  );
}

export function BalanceSummary() {
  const nav = useTranslations('navigation');
  const locale = useLocale();
  const { has } = useAuth();

  const canVerCostos = has('Contabilidad.Costos', 'Ver');
  const canVerGastos = has('Contabilidad.Gastos', 'Ver');
  const canVerPasivos = has('Contabilidad.Pasivos', 'Ver');

  const [costos, setCostos] = useState<CostoResponse[]>([]);
  const [gastos, setGastos] = useState<GastoResponse[]>([]);
  const [pasivos, setPasivos] = useState<PasivoResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    async function load() {
      setLoading(true);
      setError(null);
      try {
        const fetches = await Promise.allSettled([
          canVerCostos ? apiFetch<CostoResponse[]>('/api/v1/costos') : Promise.resolve([]),
          canVerGastos ? apiFetch<GastoResponse[]>('/api/v1/gastos') : Promise.resolve([]),
          canVerPasivos ? apiFetch<PasivoResponse[]>('/api/v1/pasivos') : Promise.resolve([]),
        ]);

        if (fetches[0].status === 'fulfilled') setCostos(fetches[0].value ?? []);
        if (fetches[1].status === 'fulfilled') setGastos(fetches[1].value ?? []);
        if (fetches[2].status === 'fulfilled') setPasivos(fetches[2].value ?? []);

        const anyError = fetches.some((r) => r.status === 'rejected');
        if (anyError && !fetches.some((r) => r.status === 'fulfilled')) {
          setError('Error al cargar los datos de balance.');
        }
      } catch {
        setError('Error al cargar los datos de balance.');
      } finally {
        setLoading(false);
      }
    }
    load();
  }, [canVerCostos, canVerGastos, canVerPasivos]);

  if (loading) {
    return (
      <Box display="flex" justifyContent="center" py={6}>
        <CircularProgress />
      </Box>
    );
  }

  if (error) {
    return <Alert severity="error">{error}</Alert>;
  }

  const totalCostos = sumActivos(costos);
  const totalGastos = sumActivos(gastos);
  const totalPasivos = sumActivos(pasivos);
  const totalEgresos = totalCostos + totalGastos;
  const posicionNeta = -(totalEgresos + totalPasivos);

  return (
    <Stack spacing={3}>
      <Grid container spacing={2}>
        {canVerCostos && (
          <Grid size={{ xs: 12, sm: 6, md: 3 }}>
            <SummaryCard
              title={nav('costos')}
              amount={totalCostos}
              subtitle={`${costos.filter((c) => c.estado).length} activos`}
              color="warning"
              locale={locale}
            />
          </Grid>
        )}
        {canVerGastos && (
          <Grid size={{ xs: 12, sm: 6, md: 3 }}>
            <SummaryCard
              title={nav('gastos')}
              amount={totalGastos}
              subtitle={`${gastos.filter((g) => g.estado).length} activos`}
              color="warning"
              locale={locale}
            />
          </Grid>
        )}
        {canVerPasivos && (
          <Grid size={{ xs: 12, sm: 6, md: 3 }}>
            <SummaryCard
              title={nav('pasivos')}
              amount={totalPasivos}
              subtitle={`${pasivos.filter((p) => p.estado).length} activos`}
              color="error"
              locale={locale}
            />
          </Grid>
        )}
        <Grid size={{ xs: 12, sm: 6, md: 3 }}>
          <SummaryCard
            title="Total egresos"
            amount={totalEgresos}
            subtitle="Costos + Gastos activos"
            color="error"
            locale={locale}
          />
        </Grid>
      </Grid>

      <Divider />

      <Card variant="outlined">
        <CardContent>
          <Typography variant="subtitle1" fontWeight="bold" gutterBottom>
            Resumen de posición
          </Typography>
          <Stack spacing={1}>
            {canVerCostos && (
              <Box display="flex" justifyContent="space-between">
                <Typography color="text.secondary">{nav('costos')} activos</Typography>
                <Typography color="warning.main" fontWeight="medium">
                  - {formatCurrency(locale, totalCostos)}
                </Typography>
              </Box>
            )}
            {canVerGastos && (
              <Box display="flex" justifyContent="space-between">
                <Typography color="text.secondary">{nav('gastos')} activos</Typography>
                <Typography color="warning.main" fontWeight="medium">
                  - {formatCurrency(locale, totalGastos)}
                </Typography>
              </Box>
            )}
            {canVerPasivos && (
              <Box display="flex" justifyContent="space-between">
                <Typography color="text.secondary">{nav('pasivos')} activos</Typography>
                <Typography color="error.main" fontWeight="medium">
                  - {formatCurrency(locale, totalPasivos)}
                </Typography>
              </Box>
            )}
            <Divider />
            <Box display="flex" justifyContent="space-between">
              <Typography fontWeight="bold">Posición neta (egresos + pasivos)</Typography>
              <Typography
                fontWeight="bold"
                color={posicionNeta >= 0 ? 'success.main' : 'error.main'}
              >
                {formatCurrency(locale, posicionNeta)}
              </Typography>
            </Box>
          </Stack>
        </CardContent>
      </Card>
    </Stack>
  );
}
