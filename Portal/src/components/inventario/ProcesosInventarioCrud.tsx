'use client';

import { useEffect, useState } from 'react';
import { useSearchParams } from 'next/navigation';

import Alert from '@mui/material/Alert';
import Button from '@mui/material/Button';
import Card from '@mui/material/Card';
import CardContent from '@mui/material/CardContent';
import CardHeader from '@mui/material/CardHeader';
import Checkbox from '@mui/material/Checkbox';
import Chip from '@mui/material/Chip';
import Dialog from '@mui/material/Dialog';
import DialogActions from '@mui/material/DialogActions';
import DialogContent from '@mui/material/DialogContent';
import DialogTitle from '@mui/material/DialogTitle';
import FormControlLabel from '@mui/material/FormControlLabel';
import MenuItem from '@mui/material/MenuItem';
import Stack from '@mui/material/Stack';
import Table from '@mui/material/Table';
import TableBody from '@mui/material/TableBody';
import TableCell from '@mui/material/TableCell';
import TableContainer from '@mui/material/TableContainer';
import TableHead from '@mui/material/TableHead';
import TableRow from '@mui/material/TableRow';
import TextField from '@mui/material/TextField';
import Typography from '@mui/material/Typography';

import { useTranslations } from '@/hooks/useTranslations';
import { apiFetch } from '@/lib/api/client';
import { useAuth } from '@/lib/auth/AuthProvider';

type Producto = {
  productoId: number;
  nombre: string;
  unidadMedida: string;
  estado: boolean;
};

type RecepcionCafe = {
  recepcionCafeId: number;
  proveedorId: number;
  proveedorNombre: string;
  productoId: number;
  productoNombre: string;
  unidadMedida: string;
  codigoLote: string;
  cantidad: number;
  cantidadDisponible: number;
  estado: string;
  fechaRecepcion?: string | null;
};

type ProcesoInventario = {
  procesoInventarioId: number;
  recepcionCafeId: number;
  loteOrigenId?: number | null;
  codigoLoteOrigen: string;
  productoOrigenId: number;
  productoOrigenNombre: string;
  unidadMedidaOrigen: string;
  productoSalidaId: number;
  productoSalidaNombre: string;
  unidadMedidaSalida: string;
  loteSalidaId: number;
  codigoLoteSalida: string;
  usaLoteCompleto: boolean;
  fechaProceso?: string | null;
  cantidadOrigenConsumida: number;
  cantidadSalida: number;
  costoProceso: number;
  observaciones?: string | null;
  estado: string;
};

type ProcesoInventarioCreateRequest = {
  recepcionCafeId: number;
  productoSalidaId: number;
  usaLoteCompleto: boolean;
  fechaProceso?: string;
  cantidadOrigenConsumida?: number;
  cantidadSalida: number;
  costoProceso: number;
  codigoLoteSalida?: string;
  observaciones?: string;
};

function toInputDate(value?: string | null) {
  if (!value) return '';
  return value.slice(0, 10);
}

function todayInputDate() {
  return new Date().toISOString().slice(0, 10);
}

function parseNumber(value: string) {
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : 0;
}

function resolveStatusColor(value: string) {
  return value === 'Registrado' ? 'success' : 'default';
}

export function ProcesosInventarioCrud() {
  const nav = useTranslations('navigation');
  const tc = useTranslations('common');
  const tf = useTranslations('fields');
  const tp = useTranslations('pages');
  const tcrud = useTranslations('crud');
  const tproduction = useTranslations('production');
  const searchParams = useSearchParams();

  const { has } = useAuth();
  const canView = has('Inventario.Procesos', 'Ver');
  const canCreate = has('Inventario.Procesos', 'Crear');
  const canAnnul = has('Inventario.Procesos', 'Eliminar');

  const [items, setItems] = useState<ProcesoInventario[]>([]);
  const [recepciones, setRecepciones] = useState<RecepcionCafe[]>([]);
  const [productos, setProductos] = useState<Producto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [createOpen, setCreateOpen] = useState(false);
  const [createError, setCreateError] = useState<string | null>(null);
  const [createSaving, setCreateSaving] = useState(false);

  const [recepcionCafeId, setRecepcionCafeId] = useState('');
  const [productoSalidaId, setProductoSalidaId] = useState('');
  const [usaLoteCompleto, setUsaLoteCompleto] = useState(false);
  const [fechaProceso, setFechaProceso] = useState(todayInputDate());
  const [cantidadOrigenConsumida, setCantidadOrigenConsumida] = useState('0');
  const [cantidadSalida, setCantidadSalida] = useState('0');
  const [costoProceso, setCostoProceso] = useState('0');
  const [codigoLoteSalida, setCodigoLoteSalida] = useState('');
  const [observaciones, setObservaciones] = useState('');

  const [annulOpen, setAnnulOpen] = useState(false);
  const [annulError, setAnnulError] = useState<string | null>(null);
  const [annulSaving, setAnnulSaving] = useState(false);
  const [annulTarget, setAnnulTarget] = useState<ProcesoInventario | null>(null);
  const [queryHandled, setQueryHandled] = useState(false);

  const activeProductos = productos.filter((item) => item.estado);
  const availableRecepciones = recepciones.filter((item) => item.estado === 'Registrada' && item.cantidadDisponible > 0);
  const selectedRecepcion = availableRecepciones.find((item) => String(item.recepcionCafeId) === recepcionCafeId) ?? null;
  const selectedProductoSalida = activeProductos.find((item) => String(item.productoId) === productoSalidaId) ?? null;
  const cantidadOrigenSeleccionada = usaLoteCompleto
    ? selectedRecepcion?.cantidadDisponible ?? 0
    : parseNumber(cantidadOrigenConsumida);

  async function reload() {
    setLoading(true);
    setError(null);
    try {
      const [procesosRes, recepcionesRes, productosRes] = await Promise.allSettled([
        apiFetch<ProcesoInventario[]>('/api/v1/procesos-inventario'),
        apiFetch<RecepcionCafe[]>('/api/v1/compras/recepciones-cafe'),
        apiFetch<Producto[]>('/api/v1/productos'),
      ]);

      if (procesosRes.status === 'fulfilled') {
        setItems(procesosRes.value);
      } else {
        setItems([]);
        setError(procesosRes.reason instanceof Error ? procesosRes.reason.message : tcrud('errorLoadingData'));
      }

      const recepcionesData = recepcionesRes.status === 'fulfilled' ? recepcionesRes.value : [];
      const productosData = productosRes.status === 'fulfilled' ? productosRes.value : [];
      setRecepciones(recepcionesData);
      setProductos(productosData);

      if (!recepcionCafeId && recepcionesData.length > 0) {
        setRecepcionCafeId(String(recepcionesData[0]?.recepcionCafeId ?? ''));
      }

      if (!productoSalidaId && productosData.some((item) => item.estado)) {
        setProductoSalidaId(String(productosData.find((item) => item.estado)?.productoId ?? ''));
      }
    } catch (e) {
      setError(e instanceof Error ? e.message : tcrud('errorLoadingData'));
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void reload();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  function resetForm(prefilledProductId?: string) {
    setRecepcionCafeId(String(availableRecepciones[0]?.recepcionCafeId ?? ''));
    setProductoSalidaId(
      prefilledProductId && activeProductos.some((item) => String(item.productoId) === prefilledProductId)
        ? prefilledProductId
        : String(activeProductos[0]?.productoId ?? ''),
    );
    setUsaLoteCompleto(false);
    setFechaProceso(todayInputDate());
    setCantidadOrigenConsumida('0');
    setCantidadSalida('0');
    setCostoProceso('0');
    setCodigoLoteSalida('');
    setObservaciones('');
  }

  function openCreate(prefilledProductId?: string) {
    if (!canCreate) return;
    resetForm(prefilledProductId);
    setCreateError(null);
    setCreateOpen(true);
  }

  function closeCreate() {
    if (createSaving) return;
    setCreateOpen(false);
    setCreateError(null);
  }

  async function saveCreate() {
    setCreateSaving(true);
    setCreateError(null);

    const payload: ProcesoInventarioCreateRequest = {
      recepcionCafeId: Number(recepcionCafeId),
      productoSalidaId: Number(productoSalidaId),
      usaLoteCompleto,
      cantidadSalida: parseNumber(cantidadSalida),
      costoProceso: parseNumber(costoProceso),
      ...(fechaProceso ? { fechaProceso: new Date(fechaProceso).toISOString() } : {}),
      ...(!usaLoteCompleto ? { cantidadOrigenConsumida: parseNumber(cantidadOrigenConsumida) } : {}),
      ...(codigoLoteSalida.trim() ? { codigoLoteSalida: codigoLoteSalida.trim() } : {}),
      ...(observaciones.trim() ? { observaciones: observaciones.trim() } : {}),
    };

    try {
      await apiFetch('/api/v1/procesos-inventario', {
        method: 'POST',
        body: JSON.stringify(payload),
      });

      setCreateOpen(false);
      await reload();
    } catch (e) {
      setCreateError(e instanceof Error ? e.message : tcrud('errorSaving'));
    } finally {
      setCreateSaving(false);
    }
  }

  function openAnnul(item: ProcesoInventario) {
    if (!canAnnul || item.estado !== 'Registrado') return;
    setAnnulTarget(item);
    setAnnulError(null);
    setAnnulOpen(true);
  }

  function closeAnnul() {
    if (annulSaving) return;
    setAnnulOpen(false);
    setAnnulTarget(null);
    setAnnulError(null);
  }

  async function confirmAnnul() {
    if (!canAnnul || !annulTarget) return;
    setAnnulSaving(true);
    setAnnulError(null);
    try {
      await apiFetch<void>(`/api/v1/procesos-inventario/${annulTarget.procesoInventarioId}/anular`, { method: 'POST' });
      setAnnulOpen(false);
      setAnnulTarget(null);
      await reload();
    } catch (e) {
      setAnnulError(e instanceof Error ? e.message : tproduction('errorAnnullingInventoryProcess'));
    } finally {
      setAnnulSaving(false);
    }
  }

  const canCreateRecord = canCreate && availableRecepciones.length > 0 && activeProductos.length > 0;
  const canSave =
    Boolean(recepcionCafeId) &&
    Boolean(productoSalidaId) &&
    parseNumber(cantidadSalida) > 0 &&
    parseNumber(costoProceso) >= 0 &&
    (usaLoteCompleto || parseNumber(cantidadOrigenConsumida) > 0);

  useEffect(() => {
    if (loading || queryHandled) {
      return;
    }

    const requestedProductId = searchParams.get('productoSalidaId');
    const shouldOpen = searchParams.get('open') === '1';
    const validRequestedProductId = requestedProductId && activeProductos.some((item) => String(item.productoId) === requestedProductId)
      ? requestedProductId
      : undefined;

    if (shouldOpen && canCreateRecord) {
      openCreate(validRequestedProductId);
      setQueryHandled(true);
      return;
    }

    if (validRequestedProductId) {
      setProductoSalidaId(validRequestedProductId);
    }

    setQueryHandled(true);
  }, [activeProductos, canCreateRecord, loading, queryHandled, searchParams]);

  if (!canView) {
    return (
      <Stack spacing={1}>
        <Typography variant="h4">{nav('procesosInventario')}</Typography>
        <Typography variant="body2" color="text.secondary">
          {tp('noPermission')}
        </Typography>
      </Stack>
    );
  }

  return (
    <Stack spacing={2}>
      <Stack direction="row" alignItems="center" justifyContent="space-between" spacing={2}>
        <Typography variant="h4">{nav('procesosInventario')}</Typography>
        <Stack direction="row" spacing={1}>
          <Button variant="outlined" onClick={() => void reload()} disabled={loading}>
            {tc('refresh')}
          </Button>
          <Button variant="contained" onClick={() => openCreate()} disabled={!canCreateRecord}>
            {tcrud('new')}
          </Button>
        </Stack>
      </Stack>

      {error ? <Alert severity="error">{error}</Alert> : null}
      {!loading && availableRecepciones.length === 0 ? <Alert severity="warning">{tproduction('missingAvailableLotsForProcesses')}</Alert> : null}
      {!loading && activeProductos.length === 0 ? <Alert severity="warning">{tproduction('missingActiveProductsForProcesses')}</Alert> : null}

      <Card variant="outlined">
        <CardHeader title={tc('listTitle')} />
        <CardContent sx={{ pt: 0 }}>
          {loading ? (
            <Typography variant="body2" color="text.secondary">
              {tc('loading')}
            </Typography>
          ) : (
            <TableContainer>
              <Table size="small">
                <TableHead>
                  <TableRow>
                    <TableCell>{tf('id')}</TableCell>
                    <TableCell>{tf('date')}</TableCell>
                    <TableCell>{tf('sourceBatch')}</TableCell>
                    <TableCell>{tf('sourceQuantity')}</TableCell>
                    <TableCell>{tf('batchCode')}</TableCell>
                    <TableCell>{tf('outputProduct')}</TableCell>
                    <TableCell>{tf('outputQuantity')}</TableCell>
                    <TableCell>{tf('processCostOptional')}</TableCell>
                    <TableCell>{tf('status')}</TableCell>
                    <TableCell align="right">{tf('actions')}</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {items.map((item) => (
                    <TableRow key={item.procesoInventarioId} hover>
                      <TableCell>{item.procesoInventarioId}</TableCell>
                      <TableCell>{toInputDate(item.fechaProceso) || '—'}</TableCell>
                      <TableCell>
                        <Stack spacing={0.25}>
                          <Typography variant="body2">{item.codigoLoteOrigen}</Typography>
                          <Typography variant="caption" color="text.secondary">{item.productoOrigenNombre}</Typography>
                        </Stack>
                      </TableCell>
                      <TableCell>{item.cantidadOrigenConsumida} {item.unidadMedidaOrigen}</TableCell>
                      <TableCell>{item.codigoLoteSalida}</TableCell>
                      <TableCell>{item.productoSalidaNombre}</TableCell>
                      <TableCell>{item.cantidadSalida} {item.unidadMedidaSalida}</TableCell>
                      <TableCell>{item.costoProceso}</TableCell>
                      <TableCell>
                        <Chip size="small" color={resolveStatusColor(item.estado)} label={item.estado} />
                      </TableCell>
                      <TableCell align="right">
                        <Button
                          size="small"
                          color="warning"
                          variant="outlined"
                          onClick={() => openAnnul(item)}
                          disabled={!canAnnul || item.estado !== 'Registrado'}
                        >
                          {tc('annul')}
                        </Button>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </TableContainer>
          )}
        </CardContent>
      </Card>

      <Dialog open={createOpen} onClose={closeCreate} fullWidth maxWidth="md">
        <DialogTitle>{`${tcrud('new')} — ${nav('procesosInventario')}`}</DialogTitle>
        <DialogContent dividers>
          <Stack spacing={2} sx={{ pt: 1 }}>
            {createError ? <Alert severity="error">{createError}</Alert> : null}

            <Card variant="outlined">
              <CardHeader title={`1. ${tf('sourceBatch')}`} />
              <CardContent sx={{ pt: 0 }}>
                <Stack spacing={2}>
                  <TextField select fullWidth label={tf('sourceBatch')} value={recepcionCafeId} onChange={(e) => setRecepcionCafeId(e.target.value)}>
                    {availableRecepciones.map((item) => (
                      <MenuItem key={item.recepcionCafeId} value={String(item.recepcionCafeId)}>
                        {item.codigoLote} · {item.productoNombre} · {item.cantidadDisponible} KG
                      </MenuItem>
                    ))}
                  </TextField>

                  <Stack direction={{ xs: 'column', md: 'row' }} spacing={2}>
                    <TextField
                      fullWidth
                      label={tf('date')}
                      value={fechaProceso}
                      onChange={(e) => setFechaProceso(e.target.value)}
                      type="date"
                      InputLabelProps={{ shrink: true }}
                    />
                    <TextField fullWidth label={tf('availableQuantity')} value={selectedRecepcion ? `${selectedRecepcion.cantidadDisponible} KG` : '—'} InputProps={{ readOnly: true }} />
                  </Stack>

                  {selectedRecepcion ? (
                    <Alert severity="info">
                      {selectedRecepcion.productoNombre} · {selectedRecepcion.codigoLote} · {selectedRecepcion.cantidadDisponible} KG · {selectedRecepcion.proveedorNombre}
                    </Alert>
                  ) : null}

                  <FormControlLabel
                    control={<Checkbox checked={usaLoteCompleto} onChange={(e) => setUsaLoteCompleto(e.target.checked)} />}
                    label={tf('useWholeBatch')}
                  />

                  <TextField
                    fullWidth
                    label={`${tf('sourceQuantity')} (KG)`}
                    value={usaLoteCompleto ? String(selectedRecepcion?.cantidadDisponible ?? 0) : cantidadOrigenConsumida}
                    onChange={(e) => setCantidadOrigenConsumida(e.target.value)}
                    type="number"
                    inputProps={{ min: 0.001, step: 0.001 }}
                    disabled={usaLoteCompleto}
                  />
                </Stack>
              </CardContent>
            </Card>

            <Card variant="outlined">
              <CardHeader title={`2. ${tf('outputProduct')}`} />
              <CardContent sx={{ pt: 0 }}>
                <Stack spacing={2}>
                  <TextField select fullWidth label={tf('outputProduct')} value={productoSalidaId} onChange={(e) => setProductoSalidaId(e.target.value)}>
                    {activeProductos.map((item) => (
                      <MenuItem key={item.productoId} value={String(item.productoId)}>
                        {item.nombre} ({item.unidadMedida})
                      </MenuItem>
                    ))}
                  </TextField>

                  <Stack direction={{ xs: 'column', md: 'row' }} spacing={2}>
                    <TextField
                      fullWidth
                      label={tf('outputQuantity')}
                      value={cantidadSalida}
                      onChange={(e) => setCantidadSalida(e.target.value)}
                      type="number"
                      inputProps={{ min: 0.001, step: 0.001 }}
                    />
                    <TextField fullWidth label={tf('batchCodeOptional')} value={codigoLoteSalida} onChange={(e) => setCodigoLoteSalida(e.target.value)} />
                  </Stack>

                  {selectedProductoSalida ? (
                    <Alert severity="success">
                      {selectedProductoSalida.nombre} · {selectedProductoSalida.unidadMedida}
                    </Alert>
                  ) : null}
                </Stack>
              </CardContent>
            </Card>

            <Card variant="outlined">
              <CardHeader title={`3. ${tc('confirm')}`} />
              <CardContent sx={{ pt: 0 }}>
                <Stack spacing={2}>
                  <Stack direction={{ xs: 'column', md: 'row' }} spacing={2}>
                    <TextField
                      fullWidth
                      label={tf('processCostOptional')}
                      value={costoProceso}
                      onChange={(e) => setCostoProceso(e.target.value)}
                      type="number"
                      inputProps={{ min: 0, step: 0.01 }}
                    />
                    <TextField fullWidth label={tf('notes')} value={observaciones} onChange={(e) => setObservaciones(e.target.value)} multiline minRows={3} />
                  </Stack>

                  <Alert severity="warning">
                    {selectedRecepcion?.codigoLote ?? '—'} → {selectedProductoSalida?.nombre ?? '—'} · {cantidadOrigenSeleccionada} KG → {parseNumber(cantidadSalida)} {selectedProductoSalida?.unidadMedida ?? ''}
                  </Alert>
                </Stack>
              </CardContent>
            </Card>
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={closeCreate} disabled={createSaving}>
            {tc('cancel')}
          </Button>
          <Button onClick={() => void saveCreate()} variant="contained" disabled={createSaving || !canSave || !canCreateRecord}>
            {tc('save')}
          </Button>
        </DialogActions>
      </Dialog>

      <Dialog open={annulOpen} onClose={closeAnnul} fullWidth maxWidth="xs">
        <DialogTitle>{tc('annul')}</DialogTitle>
        <DialogContent dividers>
          <Stack spacing={2} sx={{ pt: 1 }}>
            {annulError ? <Alert severity="error">{annulError}</Alert> : null}
            <Typography variant="body2">{tproduction('confirmAnnulInventoryProcess')}</Typography>
            {annulTarget ? (
              <Typography variant="body2" color="text.secondary">
                #{annulTarget.procesoInventarioId} · {annulTarget.codigoLoteOrigen} → {annulTarget.codigoLoteSalida}
              </Typography>
            ) : null}
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={closeAnnul} disabled={annulSaving}>
            {tc('cancel')}
          </Button>
          <Button onClick={() => void confirmAnnul()} color="warning" variant="contained" disabled={annulSaving || !annulTarget || !canAnnul}>
            {tc('annul')}
          </Button>
        </DialogActions>
      </Dialog>
    </Stack>
  );
}