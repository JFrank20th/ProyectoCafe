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

import { useTranslations } from '@/hooks/useTranslations';
import { apiFetch } from '@/lib/api/client';
import { useAuth } from '@/lib/auth/AuthProvider';

type Producto = {
  productoId: number;
  nombre: string;
  unidadMedida: string;
  presentacionId?: number | null;
  presentacionNombre?: string | null;
  presentacionCantidad?: number | null;
  presentacionUnidadMedida?: string | null;
  permiteDecimal: boolean;
  estado: boolean;
};

type Lote = {
  loteId: number;
  productoId: number;
  productoNombre: string;
  unidadMedida: string;
  codigoLote: string;
  cantidadInicial: number;
  cantidadDisponible: number;
  estado: string;
  fechaCreacion: string;
};

type EmpaqueCafe = {
  empaqueCafeId: number;
  loteOrigenId: number;
  codigoLoteOrigen: string;
  productoOrigenId: number;
  productoOrigenNombre: string;
  unidadMedidaOrigen: string;
  fechaEmpaque?: string | null;
  cantidadOrigenConsumida: number;
  cantidadEmpaques: number;
  pesoTotalGramos: number;
  costoEmpaque: number;
  costoOrigenTotal: number;
  costoTotal: number;
  observaciones?: string | null;
  estado: string;
  detalles: Array<{
    empaqueCafeDetalleId: number;
    productoSalidaId: number;
    productoSalidaNombre: string;
    unidadMedidaSalida: string;
    presentacionId: number;
    presentacionCodigo: string;
    presentacionNombre: string;
    presentacionCantidad: number;
    presentacionUnidadMedida: string;
    loteSalidaId: number;
    codigoLoteSalida: string;
    cantidad: number;
    pesoGramosUnitario: number;
    pesoTotalGramos: number;
    cantidadOrigenAsignada: number;
    costoOrigenAsignado: number;
    costoEmpaqueAsignado: number;
    costoTotalAsignado: number;
    costoUnitarioSalida: number;
    estado: string;
  }>;
};

type EmpaqueCafeCreateRequest = {
  loteOrigenId: number;
  fechaEmpaque?: string;
  costoEmpaque: number;
  observaciones?: string;
  detalles: Array<{
    productoSalidaId: number;
    presentacionId: number;
    cantidad: number;
    codigoLoteSalida?: string;
  }>;
};

type EmpaqueCafeCreateResult = {
  empaqueCafeId: number;
};

type EditableDetalle = {
  key: number;
  productoSalidaId: string;
  cantidad: string;
  codigoLoteSalida: string;
};

function toInputDate(value?: string | null) {
  if (!value) return '';
  return value.slice(0, 10);
}

function todayInputDate() {
  return new Date().toISOString().slice(0, 10);
}

function parseQuantity(value: string) {
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : 0;
}

function formatPresentationLabel(producto?: Producto | null) {
  if (!producto?.presentacionNombre) {
    return null;
  }

  if (producto.presentacionCantidad && producto.presentacionUnidadMedida) {
    return `${producto.presentacionNombre} · ${producto.presentacionCantidad} ${producto.presentacionUnidadMedida}`;
  }

  return producto.presentacionNombre;
}

function resolvePresentationWeightGrams(producto?: Producto | null) {
  if (!producto?.presentacionCantidad || !producto.presentacionUnidadMedida) {
    return null;
  }

  switch (producto.presentacionUnidadMedida.trim().toUpperCase()) {
    case 'G':
      return producto.presentacionCantidad;
    case 'KG':
      return producto.presentacionCantidad * 1000;
    case 'LB':
      return producto.presentacionCantidad * 453.59237;
    default:
      return null;
  }
}

export function EmpaquesCafeCrud() {
  const nav = useTranslations('navigation');
  const tc = useTranslations('common');
  const tf = useTranslations('fields');
  const tp = useTranslations('pages');
  const tcrud = useTranslations('crud');
  const tproduction = useTranslations('production');

  const { has } = useAuth();
  const canView = has('Produccion.EmpaquesCafe', 'Ver');
  const canCreate = has('Produccion.EmpaquesCafe', 'Crear');
  const canAnnul = has('Produccion.EmpaquesCafe', 'Eliminar');

  const [items, setItems] = useState<EmpaqueCafe[]>([]);
  const [lotes, setLotes] = useState<Lote[]>([]);
  const [productos, setProductos] = useState<Producto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [createOpen, setCreateOpen] = useState(false);
  const [createError, setCreateError] = useState<string | null>(null);
  const [createSaving, setCreateSaving] = useState(false);
  const [lineSequence, setLineSequence] = useState(1);

  const [loteOrigenId, setLoteOrigenId] = useState('');
  const [fechaEmpaque, setFechaEmpaque] = useState(todayInputDate());
  const [costoEmpaque, setCostoEmpaque] = useState('0');
  const [observaciones, setObservaciones] = useState('');
  const [detalles, setDetalles] = useState<EditableDetalle[]>([]);

  const [annulOpen, setAnnulOpen] = useState(false);
  const [annulError, setAnnulError] = useState<string | null>(null);
  const [annulSaving, setAnnulSaving] = useState(false);
  const [annulTarget, setAnnulTarget] = useState<EmpaqueCafe | null>(null);

  const activeOutputProducts = productos.filter((item) => item.estado && item.presentacionId);
  const availableSourceLots = lotes.filter((item) => item.estado === 'Activo' && item.cantidadDisponible > 0);

  function findProduct(value: string) {
    return activeOutputProducts.find((item) => String(item.productoId) === value);
  }

  function buildLine(sequence: number, preferredProductId?: number) {
    const producto = preferredProductId
      ? activeOutputProducts.find((item) => item.productoId === preferredProductId)
      : activeOutputProducts[0];

    return {
      key: sequence,
      productoSalidaId: producto ? String(producto.productoId) : '',
      cantidad: producto?.permiteDecimal ? '1' : '1',
      codigoLoteSalida: '',
    } satisfies EditableDetalle;
  }

  function allocateLine(preferredProductId?: number) {
    const sequence = lineSequence;
    setLineSequence((current) => current + 1);
    return buildLine(sequence, preferredProductId);
  }

  async function reload() {
    setLoading(true);
    setError(null);
    try {
      const [empaquesRes, lotesRes, productosRes] = await Promise.allSettled([
        apiFetch<EmpaqueCafe[]>('/api/v1/produccion/empaques-cafe'),
        apiFetch<Lote[]>('/api/v1/lotes?soloActivos=true&soloConStock=true'),
        apiFetch<Producto[]>('/api/v1/productos'),
      ]);

      if (empaquesRes.status === 'fulfilled') {
        setItems(empaquesRes.value);
      } else {
        setItems([]);
        setError(empaquesRes.reason instanceof Error ? empaquesRes.reason.message : tcrud('errorLoadingData'));
      }

      const lotesData = lotesRes.status === 'fulfilled' ? lotesRes.value : [];
      const productosData = productosRes.status === 'fulfilled' ? productosRes.value : [];

      setLotes(lotesData);
      setProductos(productosData);

      if (!loteOrigenId && lotesData.length > 0) {
        setLoteOrigenId(String(lotesData[0]?.loteId ?? ''));
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
    setLoteOrigenId(String(availableSourceLots[0]?.loteId ?? ''));
    setFechaEmpaque(todayInputDate());
    setCostoEmpaque('0');
    setObservaciones('');
    setDetalles([allocateLine()]);
  }

  function openCreate() {
    if (!canCreate) return;
    resetForm();
    setCreateError(null);
    setCreateOpen(true);
  }

  function closeCreate() {
    if (createSaving) return;
    setCreateOpen(false);
    setCreateError(null);
  }

  function addLine() {
    setDetalles((current) => [...current, allocateLine()]);
  }

  function removeLine(lineKey: number) {
    setDetalles((current) => (current.length === 1 ? current : current.filter((item) => item.key !== lineKey)));
  }

  function updateLine(lineKey: number, changes: Partial<EditableDetalle>) {
    setDetalles((current) => current.map((item) => (item.key === lineKey ? { ...item, ...changes } : item)));
  }

  function isLineValid(line: EditableDetalle) {
    const producto = findProduct(line.productoSalidaId);
    const cantidad = parseQuantity(line.cantidad);
    if (!producto || cantidad <= 0 || !producto.presentacionId) {
      return false;
    }

    if (!producto.permiteDecimal && !Number.isInteger(cantidad)) {
      return false;
    }

    return true;
  }

  function getLineWeight(line: EditableDetalle) {
    const producto = findProduct(line.productoSalidaId);
    const weightGrams = resolvePresentationWeightGrams(producto);
    if (!weightGrams) {
      return 0;
    }

    return parseQuantity(line.cantidad) * weightGrams;
  }

  async function saveCreate() {
    setCreateSaving(true);
    setCreateError(null);

    const payload: EmpaqueCafeCreateRequest = {
      loteOrigenId: Number(loteOrigenId),
      costoEmpaque: Number(costoEmpaque),
      ...(fechaEmpaque ? { fechaEmpaque: new Date(fechaEmpaque).toISOString() } : {}),
      ...(observaciones.trim() ? { observaciones: observaciones.trim() } : {}),
      detalles: detalles.map((item) => {
        const producto = findProduct(item.productoSalidaId);
        return {
          productoSalidaId: Number(item.productoSalidaId),
          presentacionId: Number(producto?.presentacionId ?? 0),
          cantidad: parseQuantity(item.cantidad),
          ...(item.codigoLoteSalida.trim() ? { codigoLoteSalida: item.codigoLoteSalida.trim() } : {}),
        };
      }),
    };

    try {
      await apiFetch<EmpaqueCafeCreateResult>('/api/v1/produccion/empaques-cafe', {
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

  function openAnnul(item: EmpaqueCafe) {
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
      await apiFetch<void>(`/api/v1/produccion/empaques-cafe/${annulTarget.empaqueCafeId}/anular`, { method: 'POST' });
      setAnnulOpen(false);
      setAnnulTarget(null);
      await reload();
    } catch (e) {
      setAnnulError(e instanceof Error ? e.message : tproduction('errorAnnullingPackaging'));
    } finally {
      setAnnulSaving(false);
    }
  }

  const totalPeso = detalles.reduce((sum, item) => sum + getLineWeight(item), 0);
  const canCreateRecord = canCreate && availableSourceLots.length > 0 && activeOutputProducts.length > 0;
  const canSave = Boolean(loteOrigenId) && Number(costoEmpaque) >= 0 && detalles.length > 0 && detalles.every(isLineValid);

  if (!canView) {
    return (
      <Stack spacing={1}>
        <Typography variant="h4">{nav('empaquesCafe')}</Typography>
        <Typography variant="body2" color="text.secondary">
          {tp('noPermission')}
        </Typography>
      </Stack>
    );
  }

  return (
    <Stack spacing={2}>
      <Stack direction="row" alignItems="center" justifyContent="space-between" spacing={2}>
        <Typography variant="h4">{nav('empaquesCafe')}</Typography>
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
      {!loading && availableSourceLots.length === 0 ? <Alert severity="warning">{tproduction('missingAvailableLots')}</Alert> : null}
      {!loading && activeOutputProducts.length === 0 ? <Alert severity="warning">{tproduction('missingPackagingProducts')}</Alert> : null}

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
                    <TableCell>{tf('packagedAt')}</TableCell>
                    <TableCell>{tf('sourceBatch')}</TableCell>
                    <TableCell>{tf('sourceQuantity')}</TableCell>
                    <TableCell>{tf('quantity')}</TableCell>
                    <TableCell>{tf('packageCost')}</TableCell>
                    <TableCell>{tf('totalCost')}</TableCell>
                    <TableCell>{tf('status')}</TableCell>
                    <TableCell>{tf('outputProduct')}</TableCell>
                    <TableCell align="right">{tf('actions')}</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {items.map((item) => (
                    <TableRow key={item.empaqueCafeId} hover>
                      <TableCell>{item.empaqueCafeId}</TableCell>
                      <TableCell>{toInputDate(item.fechaEmpaque) || '—'}</TableCell>
                      <TableCell>
                        <Stack spacing={0.25}>
                          <Typography variant="body2">{item.codigoLoteOrigen}</Typography>
                          <Typography variant="caption" color="text.secondary">{item.productoOrigenNombre}</Typography>
                        </Stack>
                      </TableCell>
                      <TableCell>{item.cantidadOrigenConsumida} {item.unidadMedidaOrigen}</TableCell>
                      <TableCell>{item.cantidadEmpaques}</TableCell>
                      <TableCell>{item.costoEmpaque}</TableCell>
                      <TableCell>{item.costoTotal}</TableCell>
                      <TableCell>
                        <Chip size="small" color={item.estado === 'Registrado' ? 'success' : 'default'} label={item.estado} />
                      </TableCell>
                      <TableCell>
                        <Stack spacing={0.5}>
                          {item.detalles.map((detalle) => (
                            <Typography key={detalle.empaqueCafeDetalleId} variant="caption" color="text.secondary">
                              {detalle.productoSalidaNombre} · {detalle.cantidad} · {detalle.codigoLoteSalida}
                            </Typography>
                          ))}
                        </Stack>
                      </TableCell>
                      <TableCell align="right">
                        <Button size="small" color="warning" variant="outlined" onClick={() => openAnnul(item)} disabled={!canAnnul || item.estado !== 'Registrado'}>
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

      <Dialog open={createOpen} onClose={closeCreate} fullWidth maxWidth="lg">
        <DialogTitle>{`${tcrud('new')} — ${nav('empaquesCafe')}`}</DialogTitle>
        <DialogContent dividers>
          <Stack spacing={2} sx={{ pt: 1 }}>
            {createError ? <Alert severity="error">{createError}</Alert> : null}

            <Stack direction={{ xs: 'column', md: 'row' }} spacing={2}>
              <TextField select fullWidth label={tf('sourceBatch')} value={loteOrigenId} onChange={(e) => setLoteOrigenId(e.target.value)}>
                {availableSourceLots.map((item) => (
                  <MenuItem key={item.loteId} value={String(item.loteId)}>
                    {item.codigoLote} · {item.productoNombre} · {item.cantidadDisponible} {item.unidadMedida}
                  </MenuItem>
                ))}
              </TextField>
              <TextField
                fullWidth
                label={tf('packagedAt')}
                value={fechaEmpaque}
                onChange={(e) => setFechaEmpaque(e.target.value)}
                type="date"
                InputLabelProps={{ shrink: true }}
              />
            </Stack>

            <Stack direction={{ xs: 'column', md: 'row' }} spacing={2}>
              <TextField
                fullWidth
                label={tf('packageCost')}
                value={costoEmpaque}
                onChange={(e) => setCostoEmpaque(e.target.value)}
                type="number"
                inputProps={{ min: 0, step: 0.01 }}
              />
              <TextField fullWidth label={tf('weightGrams')} value={totalPeso} InputProps={{ readOnly: true }} />
            </Stack>

            <TextField fullWidth label={tf('notes')} value={observaciones} onChange={(e) => setObservaciones(e.target.value)} multiline minRows={3} />

            <Card variant="outlined">
              <CardHeader title={tproduction('packagingLinesTitle')} action={<Button onClick={addLine}>{tsalesOrFallback('addLine')}</Button>} />
              <CardContent sx={{ pt: 0 }}>
                <Stack spacing={2}>
                  {detalles.map((line, index) => {
                    const producto = findProduct(line.productoSalidaId);
                    return (
                      <Card key={line.key} variant="outlined">
                        <CardContent>
                          <Stack spacing={2}>
                            <Stack direction="row" alignItems="center" justifyContent="space-between" spacing={2}>
                              <Typography variant="subtitle2">{tproduction('packagingLineLabel')} {index + 1}</Typography>
                              <Button color="error" onClick={() => removeLine(line.key)} disabled={detalles.length === 1}>
                                {tc('delete')}
                              </Button>
                            </Stack>

                            <Stack direction={{ xs: 'column', md: 'row' }} spacing={2}>
                              <TextField
                                select
                                fullWidth
                                label={tf('outputProduct')}
                                value={line.productoSalidaId}
                                onChange={(e) => updateLine(line.key, { productoSalidaId: e.target.value })}
                              >
                                {activeOutputProducts.map((item) => (
                                  <MenuItem key={item.productoId} value={String(item.productoId)}>
                                    {item.nombre}{formatPresentationLabel(item) ? ` · ${formatPresentationLabel(item)}` : ''}
                                  </MenuItem>
                                ))}
                              </TextField>
                              <TextField
                                fullWidth
                                label={tf('quantity')}
                                value={line.cantidad}
                                onChange={(e) => updateLine(line.key, { cantidad: e.target.value })}
                                type="number"
                                inputProps={{ min: 0.001, step: producto?.permiteDecimal ? 0.001 : 1 }}
                                error={!isLineValid(line)}
                                helperText={resolvePresentationWeightGrams(producto) ? `${getLineWeight(line)} ${tf('weightGrams')}` : formatPresentationLabel(producto) ?? '—'}
                              />
                            </Stack>

                            <TextField
                              fullWidth
                              label={tf('batchCodeOptional')}
                              value={line.codigoLoteSalida}
                              onChange={(e) => updateLine(line.key, { codigoLoteSalida: e.target.value })}
                            />

                            <Typography variant="caption" color="text.secondary">
                              {formatPresentationLabel(producto) ?? '—'}
                            </Typography>
                          </Stack>
                        </CardContent>
                      </Card>
                    );
                  })}
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
            <Typography variant="body2">{tproduction('confirmAnnulPackaging')}</Typography>
            {annulTarget ? (
              <Typography variant="body2" color="text.secondary">
                #{annulTarget.empaqueCafeId} · {annulTarget.codigoLoteOrigen}
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

  function tsalesOrFallback(key: string) {
    return key === 'addLine' ? tproduction('addPackagingLine') : key;
  }
}