'use client';

import { useEffect, useState } from 'react';

import Alert from '@mui/material/Alert';
import Button from '@mui/material/Button';
import Card from '@mui/material/Card';
import CardContent from '@mui/material/CardContent';
import CardHeader from '@mui/material/CardHeader';
import Chip from '@mui/material/Chip';
import Dialog from '@mui/material/Dialog';
import DialogActions from '@mui/material/DialogActions';
import DialogContent from '@mui/material/DialogContent';
import DialogTitle from '@mui/material/DialogTitle';
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

import { apiFetch } from '@/lib/api/client';
import { useAuth } from '@/lib/auth/AuthProvider';
import { useTranslations } from '@/hooks/useTranslations';

type Producto = {
  productoId: number;
  nombre: string;
  unidadMedida: string;
  estado: boolean;
};

type Lote = {
  loteId: number;
  productoId: number;
  codigoLote: string;
  cantidadInicial: number;
  cantidadDisponible: number;
  estado: string;
  fechaCreacion: string;
};

type TransformacionCafe = {
  transformacionCafeId: number;
  loteOrigenId: number;
  codigoLoteOrigen: string;
  productoOrigenId: number;
  productoOrigenNombre: string;
  unidadMedidaOrigen: string;
  productoSalidaId: number;
  productoSalidaNombre: string;
  unidadMedidaSalida: string;
  loteSalidaId: number;
  codigoLoteSalida: string;
  tipoTransformacion: string;
  fechaTransformacion?: string | null;
  cantidadEntrada: number;
  cantidadSalida: number;
  merma?: number | null;
  rendimiento?: number | null;
  observaciones?: string | null;
  estado: string;
};

type TransformacionCafeCreateRequest = {
  loteOrigenId: number;
  productoSalidaId: number;
  tipoTransformacion: string;
  fechaTransformacion?: string;
  cantidadEntrada: number;
  cantidadSalida: number;
  costoProceso: number;
  codigoLoteSalida?: string;
  observaciones?: string;
};

type TransformacionCafeCreateResult = {
  transformacionCafeId: number;
  loteSalidaId: number;
  codigoLoteSalida: string;
};

function toInputDate(value?: string | null) {
  if (!value) return '';
  return value.slice(0, 10);
}

function todayInputDate() {
  return new Date().toISOString().slice(0, 10);
}

function resolveStatusLabel(value: string, tproduction: (key: string) => string) {
  if (value === 'Anulada') {
    return tproduction('statusAnnulled');
  }

  return tproduction('statusRegistered');
}

export function TransformacionesCafeCrud() {
  const nav = useTranslations('navigation');
  const tc = useTranslations('common');
  const tf = useTranslations('fields');
  const tp = useTranslations('pages');
  const tcrud = useTranslations('crud');
  const tproduction = useTranslations('production');

  const { has } = useAuth();
  const canView = has('Produccion.TransformacionesCafe', 'Ver');
  const canCreate = has('Produccion.TransformacionesCafe', 'Crear');
  const canAnnul = has('Produccion.TransformacionesCafe', 'Eliminar');

  const [items, setItems] = useState<TransformacionCafe[]>([]);
  const [lotes, setLotes] = useState<Lote[]>([]);
  const [productos, setProductos] = useState<Producto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [editOpen, setEditOpen] = useState(false);
  const [editError, setEditError] = useState<string | null>(null);
  const [editSaving, setEditSaving] = useState(false);

  const [loteOrigenId, setLoteOrigenId] = useState('');
  const [productoSalidaId, setProductoSalidaId] = useState('');
  const [tipoTransformacion, setTipoTransformacion] = useState('');
  const [fechaTransformacion, setFechaTransformacion] = useState(todayInputDate());
  const [cantidadEntrada, setCantidadEntrada] = useState('0');
  const [cantidadSalida, setCantidadSalida] = useState('0');
  const [costoProceso, setCostoProceso] = useState('0');
  const [codigoLoteSalida, setCodigoLoteSalida] = useState('');
  const [observaciones, setObservaciones] = useState('');

  const [annulOpen, setAnnulOpen] = useState(false);
  const [annulError, setAnnulError] = useState<string | null>(null);
  const [annulSaving, setAnnulSaving] = useState(false);
  const [annulTarget, setAnnulTarget] = useState<TransformacionCafe | null>(null);

  const activeProductos = productos.filter((item) => item.estado);
  const availableLotes = lotes.filter((item) => item.estado === 'Activo' && item.cantidadDisponible > 0);

  async function reload() {
    setLoading(true);
    setError(null);
    try {
      const [transformacionesRes, lotesRes, productosRes] = await Promise.allSettled([
        apiFetch<TransformacionCafe[]>('/api/v1/produccion/transformaciones-cafe'),
        apiFetch<Lote[]>('/api/v1/lotes?soloActivos=true&soloConStock=true'),
        apiFetch<Producto[]>('/api/v1/productos'),
      ]);

      if (transformacionesRes.status === 'fulfilled') {
        setItems(transformacionesRes.value);
      } else {
        setItems([]);
        setError(transformacionesRes.reason instanceof Error ? transformacionesRes.reason.message : tcrud('errorLoadingData'));
      }

      const lotesData = lotesRes.status === 'fulfilled' ? lotesRes.value : [];
      const productosData = productosRes.status === 'fulfilled' ? productosRes.value : [];
      setLotes(lotesData);
      setProductos(productosData);

      if (!loteOrigenId && lotesData.length > 0) {
        setLoteOrigenId(String(lotesData[0]?.loteId ?? ''));
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

  function resetForm() {
    setLoteOrigenId(String(availableLotes[0]?.loteId ?? ''));
    setProductoSalidaId(String(activeProductos[0]?.productoId ?? ''));
    setTipoTransformacion('');
    setFechaTransformacion(todayInputDate());
    setCantidadEntrada('0');
    setCantidadSalida('0');
    setCostoProceso('0');
    setCodigoLoteSalida('');
    setObservaciones('');
  }

  function openCreate() {
    if (!canCreate) return;
    resetForm();
    setEditError(null);
    setEditOpen(true);
  }

  function closeEdit() {
    if (editSaving) return;
    setEditOpen(false);
    setEditError(null);
  }

  async function saveEdit() {
    setEditSaving(true);
    setEditError(null);

    const payload: TransformacionCafeCreateRequest = {
      loteOrigenId: Number(loteOrigenId),
      productoSalidaId: Number(productoSalidaId),
      tipoTransformacion: tipoTransformacion.trim(),
      cantidadEntrada: Number(cantidadEntrada),
      cantidadSalida: Number(cantidadSalida),
      costoProceso: Number(costoProceso),
      ...(fechaTransformacion ? { fechaTransformacion: new Date(fechaTransformacion).toISOString() } : {}),
      ...(codigoLoteSalida.trim() ? { codigoLoteSalida: codigoLoteSalida.trim() } : {}),
      ...(observaciones.trim() ? { observaciones: observaciones.trim() } : {}),
    };

    try {
      await apiFetch<TransformacionCafeCreateResult>('/api/v1/produccion/transformaciones-cafe', {
        method: 'POST',
        body: JSON.stringify(payload),
      });

      setEditOpen(false);
      await reload();
    } catch (e) {
      setEditError(e instanceof Error ? e.message : tcrud('errorSaving'));
    } finally {
      setEditSaving(false);
    }
  }

  function openAnnul(item: TransformacionCafe) {
    if (!canAnnul || item.estado !== 'Registrada') return;
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
      await apiFetch<void>(`/api/v1/produccion/transformaciones-cafe/${annulTarget.transformacionCafeId}/anular`, { method: 'POST' });
      setAnnulOpen(false);
      setAnnulTarget(null);
      await reload();
    } catch (e) {
      setAnnulError(e instanceof Error ? e.message : tproduction('errorAnnullingTransformation'));
    } finally {
      setAnnulSaving(false);
    }
  }

  const canCreateRecord = canCreate && availableLotes.length > 0 && activeProductos.length > 0;
  const canSave = Boolean(loteOrigenId) && Boolean(productoSalidaId) && Boolean(tipoTransformacion.trim()) && Number(cantidadEntrada) > 0 && Number(cantidadSalida) > 0;

  if (!canView) {
    return (
      <Stack spacing={1}>
        <Typography variant="h4">{nav('transformacionesCafe')}</Typography>
        <Typography variant="body2" color="text.secondary">
          {tp('noPermission')}
        </Typography>
      </Stack>
    );
  }

  return (
    <Stack spacing={2}>
      <Stack direction="row" alignItems="center" justifyContent="space-between" spacing={2}>
        <Typography variant="h4">{nav('transformacionesCafe')}</Typography>
        <Stack direction="row" spacing={1}>
          <Button variant="outlined" onClick={() => void reload()} disabled={loading}>
            {tc('refresh')}
          </Button>
          <Button variant="contained" onClick={openCreate} disabled={!canCreateRecord}>
            {tcrud('new')}
          </Button>
        </Stack>
      </Stack>

      {error ? <Alert severity="error">{error}</Alert> : null}
      {!loading && availableLotes.length === 0 ? <Alert severity="warning">{tproduction('missingAvailableLots')}</Alert> : null}
      {!loading && activeProductos.length === 0 ? <Alert severity="warning">{tproduction('missingActiveProducts')}</Alert> : null}

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
                    <TableCell>{tf('transformedAt')}</TableCell>
                    <TableCell>{tf('transformationType')}</TableCell>
                    <TableCell>{tf('sourceBatch')}</TableCell>
                    <TableCell>{tf('product')}</TableCell>
                    <TableCell>{tf('inputQuantity')}</TableCell>
                    <TableCell>{tf('batchCode')}</TableCell>
                    <TableCell>{tf('outputProduct')}</TableCell>
                    <TableCell>{tf('outputQuantity')}</TableCell>
                    <TableCell>{tf('lossQuantity')}</TableCell>
                    <TableCell>{tf('yieldPercent')}</TableCell>
                    <TableCell>{tf('status')}</TableCell>
                    <TableCell align="right">{tf('actions')}</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {items.map((item) => (
                    <TableRow key={item.transformacionCafeId} hover>
                      <TableCell>{item.transformacionCafeId}</TableCell>
                      <TableCell>{toInputDate(item.fechaTransformacion) || '-'}</TableCell>
                      <TableCell>{item.tipoTransformacion}</TableCell>
                      <TableCell>{item.codigoLoteOrigen}</TableCell>
                      <TableCell>{item.productoOrigenNombre}</TableCell>
                      <TableCell>{item.cantidadEntrada} {item.unidadMedidaOrigen}</TableCell>
                      <TableCell>{item.codigoLoteSalida}</TableCell>
                      <TableCell>{item.productoSalidaNombre}</TableCell>
                      <TableCell>{item.cantidadSalida} {item.unidadMedidaSalida}</TableCell>
                      <TableCell>{item.merma ?? '-'}</TableCell>
                      <TableCell>{item.rendimiento != null ? `${item.rendimiento}%` : '-'}</TableCell>
                      <TableCell>
                        <Chip
                          size="small"
                          color={item.estado === 'Registrada' ? 'success' : 'default'}
                          label={resolveStatusLabel(item.estado, tproduction)}
                        />
                      </TableCell>
                      <TableCell align="right">
                        <Stack direction="row" spacing={1} justifyContent="flex-end">
                          <Button
                            size="small"
                            color="warning"
                            variant="outlined"
                            onClick={() => openAnnul(item)}
                            disabled={!canAnnul || item.estado !== 'Registrada'}
                          >
                            {tc('annul')}
                          </Button>
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

      <Dialog open={editOpen} onClose={closeEdit} fullWidth maxWidth="md">
        <DialogTitle>{`${tcrud('new')} — ${nav('transformacionesCafe')}`}</DialogTitle>
        <DialogContent dividers>
          <Stack spacing={2} sx={{ pt: 1 }}>
            {editError ? <Alert severity="error">{editError}</Alert> : null}

            <Stack direction={{ xs: 'column', md: 'row' }} spacing={2}>
              <TextField select fullWidth label={tf('sourceBatch')} value={loteOrigenId} onChange={(e) => setLoteOrigenId(e.target.value)}>
                {availableLotes.map((item) => {
                  const producto = productos.find((product) => product.productoId === item.productoId);
                  return (
                    <MenuItem key={item.loteId} value={String(item.loteId)}>
                      {item.codigoLote} · {producto?.nombre ?? item.productoId} · {item.cantidadDisponible}
                    </MenuItem>
                  );
                })}
              </TextField>
              <TextField select fullWidth label={tf('outputProduct')} value={productoSalidaId} onChange={(e) => setProductoSalidaId(e.target.value)}>
                {activeProductos.map((item) => (
                  <MenuItem key={item.productoId} value={String(item.productoId)}>
                    {item.nombre} ({item.unidadMedida})
                  </MenuItem>
                ))}
              </TextField>
            </Stack>

            <Stack direction={{ xs: 'column', md: 'row' }} spacing={2}>
              <TextField
                fullWidth
                label={tf('transformationType')}
                value={tipoTransformacion}
                onChange={(e) => setTipoTransformacion(e.target.value)}
              />
              <TextField
                fullWidth
                label={tf('transformedAt')}
                value={fechaTransformacion}
                onChange={(e) => setFechaTransformacion(e.target.value)}
                type="date"
                InputLabelProps={{ shrink: true }}
              />
            </Stack>

            <Stack direction={{ xs: 'column', md: 'row' }} spacing={2}>
              <TextField
                fullWidth
                label={tf('inputQuantity')}
                value={cantidadEntrada}
                onChange={(e) => setCantidadEntrada(e.target.value)}
                type="number"
                inputProps={{ min: 0.001, step: 0.001 }}
              />
              <TextField
                fullWidth
                label={tf('outputQuantity')}
                value={cantidadSalida}
                onChange={(e) => setCantidadSalida(e.target.value)}
                type="number"
                inputProps={{ min: 0.001, step: 0.001 }}
              />
            </Stack>

            <TextField
              fullWidth
              label={tf('processCostOptional')}
              value={costoProceso}
              onChange={(e) => setCostoProceso(e.target.value)}
              type="number"
              inputProps={{ min: 0, step: 0.01 }}
            />

            <TextField fullWidth label={tf('batchCodeOptional')} value={codigoLoteSalida} onChange={(e) => setCodigoLoteSalida(e.target.value)} />
            <TextField fullWidth label={tf('notes')} value={observaciones} onChange={(e) => setObservaciones(e.target.value)} multiline minRows={3} />
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={closeEdit} disabled={editSaving}>
            {tc('cancel')}
          </Button>
          <Button onClick={() => void saveEdit()} variant="contained" disabled={editSaving || !canSave || !canCreateRecord}>
            {tc('save')}
          </Button>
        </DialogActions>
      </Dialog>

      <Dialog open={annulOpen} onClose={closeAnnul} fullWidth maxWidth="xs">
        <DialogTitle>{tc('annul')}</DialogTitle>
        <DialogContent dividers>
          <Stack spacing={2} sx={{ pt: 1 }}>
            {annulError ? <Alert severity="error">{annulError}</Alert> : null}
            <Typography variant="body2">{tproduction('confirmAnnulTransformation')}</Typography>
            {annulTarget ? (
              <Typography variant="body2" color="text.secondary">
                #{annulTarget.transformacionCafeId} · {annulTarget.codigoLoteOrigen} → {annulTarget.codigoLoteSalida}
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