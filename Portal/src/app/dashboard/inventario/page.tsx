'use client';

import { useEffect, useMemo, useState, type ReactNode } from 'react';

import { IconSearch } from '@tabler/icons-react';

import { useLocale, useTranslations } from '@/hooks/useTranslations';
import { apiFetch } from '@/lib/api/client';
import { useAuth } from '@/lib/auth/AuthProvider';
import {
  estadoLoteColor,
  estadoLoteKey,
  filterLotes,
  formatDate,
  formatDateTime,
  formatGrams,
  formatKg,
  formatNumber,
  isCatalogoActivo,
  tipoMovimientoColor,
  tipoMovimientoKey,
  type EstadoLoteCafe,
  type EstadoLoteFiltro,
  type ExistenciaCafe,
  type LoteCafeResumen,
  type MovimientoCafe,
  type PresentacionCafe,
  type TipoCafe,
} from '@/lib/inventario/cafe';
import {
  toPresentacionCafeRow,
  toTipoCafeRow,
  type CatalogoCafeKind,
  type CatalogoCafeRow,
} from '@/lib/inventario/catalogos';
import {
  resolveInventoryError,
  type CreateLoteOperationResult,
} from '@/lib/inventario/operations';

import { CatalogoCafeDeleteDialog } from '@/components/inventario/catalogo-cafe-delete-dialog';
import {
  CatalogoCafeDialog,
  type CatalogoCafeSaved,
} from '@/components/inventario/catalogo-cafe-dialog';
import { CatalogoCafePanel } from '@/components/inventario/catalogo-cafe-panel';
import { CreateLoteDialog } from '@/components/inventario/create-lote-dialog';
import { MovimientoDialog } from '@/components/inventario/movimiento-dialog';

import Alert from '@mui/material/Alert';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import Card from '@mui/material/Card';
import CardContent from '@mui/material/CardContent';
import CardHeader from '@mui/material/CardHeader';
import Chip from '@mui/material/Chip';
import FormControl from '@mui/material/FormControl';
import InputAdornment from '@mui/material/InputAdornment';
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
import Tab from '@mui/material/Tab';
import Tabs from '@mui/material/Tabs';
import TextField from '@mui/material/TextField';
import Typography from '@mui/material/Typography';

const ESTADOS_LOTE: readonly EstadoLoteCafe[] = [1, 2, 3];

type InventoryTab = 'lotes' | 'presentaciones' | 'tiposCafe';

export default function InventarioCafePage() {
  const nav = useTranslations('navigation');
  const tc = useTranslations('common');
  const tf = useTranslations('fields');
  const tp = useTranslations('pages');
  const ti = useTranslations('inventory');
  const locale = useLocale();

  const { has } = useAuth();
  const canViewLotes = has('Inventario.Lotes', 'Ver');
  const canCreateLotes = has('Inventario.Lotes', 'Crear');
  const canViewExistencias = has('Inventario.Existencias', 'Ver');
  const canViewMovimientos = has('Inventario.Movimientos', 'Ver');
  const canCreateMovimientos = has('Inventario.Movimientos', 'Crear');
  const canViewPresentaciones = has('Inventario.Presentaciones', 'Ver');
  const canCreatePresentaciones = has('Inventario.Presentaciones', 'Crear');
  const canEditPresentaciones = has('Inventario.Presentaciones', 'Editar');
  const canDeletePresentaciones = has('Inventario.Presentaciones', 'Eliminar');
  const canViewTiposCafe = has('Inventario.TiposCafe', 'Ver');
  const canCreateTiposCafe = has('Inventario.TiposCafe', 'Crear');
  const canEditTiposCafe = has('Inventario.TiposCafe', 'Editar');
  const canDeleteTiposCafe = has('Inventario.TiposCafe', 'Eliminar');
  const canLoadCatalogos = canViewPresentaciones && canViewTiposCafe;
  const canAccessInventario =
    canViewLotes || canViewPresentaciones || canViewTiposCafe;

  const [items, setItems] = useState<LoteCafeResumen[]>([]);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [createReport, setCreateReport] = useState<CreateLoteOperationResult | null>(null);
  const [refreshToken, setRefreshToken] = useState(0);

  const [query, setQuery] = useState('');
  const [estadoFiltro, setEstadoFiltro] = useState<EstadoLoteFiltro>('all');
  const [selectedLoteId, setSelectedLoteId] = useState<number | null>(null);
  const [preferredLoteId, setPreferredLoteId] = useState<number | null>(null);

  const [createOpen, setCreateOpen] = useState(false);
  const [movementOpen, setMovementOpen] = useState(false);

  const [presentaciones, setPresentaciones] = useState<PresentacionCafe[]>([]);
  const [tiposCafe, setTiposCafe] = useState<TipoCafe[]>([]);
  const [presentacionesLoading, setPresentacionesLoading] = useState(false);
  const [presentacionesError, setPresentacionesError] = useState<string | null>(null);
  const [tiposCafeLoading, setTiposCafeLoading] = useState(false);
  const [tiposCafeError, setTiposCafeError] = useState<string | null>(null);

  const [presentacionDialogOpen, setPresentacionDialogOpen] = useState(false);
  const [editingPresentacion, setEditingPresentacion] = useState<CatalogoCafeRow | null>(null);
  const [tipoCafeDialogOpen, setTipoCafeDialogOpen] = useState(false);
  const [editingTipoCafe, setEditingTipoCafe] = useState<CatalogoCafeRow | null>(null);
  const [deleteCatalogOpen, setDeleteCatalogOpen] = useState(false);
  const [deleteCatalogKind, setDeleteCatalogKind] =
    useState<CatalogoCafeKind>('presentacion');
  const [deleteCatalogItem, setDeleteCatalogItem] = useState<CatalogoCafeRow | null>(null);

  const [activeTab, setActiveTab] = useState<InventoryTab | null>(null);

  const [existencias, setExistencias] = useState<ExistenciaCafe[]>([]);
  const [existenciasLoading, setExistenciasLoading] = useState(false);
  const [existenciasError, setExistenciasError] = useState<string | null>(null);

  const [movimientos, setMovimientos] = useState<MovimientoCafe[]>([]);
  const [movimientosLoading, setMovimientosLoading] = useState(false);
  const [movimientosError, setMovimientosError] = useState<string | null>(null);

  const availableTabs = useMemo<InventoryTab[]>(() => {
    const tabs: InventoryTab[] = [];
    if (canViewLotes) tabs.push('lotes');
    if (canViewPresentaciones) tabs.push('presentaciones');
    if (canViewTiposCafe) tabs.push('tiposCafe');
    return tabs;
  }, [canViewLotes, canViewPresentaciones, canViewTiposCafe]);

  const effectiveTab =
    activeTab && availableTabs.includes(activeTab)
      ? activeTab
      : (availableTabs[0] ?? 'lotes');

  const presentacionRows = useMemo(
    () => presentaciones.map(toPresentacionCafeRow),
    [presentaciones],
  );
  const tipoCafeRows = useMemo(() => tiposCafe.map(toTipoCafeRow), [tiposCafe]);

  const catalogsLoading = presentacionesLoading || tiposCafeLoading;
  const catalogsError = presentacionesError ?? tiposCafeError;
  const refreshLoading =
    effectiveTab === 'lotes'
      ? loading
      : effectiveTab === 'presentaciones'
        ? presentacionesLoading
        : tiposCafeLoading;

  const filteredLotes = useMemo(
    () => filterLotes(items, query, estadoFiltro),
    [items, query, estadoFiltro],
  );

  const selectedLote = useMemo(
    () => items.find((lote) => lote.loteCafeId === selectedLoteId) ?? null,
    [items, selectedLoteId],
  );

  const hasInactiveCatalogs = useMemo(
    () =>
      existencias.some(
        (existencia) =>
          !isCatalogoActivo(existencia.presentacionEstado) ||
          !isCatalogoActivo(existencia.tipoCafeEstado),
      ),
    [existencias],
  );

  async function loadLotes(): Promise<boolean> {
    if (!canViewLotes) {
      setItems([]);
      setLoadError(null);
      setLoading(false);
      return false;
    }

    setLoading(true);
    setLoadError(null);

    try {
      const list = await apiFetch<LoteCafeResumen[]>('/api/v1/inventario/lotes');
      setItems(list);
      return true;
    } catch (error) {
      setLoadError(error instanceof Error ? error.message : ti('errorLoadingLots'));
      return false;
    } finally {
      setLoading(false);
    }
  }

  async function loadPresentaciones(): Promise<boolean> {
    if (!canViewPresentaciones) {
      setPresentaciones([]);
      setPresentacionesError(null);
      setPresentacionesLoading(false);
      return false;
    }

    setPresentacionesLoading(true);
    setPresentacionesError(null);

    try {
      const list = await apiFetch<PresentacionCafe[]>(
        '/api/v1/inventario/presentaciones',
      );
      setPresentaciones(list);
      return true;
    } catch (error) {
      setPresentacionesError(
        resolveInventoryError(error, ti('errorLoadingPresentaciones'), {
          conflict: ti('conflictError'),
          forbidden: ti('forbiddenError'),
          notFound: ti('catalogNotFoundError'),
        }),
      );
      return false;
    } finally {
      setPresentacionesLoading(false);
    }
  }

  async function loadTiposCafe(): Promise<boolean> {
    if (!canViewTiposCafe) {
      setTiposCafe([]);
      setTiposCafeError(null);
      setTiposCafeLoading(false);
      return false;
    }

    setTiposCafeLoading(true);
    setTiposCafeError(null);

    try {
      const list = await apiFetch<TipoCafe[]>('/api/v1/inventario/tipos-cafe');
      setTiposCafe(list);
      return true;
    } catch (error) {
      setTiposCafeError(
        resolveInventoryError(error, ti('errorLoadingTiposCafe'), {
          conflict: ti('conflictError'),
          forbidden: ti('forbiddenError'),
          notFound: ti('catalogNotFoundError'),
        }),
      );
      return false;
    } finally {
      setTiposCafeLoading(false);
    }
  }

  async function loadCatalogos(): Promise<boolean> {
    if (!canLoadCatalogos) {
      setPresentacionesLoading(false);
      setTiposCafeLoading(false);
      return false;
    }

    const [presentacionesOk, tiposCafeOk] = await Promise.all([
      loadPresentaciones(),
      loadTiposCafe(),
    ]);

    return presentacionesOk && tiposCafeOk;
  }

  useEffect(() => {
    void loadLotes();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [canViewLotes]);

  useEffect(() => {
    void loadPresentaciones();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [canViewPresentaciones]);

  useEffect(() => {
    void loadTiposCafe();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [canViewTiposCafe]);

  useEffect(() => {
    if (preferredLoteId) {
      const preferredIsVisible = filteredLotes.some(
        (lote) => lote.loteCafeId === preferredLoteId,
      );

      if (preferredIsVisible) {
        if (selectedLoteId !== preferredLoteId) {
          setExistenciasLoading(canViewExistencias);
          setMovimientosLoading(canViewMovimientos);
          setSelectedLoteId(preferredLoteId);
        }
        setPreferredLoteId(null);
        return;
      }
    }

    if (filteredLotes.length === 0) {
      setSelectedLoteId(null);
      return;
    }

    const selectedIsVisible = filteredLotes.some(
      (lote) => lote.loteCafeId === selectedLoteId,
    );

    if (!selectedIsVisible) {
      setExistenciasLoading(canViewExistencias);
      setMovimientosLoading(canViewMovimientos);
      setSelectedLoteId(filteredLotes[0].loteCafeId);
    }
  }, [
    canViewExistencias,
    canViewMovimientos,
    filteredLotes,
    preferredLoteId,
    selectedLoteId,
  ]);

  useEffect(() => {
    if (!selectedLoteId || !canViewExistencias) {
      setExistencias([]);
      setExistenciasError(null);
      setExistenciasLoading(false);
      return;
    }

    let cancelled = false;
    setExistencias([]);
    setExistenciasError(null);
    setExistenciasLoading(true);

    void (async () => {
      try {
        const list = await apiFetch<ExistenciaCafe[]>(
          `/api/v1/inventario/lotes/${selectedLoteId}/existencias`,
        );
        if (!cancelled) setExistencias(list);
      } catch (error) {
        if (!cancelled) {
          setExistenciasError(
            error instanceof Error ? error.message : ti('errorLoadingExistencias'),
          );
        }
      } finally {
        if (!cancelled) setExistenciasLoading(false);
      }
    })();

    return () => {
      cancelled = true;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [selectedLoteId, canViewExistencias, refreshToken]);

  useEffect(() => {
    if (!selectedLoteId || !canViewMovimientos) {
      setMovimientos([]);
      setMovimientosError(null);
      setMovimientosLoading(false);
      return;
    }

    let cancelled = false;
    setMovimientos([]);
    setMovimientosError(null);
    setMovimientosLoading(true);

    void (async () => {
      try {
        const list = await apiFetch<MovimientoCafe[]>(
          `/api/v1/inventario/lotes/${selectedLoteId}/movimientos`,
        );
        if (!cancelled) setMovimientos(list);
      } catch (error) {
        if (!cancelled) {
          setMovimientosError(
            error instanceof Error ? error.message : ti('errorLoadingMovimientos'),
          );
        }
      } finally {
        if (!cancelled) setMovimientosLoading(false);
      }
    })();

    return () => {
      cancelled = true;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [selectedLoteId, canViewMovimientos, refreshToken]);

  async function onRefresh() {
    setSuccess(null);
    setCreateReport(null);

    if (effectiveTab === 'lotes') {
      const ok = await loadLotes();

      if (ok) {
        setRefreshToken((value) => value + 1);
        setSuccess(ti('refreshSuccess'));
      }
      return;
    }

    if (effectiveTab === 'presentaciones') {
      const ok = await loadPresentaciones();
      if (ok) setSuccess(ti('refreshSuccess'));
      return;
    }

    const ok = await loadTiposCafe();
    if (ok) setSuccess(ti('refreshSuccess'));
  }

  function openCreateDialog() {
    if (!canCreateLotes) return;

    setSuccess(null);
    setCreateReport(null);
    setCreateOpen(true);
    void loadCatalogos();
  }

  function openMovementDialog() {
    if (!selectedLote || !canCreateMovimientos || selectedLote.estado === 3) return;

    setSuccess(null);
    setCreateReport(null);
    setMovementOpen(true);
    void loadCatalogos();
  }

  function openPresentacionCreate() {
    if (!canCreatePresentaciones) return;

    setSuccess(null);
    setEditingPresentacion(null);
    setPresentacionDialogOpen(true);
  }

  function openPresentacionEdit(item: CatalogoCafeRow) {
    if (!canEditPresentaciones) return;

    setSuccess(null);
    setEditingPresentacion(item);
    setPresentacionDialogOpen(true);
  }

  function closePresentacionDialog() {
    setPresentacionDialogOpen(false);
    setEditingPresentacion(null);
  }

  function onPresentacionSaved(result: CatalogoCafeSaved) {
    setSuccess(
      ti(result.created ? 'presentacionCreated' : 'presentacionUpdated', {
        name: result.nombre,
      }),
    );
    setRefreshToken((value) => value + 1);
    void loadPresentaciones();
  }

  function openTipoCafeCreate() {
    if (!canCreateTiposCafe) return;

    setSuccess(null);
    setEditingTipoCafe(null);
    setTipoCafeDialogOpen(true);
  }

  function openTipoCafeEdit(item: CatalogoCafeRow) {
    if (!canEditTiposCafe) return;

    setSuccess(null);
    setEditingTipoCafe(item);
    setTipoCafeDialogOpen(true);
  }

  function closeTipoCafeDialog() {
    setTipoCafeDialogOpen(false);
    setEditingTipoCafe(null);
  }

  function onTipoCafeSaved(result: CatalogoCafeSaved) {
    setSuccess(
      ti(result.created ? 'tipoCafeCreated' : 'tipoCafeUpdated', {
        name: result.nombre,
      }),
    );
    setRefreshToken((value) => value + 1);
    void loadTiposCafe();
  }

  function openCatalogDelete(kind: CatalogoCafeKind, item: CatalogoCafeRow) {
    if (kind === 'presentacion' && !canDeletePresentaciones) return;
    if (kind === 'tipoCafe' && !canDeleteTiposCafe) return;

    setSuccess(null);
    setDeleteCatalogKind(kind);
    setDeleteCatalogItem(item);
    setDeleteCatalogOpen(true);
  }

  function closeCatalogDelete() {
    setDeleteCatalogOpen(false);
    setDeleteCatalogItem(null);
  }

  function onCatalogDeleted(item: CatalogoCafeRow) {
    setSuccess(
      ti(
        deleteCatalogKind === 'presentacion'
          ? 'presentacionDeleted'
          : 'tipoCafeDeleted',
        { name: item.nombre },
      ),
    );

    if (deleteCatalogKind === 'presentacion') {
      void loadPresentaciones();
    } else {
      void loadTiposCafe();
    }

    setRefreshToken((value) => value + 1);
  }

  function onLoteCreated(result: CreateLoteOperationResult) {
    setCreateOpen(false);
    setSuccess(null);
    setCreateReport(result);
    setPreferredLoteId(result.loteCafeId);
    setQuery('');
    setEstadoFiltro('all');

    void (async () => {
      const ok = await loadLotes();
      if (ok) setRefreshToken((value) => value + 1);
    })();
  }

  function onMovementSaved(movement: MovimientoCafe) {
    setMovementOpen(false);
    setCreateReport(null);
    setSuccess(
      ti('movementSuccessDetail', {
        type: ti(tipoMovimientoKey(movement.tipoMovimiento)),
      }),
    );
    setRefreshToken((value) => value + 1);
    void loadLotes();
  }

  function selectLote(loteCafeId: number) {
    if (loteCafeId === selectedLoteId) return;

    setExistenciasLoading(canViewExistencias);
    setMovimientosLoading(canViewMovimientos);
    setSelectedLoteId(loteCafeId);
  }

  function clearFilters() {
    setQuery('');
    setEstadoFiltro('all');
  }

  if (!canAccessInventario) {
    return (
      <Stack spacing={1}>
        <Typography variant="h4">{nav('inventarioCafe')}</Typography>
        <Typography variant="body2" color="text.secondary">
          {tp('noPermission')}
        </Typography>
      </Stack>
    );
  }

  return (
    <Stack spacing={2}>
      <Stack
        direction={{ xs: 'column', sm: 'row' }}
        alignItems={{ xs: 'flex-start', sm: 'center' }}
        justifyContent="space-between"
        spacing={2}
      >
        <Box>
          <Typography variant="h4">{nav('inventarioCafe')}</Typography>
          <Typography variant="body2" color="text.secondary">
            {ti('subtitle')}
          </Typography>
        </Box>

        <Stack direction="row" spacing={1} useFlexGap flexWrap="wrap">
          <Button
            variant="outlined"
            onClick={() => void onRefresh()}
            disabled={refreshLoading}
          >
            {tc('refresh')}
          </Button>
          {effectiveTab === 'lotes' ? (
            <Button
              variant="contained"
              onClick={openCreateDialog}
              disabled={!canCreateLotes}
            >
              {ti('newLote')}
            </Button>
          ) : null}
        </Stack>
      </Stack>

      {availableTabs.length > 1 ? (
        <Box sx={{ borderBottom: 1, borderColor: 'divider' }}>
          <Tabs
            value={effectiveTab}
            onChange={(_event, value: InventoryTab) => setActiveTab(value)}
            variant="scrollable"
            allowScrollButtonsMobile
          >
            {canViewLotes ? <Tab value="lotes" label={ti('tabLotes')} /> : null}
            {canViewPresentaciones ? (
              <Tab value="presentaciones" label={ti('tabPresentaciones')} />
            ) : null}
            {canViewTiposCafe ? (
              <Tab value="tiposCafe" label={ti('tabTiposCafe')} />
            ) : null}
          </Tabs>
        </Box>
      ) : null}

      {success ? (
        <Alert severity="success" onClose={() => setSuccess(null)}>
          {success}
        </Alert>
      ) : null}

      {effectiveTab === 'lotes' && createReport ? (
        <Alert
          severity={
            createReport.lineResults.some((line) => !line.success) ? 'warning' : 'success'
          }
          onClose={() => setCreateReport(null)}
        >
          <Typography variant="subtitle2">
            {ti('createLoteResultTitle', { code: createReport.codigoLote })}
          </Typography>
          {createReport.lineResults.length === 0 ? (
            <Typography variant="body2">{ti('createLoteNoLines')}</Typography>
          ) : (
            <Box component="ul" sx={{ pl: 2, my: 0 }}>
              {createReport.lineResults.map((line) => (
                <li key={line.id}>
                  <Typography variant="body2">
                    {line.success
                      ? ti('lineSuccess', {
                          presentation: line.presentacionNombre,
                          coffeeType: line.tipoCafeNombre,
                          count: formatNumber(locale, line.cantidadPaquetes),
                        })
                      : ti('lineFailure', {
                          presentation: line.presentacionNombre,
                          coffeeType: line.tipoCafeNombre,
                          count: formatNumber(locale, line.cantidadPaquetes),
                          error: line.error ?? ti('lineErrorFallback'),
                        })}
                  </Typography>
                </li>
              ))}
            </Box>
          )}
        </Alert>
      ) : null}

      {effectiveTab === 'lotes' && loadError ? (
        <Alert severity="error">{loadError}</Alert>
      ) : null}

      {effectiveTab === 'lotes' ? (
        <>
          <Card variant="outlined">
            <CardHeader title={ti('filtersTitle')} />
        <CardContent sx={{ pt: 0 }}>
          <Stack
            direction={{ xs: 'column', md: 'row' }}
            spacing={2}
            alignItems={{ xs: 'stretch', md: 'center' }}
            useFlexGap
            flexWrap="wrap"
          >
            <TextField
              size="small"
              label={ti('searchByCode')}
              placeholder={ti('searchPlaceholder')}
              value={query}
              onChange={(event) => setQuery(event.target.value)}
              sx={{ flex: 1, minWidth: { xs: '100%', md: 260 } }}
              slotProps={{
                input: {
                  startAdornment: (
                    <InputAdornment position="start">
                      <IconSearch size={18} stroke={1.5} />
                    </InputAdornment>
                  ),
                },
              }}
            />

            <FormControl size="small" sx={{ minWidth: { xs: '100%', md: 220 } }}>
              <InputLabel id="inventario-estado-label">{ti('filterByStatus')}</InputLabel>
              <Select
                labelId="inventario-estado-label"
                label={ti('filterByStatus')}
                value={String(estadoFiltro)}
                onChange={(event: SelectChangeEvent) => {
                  const value = event.target.value;
                  setEstadoFiltro(value === 'all' ? 'all' : (Number(value) as EstadoLoteCafe));
                }}
              >
                <MenuItem value="all">{ti('allStatuses')}</MenuItem>
                {ESTADOS_LOTE.map((estado) => (
                  <MenuItem key={estado} value={String(estado)}>
                    {ti(estadoLoteKey(estado))}
                  </MenuItem>
                ))}
              </Select>
            </FormControl>

            <Button
              variant="text"
              onClick={clearFilters}
              disabled={!query.trim() && estadoFiltro === 'all'}
            >
              {ti('clearFilters')}
            </Button>
          </Stack>
        </CardContent>
      </Card>

      <Card variant="outlined">
        <CardHeader
          title={ti('lotsTitle')}
          subheader={ti('lotsCount', { count: filteredLotes.length })}
        />
        <CardContent sx={{ pt: 0 }}>
          {loading ? (
            <Typography variant="body2" color="text.secondary">
              {tc('loading')}
            </Typography>
          ) : filteredLotes.length === 0 ? (
            <Alert severity="info">
              {items.length === 0 ? ti('noLots') : ti('noFilteredLots')}
            </Alert>
          ) : (
            <TableContainer>
              <Table size="small" sx={{ minWidth: 860 }}>
                <TableHead>
                  <TableRow>
                    <TableCell>{tf('lotCode')}</TableCell>
                    <TableCell>{tf('productionDate')}</TableCell>
                    <TableCell>{tf('packagingDate')}</TableCell>
                    <TableCell>{tf('expirationDate')}</TableCell>
                    <TableCell>{tf('status')}</TableCell>
                    <TableCell align="right">{tf('packages')}</TableCell>
                    <TableCell align="right">{tf('netWeight')}</TableCell>
                    <TableCell align="right">{tf('actions')}</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {filteredLotes.map((lote) => {
                    const selected = lote.loteCafeId === selectedLoteId;

                    return (
                      <TableRow key={lote.loteCafeId} hover selected={selected}>
                        <TableCell>
                          <Typography variant="body2" sx={{ fontWeight: 600 }}>
                            {lote.codigoLote}
                          </Typography>
                        </TableCell>
                        <TableCell>{formatDate(locale, lote.fechaProduccion)}</TableCell>
                        <TableCell>{formatDate(locale, lote.fechaEnvasado)}</TableCell>
                        <TableCell>{formatDate(locale, lote.fechaVencimiento)}</TableCell>
                        <TableCell>
                          <Chip
                            size="small"
                            color={estadoLoteColor(lote.estado)}
                            label={ti(estadoLoteKey(lote.estado))}
                          />
                        </TableCell>
                        <TableCell align="right">
                          {formatNumber(locale, lote.totalPaquetes)}
                        </TableCell>
                        <TableCell align="right">
                          {formatKg(locale, lote.totalPesoNetoKg)}
                        </TableCell>
                        <TableCell align="right">
                          <Button
                            size="small"
                            variant={selected ? 'contained' : 'outlined'}
                            onClick={() => selectLote(lote.loteCafeId)}
                          >
                            {ti('viewDetail')}
                          </Button>
                        </TableCell>
                      </TableRow>
                    );
                  })}
                </TableBody>
              </Table>
            </TableContainer>
          )}
        </CardContent>
      </Card>

      {selectedLote ? (
        <Stack spacing={2}>
          <Card variant="outlined">
            <CardHeader
              title={ti('selectedLotTitle', { code: selectedLote.codigoLote })}
              action={
                <Stack direction="row" spacing={1} alignItems="center" useFlexGap flexWrap="wrap">
                  <Chip
                    size="small"
                    color={estadoLoteColor(selectedLote.estado)}
                    label={ti(estadoLoteKey(selectedLote.estado))}
                  />
                  <Button
                    size="small"
                    variant="contained"
                    onClick={openMovementDialog}
                    disabled={!canCreateMovimientos || selectedLote.estado === 3}
                  >
                    {ti('registerMovement')}
                  </Button>
                </Stack>
              }
            />
            <CardContent sx={{ pt: 0 }}>
              <Stack
                direction="row"
                spacing={3}
                useFlexGap
                flexWrap="wrap"
                sx={{ rowGap: 2 }}
              >
                <SummaryItem label={tf('productionDate')}>
                  {formatDate(locale, selectedLote.fechaProduccion)}
                </SummaryItem>
                <SummaryItem label={tf('packagingDate')}>
                  {formatDate(locale, selectedLote.fechaEnvasado)}
                </SummaryItem>
                <SummaryItem label={tf('expirationDate')}>
                  {formatDate(locale, selectedLote.fechaVencimiento)}
                </SummaryItem>
                <SummaryItem label={tf('createdAt')}>
                  {formatDateTime(locale, selectedLote.creadoEn)}
                </SummaryItem>
                <SummaryItem label={tf('updatedAt')}>
                  {formatDateTime(locale, selectedLote.actualizadoEn)}
                </SummaryItem>
                <SummaryItem label={tf('packages')}>
                  {formatNumber(locale, selectedLote.totalPaquetes)}
                </SummaryItem>
                <SummaryItem label={tf('netWeight')}>
                  {formatKg(locale, selectedLote.totalPesoNetoKg)}
                </SummaryItem>
              </Stack>

              {selectedLote.estado === 3 ? (
                <Alert severity="error" sx={{ mt: 2 }}>
                  {ti('lotBlockedWarning')}
                </Alert>
              ) : null}

              {selectedLote.observacion ? (
                <Alert severity="info" sx={{ mt: 2 }}>
                  {selectedLote.observacion}
                </Alert>
              ) : null}
            </CardContent>
          </Card>

          <Card variant="outlined">
            <CardHeader
              title={ti('existenciasTitle')}
              subheader={ti('existenciasCount', { count: existencias.length })}
            />
            <CardContent sx={{ pt: 0 }}>
              {!canViewExistencias ? (
                <Alert severity="info">{ti('noPermissionExistencias')}</Alert>
              ) : existenciasLoading ? (
                <Typography variant="body2" color="text.secondary">
                  {tc('loading')}
                </Typography>
              ) : existenciasError ? (
                <Alert severity="error">{existenciasError}</Alert>
              ) : existencias.length === 0 ? (
                <Alert severity="info">{ti('noExistencias')}</Alert>
              ) : (
                <Stack spacing={2}>
                  {hasInactiveCatalogs ? (
                    <Alert severity="warning">{ti('inactiveCatalogWarning')}</Alert>
                  ) : null}

                  <TableContainer>
                    <Table size="small" sx={{ minWidth: 780 }}>
                      <TableHead>
                        <TableRow>
                          <TableCell>{tf('presentation')}</TableCell>
                          <TableCell>{tf('coffeeType')}</TableCell>
                          <TableCell align="right">{tf('packages')}</TableCell>
                          <TableCell align="right">{tf('unitWeight')}</TableCell>
                          <TableCell align="right">{tf('netWeight')}</TableCell>
                          <TableCell>{tf('updatedAt')}</TableCell>
                        </TableRow>
                      </TableHead>
                      <TableBody>
                        {existencias.map((existencia) => (
                          <TableRow key={existencia.loteCafeExistenciaId} hover>
                            <TableCell>
                              <Stack spacing={0.25}>
                                <Typography variant="body2">
                                  {existencia.presentacionNombre}
                                </Typography>
                                <Stack direction="row" spacing={1} alignItems="center">
                                  <Typography variant="caption" color="text.secondary">
                                    {existencia.presentacionCodigo}
                                  </Typography>
                                  {!isCatalogoActivo(existencia.presentacionEstado) ? (
                                    <Chip
                                      size="small"
                                      color={
                                        existencia.presentacionEstado === 3
                                          ? 'error'
                                          : 'warning'
                                      }
                                      variant="outlined"
                                      label={ti(
                                        existencia.presentacionEstado === 3
                                          ? 'catalogDeleted'
                                          : 'catalogInactive',
                                      )}
                                    />
                                  ) : null}
                                </Stack>
                              </Stack>
                            </TableCell>
                            <TableCell>
                              <Stack spacing={0.25}>
                                <Typography variant="body2">
                                  {existencia.tipoCafeNombre}
                                </Typography>
                                <Stack direction="row" spacing={1} alignItems="center">
                                  <Typography variant="caption" color="text.secondary">
                                    {existencia.tipoCafeCodigo}
                                  </Typography>
                                  {!isCatalogoActivo(existencia.tipoCafeEstado) ? (
                                    <Chip
                                      size="small"
                                      color={
                                        existencia.tipoCafeEstado === 3
                                          ? 'error'
                                          : 'warning'
                                      }
                                      variant="outlined"
                                      label={ti(
                                        existencia.tipoCafeEstado === 3
                                          ? 'catalogDeleted'
                                          : 'catalogInactive',
                                      )}
                                    />
                                  ) : null}
                                </Stack>
                              </Stack>
                            </TableCell>
                            <TableCell align="right">
                              {formatNumber(locale, existencia.cantidadPaquetes)}
                            </TableCell>
                            <TableCell align="right">
                              {formatGrams(locale, existencia.pesoUnitarioGramos)}
                            </TableCell>
                            <TableCell align="right">
                              {formatKg(locale, existencia.pesoNetoKg)}
                            </TableCell>
                            <TableCell>
                              {formatDateTime(locale, existencia.actualizadoEn)}
                            </TableCell>
                          </TableRow>
                        ))}
                      </TableBody>
                    </Table>
                  </TableContainer>
                </Stack>
              )}
            </CardContent>
          </Card>

          <Card variant="outlined">
            <CardHeader
              title={ti('movimientosTitle')}
              subheader={ti('movementsCount', { count: movimientos.length })}
            />
            <CardContent sx={{ pt: 0 }}>
              {!canViewMovimientos ? (
                <Alert severity="info">{ti('noPermissionMovimientos')}</Alert>
              ) : movimientosLoading ? (
                <Typography variant="body2" color="text.secondary">
                  {tc('loading')}
                </Typography>
              ) : movimientosError ? (
                <Alert severity="error">{movimientosError}</Alert>
              ) : movimientos.length === 0 ? (
                <Alert severity="info">{ti('noMovimientos')}</Alert>
              ) : (
                <TableContainer>
                  <Table size="small" sx={{ minWidth: 1080 }}>
                    <TableHead>
                      <TableRow>
                        <TableCell>{tf('date')}</TableCell>
                        <TableCell>{tf('movementType')}</TableCell>
                        <TableCell>{tf('presentation')}</TableCell>
                        <TableCell>{tf('coffeeType')}</TableCell>
                        <TableCell align="right">{tf('quantity')}</TableCell>
                        <TableCell align="right">{tf('netWeight')}</TableCell>
                        <TableCell>{tf('user')}</TableCell>
                        <TableCell>{tf('observation')}</TableCell>
                      </TableRow>
                    </TableHead>
                    <TableBody>
                      {movimientos.map((movimiento) => (
                        <TableRow key={movimiento.movimientoLoteCafeId} hover>
                          <TableCell>
                            {formatDateTime(locale, movimiento.fechaMovimiento)}
                          </TableCell>
                          <TableCell>
                            <Chip
                              size="small"
                              color={tipoMovimientoColor(movimiento.tipoMovimiento)}
                              label={ti(tipoMovimientoKey(movimiento.tipoMovimiento))}
                            />
                          </TableCell>
                          <TableCell>
                            <Stack spacing={0.25}>
                              <Typography variant="body2">
                                {movimiento.presentacionNombre}
                              </Typography>
                              <Typography variant="caption" color="text.secondary">
                                {movimiento.presentacionCodigo}
                              </Typography>
                            </Stack>
                          </TableCell>
                          <TableCell>
                            <Stack spacing={0.25}>
                              <Typography variant="body2">
                                {movimiento.tipoCafeNombre}
                              </Typography>
                              <Typography variant="caption" color="text.secondary">
                                {movimiento.tipoCafeCodigo}
                              </Typography>
                            </Stack>
                          </TableCell>
                          <TableCell align="right">
                            {formatNumber(locale, movimiento.cantidadPaquetes)}
                          </TableCell>
                          <TableCell align="right">
                            {formatKg(locale, movimiento.pesoNetoKg)}
                          </TableCell>
                          <TableCell>
                            {movimiento.usuarioId
                              ? ti('userNumber', { id: movimiento.usuarioId })
                              : '—'}
                          </TableCell>
                          <TableCell>
                            <Stack spacing={0.25}>
                              <Typography variant="body2">
                                {movimiento.observacion || '—'}
                              </Typography>
                              {movimiento.referencia ? (
                                <Typography variant="caption" color="text.secondary">
                                  {tf('reference')}: {movimiento.referencia}
                                </Typography>
                              ) : null}
                            </Stack>
                          </TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                </TableContainer>
              )}
            </CardContent>
          </Card>
        </Stack>
      ) : null}
        </>
      ) : null}

      {effectiveTab === 'presentaciones' ? (
        <CatalogoCafePanel
          kind="presentacion"
          items={presentacionRows}
          loading={presentacionesLoading}
          error={presentacionesError}
          canCreate={canCreatePresentaciones}
          canEdit={canEditPresentaciones}
          canDelete={canDeletePresentaciones}
          onCreate={openPresentacionCreate}
          onEdit={openPresentacionEdit}
          onDelete={(item) => openCatalogDelete('presentacion', item)}
        />
      ) : null}

      {effectiveTab === 'tiposCafe' ? (
        <CatalogoCafePanel
          kind="tipoCafe"
          items={tipoCafeRows}
          loading={tiposCafeLoading}
          error={tiposCafeError}
          canCreate={canCreateTiposCafe}
          canEdit={canEditTiposCafe}
          canDelete={canDeleteTiposCafe}
          onCreate={openTipoCafeCreate}
          onEdit={openTipoCafeEdit}
          onDelete={(item) => openCatalogDelete('tipoCafe', item)}
        />
      ) : null}

      <CreateLoteDialog
        open={createOpen}
        onClose={() => setCreateOpen(false)}
        onCreated={onLoteCreated}
        canCreateMovimiento={canCreateMovimientos}
        canLoadCatalogos={canLoadCatalogos}
        presentaciones={presentaciones}
        tiposCafe={tiposCafe}
        catalogsLoading={catalogsLoading}
        catalogsError={catalogsError}
      />

      {selectedLote ? (
        <MovimientoDialog
          open={movementOpen}
          onClose={() => setMovementOpen(false)}
          onSaved={onMovementSaved}
          lote={selectedLote}
          existencias={existencias}
          presentaciones={presentaciones}
          tiposCafe={tiposCafe}
          canLoadCatalogos={canLoadCatalogos}
          canViewExistencias={canViewExistencias}
          existenciasLoading={existenciasLoading}
          existenciasError={existenciasError}
          catalogsLoading={catalogsLoading}
          catalogsError={catalogsError}
        />
      ) : null}

      <CatalogoCafeDialog
        kind="presentacion"
        open={presentacionDialogOpen}
        item={editingPresentacion}
        onClose={closePresentacionDialog}
        onSaved={onPresentacionSaved}
      />

      <CatalogoCafeDialog
        kind="tipoCafe"
        open={tipoCafeDialogOpen}
        item={editingTipoCafe}
        onClose={closeTipoCafeDialog}
        onSaved={onTipoCafeSaved}
      />

      <CatalogoCafeDeleteDialog
        kind={deleteCatalogKind}
        open={deleteCatalogOpen}
        item={deleteCatalogItem}
        onClose={closeCatalogDelete}
        onDeleted={onCatalogDeleted}
      />
    </Stack>
  );
}

function SummaryItem({ label, children }: { label: string; children: ReactNode }) {
  return (
    <Box sx={{ minWidth: 140 }}>
      <Typography variant="caption" color="text.secondary">
        {label}
      </Typography>
      <Box sx={{ mt: 0.5 }}>
        <Typography variant="body2">{children}</Typography>
      </Box>
    </Box>
  );
}
