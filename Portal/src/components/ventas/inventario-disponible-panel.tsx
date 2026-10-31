'use client';

import { useEffect, useMemo, useState } from 'react';

import { useLocale, useTranslations } from '@/hooks/useTranslations';
import { apiFetch } from '@/lib/api/client';
import {
  type ExistenciaCafe,
  type LoteCafeResumen,
  type PresentacionCafe,
  type TipoCafe,
} from '@/lib/inventario/cafe';


import Alert from '@mui/material/Alert';

import Button from '@mui/material/Button';
import Card from '@mui/material/Card';
import CardContent from '@mui/material/CardContent';
import CardHeader from '@mui/material/CardHeader';
import FormControl from '@mui/material/FormControl';
import InputLabel from '@mui/material/InputLabel';
import MenuItem from '@mui/material/MenuItem';
import Select, { type SelectChangeEvent } from '@mui/material/Select';
import Stack from '@mui/material/Stack';
import Table from '@mui/material/Table';
import TableBody from '@mui/material/TableBody';
import TableCell from '@mui/material/TableCell';
import TableContainer from '@mui/material/TableContainer';
import TableHead from '@mui/material/TableHead';
import TableRow from '@mui/material/TableRow';

import Typography from '@mui/material/Typography';

type ConsolidatedExistencia = ExistenciaCafe & { loteCodigo: string };

export type InventarioDisponiblePanelProps = {
  canQuery: boolean;
  canLoadCatalogs: boolean;
};

export function InventarioDisponiblePanel({
  canQuery,
  canLoadCatalogs,
}: InventarioDisponiblePanelProps) {
  const tc = useTranslations('common');
  const tf = useTranslations('fields');
  const ts = useTranslations('sales');
  const ti = useTranslations('inventory');
  const locale = useLocale();

  const [lotes, setLotes] = useState<LoteCafeResumen[]>([]);
  const [presentaciones, setPresentaciones] = useState<PresentacionCafe[]>([]);
  const [tiposCafe, setTiposCafe] = useState<TipoCafe[]>([]);
  const [existencias, setExistencias] = useState<ConsolidatedExistencia[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [loteFiltro, setLoteFiltro] = useState<string>('all');
  const [presentacionFiltro, setPresentacionFiltro] = useState<string>('all');
  const [tipoCafeFiltro, setTipoCafeFiltro] = useState<string>('all');

  async function loadAll() {
    if (!canQuery) {
      setLotes([]);
      setExistencias([]);
      setError(null);
      setLoading(false);
      return;
    }

    setLoading(true);
    setError(null);

    try {
      const lotesList = await apiFetch<LoteCafeResumen[]>('/api/v1/inventario/lotes');
      setLotes(lotesList);

      const selectedLotes =
        loteFiltro === 'all'
          ? lotesList
          : lotesList.filter((lote) => String(lote.loteCafeId) === loteFiltro);

      const responses = await Promise.all(
        selectedLotes.map(async (lote) => {
          const list = await apiFetch<ExistenciaCafe[]>(
            `/api/v1/inventario/lotes/${lote.loteCafeId}/existencias`,
          );
          return list.map((existencia) => ({
            ...existencia,
            loteCodigo: lote.codigoLote,
          }));
        }),
      );

      setExistencias(responses.flat());
    } catch (requestError) {
      setError(
        requestError instanceof Error ? requestError.message : ts('inventoryLoadError'),
      );
    } finally {
      setLoading(false);
    }
  }

  async function loadCatalogs() {
    if (!canLoadCatalogs) return;

    try {
      const [presentacionesList, tiposList] = await Promise.all([
        apiFetch<PresentacionCafe[]>('/api/v1/inventario/presentaciones'),
        apiFetch<TipoCafe[]>('/api/v1/inventario/tipos-cafe'),
      ]);
      setPresentaciones(presentacionesList);
      setTiposCafe(tiposList);
    } catch {
      // Los filtros de catálogo son opcionales; no bloquean la consulta principal.
    }
  }

  useEffect(() => {
    void loadAll();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [canQuery, loteFiltro]);

  useEffect(() => {
    void loadCatalogs();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [canLoadCatalogs]);

  const filteredExistencias = useMemo(() => {
    return existencias.filter((existencia) => {
      if (existencia.cantidadPaquetes <= 0) return false;
      if (presentacionFiltro !== 'all' && String(existencia.presentacionCafeId) !== presentacionFiltro) {
        return false;
      }
      if (tipoCafeFiltro !== 'all' && String(existencia.tipoCafeId) !== tipoCafeFiltro) {
        return false;
      }
      return true;
    });
  }, [existencias, presentacionFiltro, tipoCafeFiltro]);

  function clearFilters() {
    setLoteFiltro('all');
    setPresentacionFiltro('all');
    setTipoCafeFiltro('all');
  }

  if (!canQuery) {
    return (
      <Alert severity="info">{ts('inventoryNoPermission')}</Alert>
    );
  }

  return (
    <Card variant="outlined">
      <CardHeader
        title={ts('inventoryTitle')}
        subheader={ts('inventoryCount', { count: filteredExistencias.length })}
        action={
          <Button
            variant="outlined"
            size="small"
            onClick={() => void loadAll()}
            disabled={loading}
          >
            {tc('refresh')}
          </Button>
        }
      />
      <CardContent sx={{ pt: 0 }}>
        <Stack spacing={2}>
          {error ? <Alert severity="error">{error}</Alert> : null}

          <Stack
            direction={{ xs: 'column', md: 'row' }}
            spacing={2}
            alignItems={{ xs: 'stretch', md: 'center' }}
            useFlexGap
            flexWrap="wrap"
          >
            <FormControl size="small" sx={{ minWidth: { xs: '100%', md: 220 } }}>
              <InputLabel id="ventas-lote-filter-label">{ts('filterByLot')}</InputLabel>
              <Select
                labelId="ventas-lote-filter-label"
                label={ts('filterByLot')}
                value={loteFiltro}
                onChange={(event: SelectChangeEvent) => setLoteFiltro(event.target.value)}
              >
                <MenuItem value="all">{ts('allLots')}</MenuItem>
                {lotes.map((lote) => (
                  <MenuItem key={lote.loteCafeId} value={String(lote.loteCafeId)}>
                    {lote.codigoLote}
                  </MenuItem>
                ))}
              </Select>
            </FormControl>

            <FormControl size="small" sx={{ minWidth: { xs: '100%', md: 220 } }}>
              <InputLabel id="ventas-presentacion-filter-label">{tf('presentation')}</InputLabel>
              <Select
                labelId="ventas-presentacion-filter-label"
                label={tf('presentation')}
                value={presentacionFiltro}
                onChange={(event: SelectChangeEvent) => setPresentacionFiltro(event.target.value)}
                disabled={!canLoadCatalogs}
              >
                <MenuItem value="all">{ts('allPresentations')}</MenuItem>
                {presentaciones.map((presentacion) => (
                  <MenuItem
                    key={presentacion.presentacionCafeId}
                    value={String(presentacion.presentacionCafeId)}
                  >
                    {presentacion.nombre}
                  </MenuItem>
                ))}
              </Select>
            </FormControl>

            <FormControl size="small" sx={{ minWidth: { xs: '100%', md: 220 } }}>
              <InputLabel id="ventas-tipo-cafe-filter-label">{tf('coffeeType')}</InputLabel>
              <Select
                labelId="ventas-tipo-cafe-filter-label"
                label={tf('coffeeType')}
                value={tipoCafeFiltro}
                onChange={(event: SelectChangeEvent) => setTipoCafeFiltro(event.target.value)}
                disabled={!canLoadCatalogs}
              >
                <MenuItem value="all">{ts('allCoffeeTypes')}</MenuItem>
                {tiposCafe.map((tipoCafe) => (
                  <MenuItem key={tipoCafe.tipoCafeId} value={String(tipoCafe.tipoCafeId)}>
                    {tipoCafe.nombre}
                  </MenuItem>
                ))}
              </Select>
            </FormControl>

            <Button
              variant="text"
              onClick={clearFilters}
              disabled={
                loteFiltro === 'all' &&
                presentacionFiltro === 'all' &&
                tipoCafeFiltro === 'all'
              }
            >
              {ti('clearFilters')}
            </Button>
          </Stack>

          {loading ? (
            <Typography variant="body2" color="text.secondary">
              {tc('loading')}
            </Typography>
          ) : filteredExistencias.length === 0 ? (
            <Alert severity="info">{ts('noAvailableInventory')}</Alert>
          ) : (
            <TableContainer>
              <Table size="small" sx={{ minWidth: 720 }}>
                <TableHead>
                  <TableRow>
                    <TableCell>{tf('lotCode')}</TableCell>
                    <TableCell>{tf('presentation')}</TableCell>
                    <TableCell>{tf('coffeeType')}</TableCell>
                    <TableCell align="right">{tf('packages')}</TableCell>
                    <TableCell align="right">{tf('netWeight')}</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {filteredExistencias.map((existencia) => (
                    <TableRow key={existencia.loteCafeExistenciaId} hover>
                      <TableCell>{existencia.loteCodigo}</TableCell>
                      <TableCell>
                        <Stack spacing={0.25}>
                          <Typography variant="body2">{existencia.presentacionNombre}</Typography>
                          <Typography variant="caption" color="text.secondary">
                            {existencia.presentacionCodigo}
                          </Typography>
                        </Stack>
                      </TableCell>
                      <TableCell>
                        <Stack spacing={0.25}>
                          <Typography variant="body2">{existencia.tipoCafeNombre}</Typography>
                          <Typography variant="caption" color="text.secondary">
                            {existencia.tipoCafeCodigo}
                          </Typography>
                        </Stack>
                      </TableCell>
                      <TableCell align="right">
                        {new Intl.NumberFormat(locale, { maximumFractionDigits: 0 }).format(
                          existencia.cantidadPaquetes,
                        )}
                      </TableCell>
                      <TableCell align="right">
                        {new Intl.NumberFormat(locale, {
                          minimumFractionDigits: 0,
                          maximumFractionDigits: 3,
                        }).format(existencia.pesoNetoKg)}{' '}
                        kg
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </TableContainer>
          )}
        </Stack>
      </CardContent>
    </Card>
  );
}