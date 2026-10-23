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

type Proveedor = {
  proveedorId: number;
  nombre: string;
  estado: boolean;
};

type Producto = {
  productoId: number;
  nombre: string;
  unidadMedida: string;
  permiteDecimal: boolean;
  estado: boolean;
};

type RecepcionCafe = {
  recepcionCafeId: number;
  proveedorId: number;
  proveedorNombre: string;
  productoId: number;
  productoNombre: string;
  unidadMedida: string;
  tipoCafe: string;
  fechaRecepcion?: string | null;
  cantidad: number;
  cantidadDisponible: number;
  precioUnitario: number;
  total: number;
  humedad?: number | null;
  observaciones?: string | null;
  estado: string;
  codigoLote: string;
};

type RecepcionCafeCreateRequest = {
  proveedorId: number;
  productoId: number;
  tipoCafe: string;
  fechaRecepcion?: string;
  cantidad: number;
  precioUnitario: number;
  humedad?: number;
  observaciones?: string;
};

type RecepcionCafeCreateResult = {
  recepcionCafeId: number;
  codigoLote: string;
};

const COFFEE_TYPES = ['Cereza', 'Oreado', 'RecienLavado', 'Seco'] as const;

function toInputDate(value?: string | null) {
  if (!value) return '';
  return value.slice(0, 10);
}

function todayInputDate() {
  return new Date().toISOString().slice(0, 10);
}

function parseOptionalNumber(value: string) {
  const trimmed = value.trim();
  if (!trimmed) return undefined;
  return Number(trimmed);
}

function mapCoffeeTypeLabel(value: string, tpurchases: (key: string) => string) {
  switch (value) {
    case 'Cereza':
      return tpurchases('coffeeTypeCereza');
    case 'Oreado':
      return tpurchases('coffeeTypeOreado');
    case 'RecienLavado':
      return tpurchases('coffeeTypeRecienLavado');
    default:
      return tpurchases('coffeeTypeSeco');
  }
}

function mapStatusLabel(value: string, tpurchases: (key: string) => string) {
  if (value === 'Anulada') {
    return tpurchases('statusAnnulled');
  }

  return tpurchases('statusRegistered');
}

export function RecepcionesCafeCrud() {
  const nav = useTranslations('navigation');
  const tc = useTranslations('common');
  const tf = useTranslations('fields');
  const tp = useTranslations('pages');
  const tcrud = useTranslations('crud');
  const tpurchases = useTranslations('purchases');

  const { has } = useAuth();
  const canView = has('Compras.RecepcionesCafe', 'Ver');
  const canCreate = has('Compras.RecepcionesCafe', 'Crear');
  const canAnnul = has('Compras.RecepcionesCafe', 'Eliminar');

  const [items, setItems] = useState<RecepcionCafe[]>([]);
  const [proveedores, setProveedores] = useState<Proveedor[]>([]);
  const [productos, setProductos] = useState<Producto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [editOpen, setEditOpen] = useState(false);
  const [editError, setEditError] = useState<string | null>(null);
  const [editSaving, setEditSaving] = useState(false);

  const [proveedorId, setProveedorId] = useState('');
  const [productoId, setProductoId] = useState('');
  const [tipoCafe, setTipoCafe] = useState<(typeof COFFEE_TYPES)[number]>('Cereza');
  const [fechaRecepcion, setFechaRecepcion] = useState(todayInputDate());
  const [cantidad, setCantidad] = useState('0');
  const [precioUnitario, setPrecioUnitario] = useState('0');
  const [humedad, setHumedad] = useState('');
  const [observaciones, setObservaciones] = useState('');

  const [annulOpen, setAnnulOpen] = useState(false);
  const [annulError, setAnnulError] = useState<string | null>(null);
  const [annulSaving, setAnnulSaving] = useState(false);
  const [annulTarget, setAnnulTarget] = useState<RecepcionCafe | null>(null);

  const activeProveedores = proveedores.filter((item) => item.estado);
  const activeProductos = productos.filter((item) => item.estado);

  async function reload() {
    setLoading(true);
    setError(null);
    try {
      const [recepcionesRes, proveedoresRes, productosRes] = await Promise.allSettled([
        apiFetch<RecepcionCafe[]>('/api/v1/compras/recepciones-cafe'),
        apiFetch<Proveedor[]>('/api/v1/compras/proveedores?soloExternos=true'),
        apiFetch<Producto[]>('/api/v1/productos'),
      ]);

      if (recepcionesRes.status === 'fulfilled') {
        setItems(recepcionesRes.value);
      } else {
        setItems([]);
        setError(recepcionesRes.reason instanceof Error ? recepcionesRes.reason.message : tcrud('errorLoadingData'));
      }

      const proveedoresData = proveedoresRes.status === 'fulfilled' ? proveedoresRes.value : [];
      const productosData = productosRes.status === 'fulfilled' ? productosRes.value : [];
      setProveedores(proveedoresData);
      setProductos(productosData);

      if (!proveedorId && proveedoresData.some((item) => item.estado)) {
        setProveedorId(String(proveedoresData.find((item) => item.estado)?.proveedorId ?? ''));
      }

      if (!productoId && productosData.some((item) => item.estado)) {
        setProductoId(String(productosData.find((item) => item.estado)?.productoId ?? ''));
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
    setProveedorId(String(activeProveedores[0]?.proveedorId ?? ''));
    setProductoId(String(activeProductos[0]?.productoId ?? ''));
    setTipoCafe('Cereza');
    setFechaRecepcion(todayInputDate());
    setCantidad('0');
    setPrecioUnitario('0');
    setHumedad('');
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
    const cantidadValue = Number(cantidad);
    const selectedProducto = productos.find((item) => item.productoId === Number(productoId));
    if (selectedProducto && !selectedProducto.permiteDecimal && !Number.isInteger(cantidadValue)) {
      setEditError(tpurchases('integerQuantityOnly'));
      return;
    }

    setEditSaving(true);
    setEditError(null);

    const humedadValue = parseOptionalNumber(humedad);
    const payload: RecepcionCafeCreateRequest = {
      proveedorId: Number(proveedorId),
      productoId: Number(productoId),
      tipoCafe,
      cantidad: cantidadValue,
      precioUnitario: Number(precioUnitario),
      ...(fechaRecepcion ? { fechaRecepcion: new Date(fechaRecepcion).toISOString() } : {}),
      ...(humedadValue !== undefined ? { humedad: humedadValue } : {}),
      ...(observaciones.trim() ? { observaciones: observaciones.trim() } : {}),
    };

    try {
      await apiFetch<RecepcionCafeCreateResult>('/api/v1/compras/recepciones-cafe', {
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

  function openAnnul(item: RecepcionCafe) {
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
      await apiFetch<void>(`/api/v1/compras/recepciones-cafe/${annulTarget.recepcionCafeId}/anular`, { method: 'POST' });
      setAnnulOpen(false);
      setAnnulTarget(null);
      await reload();
    } catch (e) {
      setAnnulError(e instanceof Error ? e.message : tpurchases('errorAnnullingReception'));
    } finally {
      setAnnulSaving(false);
    }
  }

  const canCreateRecord = canCreate && activeProveedores.length > 0 && activeProductos.length > 0;
  const selectedProducto = productos.find((item) => item.productoId === Number(productoId));
  const quantityAllowsDecimal = selectedProducto?.permiteDecimal ?? true;
  const cantidadValue = Number(cantidad);
  const hasValidQuantity = cantidadValue > 0 && (quantityAllowsDecimal || Number.isInteger(cantidadValue));
  const canSave = Boolean(proveedorId) && Boolean(productoId) && hasValidQuantity;

  if (!canView) {
    return (
      <Stack spacing={1}>
        <Typography variant="h4">{nav('recepcionesCafe')}</Typography>
        <Typography variant="body2" color="text.secondary">
          {tp('noPermission')}
        </Typography>
      </Stack>
    );
  }

  return (
    <Stack spacing={2}>
      <Stack direction="row" alignItems="center" justifyContent="space-between" spacing={2}>
        <Typography variant="h4">{nav('recepcionesCafe')}</Typography>
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
      {!loading && activeProveedores.length === 0 ? <Alert severity="warning">{tpurchases('missingActiveSuppliers')}</Alert> : null}
      {!loading && activeProductos.length === 0 ? <Alert severity="warning">{tpurchases('missingActiveProducts')}</Alert> : null}

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
                    <TableCell>{tf('receivedAt')}</TableCell>
                    <TableCell>{tf('supplier')}</TableCell>
                    <TableCell>{tf('product')}</TableCell>
                    <TableCell>{tf('coffeeState')}</TableCell>
                    <TableCell>{tf('quantity')}</TableCell>
                    <TableCell>{tf('availableQuantity')}</TableCell>
                    <TableCell>{tf('unitPrice')}</TableCell>
                    <TableCell>{tf('total')}</TableCell>
                    <TableCell>{tf('batchCode')}</TableCell>
                    <TableCell>{tf('status')}</TableCell>
                    <TableCell align="right">{tf('actions')}</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {items.map((item) => (
                    <TableRow key={item.recepcionCafeId} hover>
                      <TableCell>{item.recepcionCafeId}</TableCell>
                      <TableCell>{toInputDate(item.fechaRecepcion) || '-'}</TableCell>
                      <TableCell>{item.proveedorNombre}</TableCell>
                      <TableCell>{item.productoNombre} ({item.unidadMedida})</TableCell>
                      <TableCell>{mapCoffeeTypeLabel(item.tipoCafe, tpurchases)}</TableCell>
                      <TableCell>{item.cantidad}</TableCell>
                      <TableCell>{item.cantidadDisponible}</TableCell>
                      <TableCell>{item.precioUnitario}</TableCell>
                      <TableCell>{item.total}</TableCell>
                      <TableCell>{item.codigoLote}</TableCell>
                      <TableCell>
                        <Chip
                          size="small"
                          color={item.estado === 'Registrada' ? 'success' : 'default'}
                          label={mapStatusLabel(item.estado, tpurchases)}
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
        <DialogTitle>{`${tcrud('new')} — ${nav('recepcionesCafe')}`}</DialogTitle>
        <DialogContent dividers>
          <Stack spacing={2} sx={{ pt: 1 }}>
            {editError ? <Alert severity="error">{editError}</Alert> : null}

            <Stack direction={{ xs: 'column', md: 'row' }} spacing={2}>
              <TextField select fullWidth label={tf('supplier')} value={proveedorId} onChange={(e) => setProveedorId(e.target.value)}>
                {activeProveedores.map((item) => (
                  <MenuItem key={item.proveedorId} value={String(item.proveedorId)}>
                    {item.nombre}
                  </MenuItem>
                ))}
              </TextField>
              <TextField select fullWidth label={tf('product')} value={productoId} onChange={(e) => setProductoId(e.target.value)}>
                {activeProductos.map((item) => (
                  <MenuItem key={item.productoId} value={String(item.productoId)}>
                    {item.nombre} (KG)
                  </MenuItem>
                ))}
              </TextField>
            </Stack>

            <Stack direction={{ xs: 'column', md: 'row' }} spacing={2}>
              <TextField select fullWidth label={tf('coffeeState')} value={tipoCafe} onChange={(e) => setTipoCafe(e.target.value as (typeof COFFEE_TYPES)[number])}>
                {COFFEE_TYPES.map((item) => (
                  <MenuItem key={item} value={item}>
                    {mapCoffeeTypeLabel(item, tpurchases)}
                  </MenuItem>
                ))}
              </TextField>
              <TextField
                fullWidth
                label={tf('receivedAt')}
                value={fechaRecepcion}
                onChange={(e) => setFechaRecepcion(e.target.value)}
                type="date"
                InputLabelProps={{ shrink: true }}
              />
            </Stack>

            <Stack direction={{ xs: 'column', md: 'row' }} spacing={2}>
              <TextField
                fullWidth
                label={`${tf('quantity')} (KG)`}
                value={cantidad}
                onChange={(e) => setCantidad(e.target.value)}
                type="number"
                inputProps={quantityAllowsDecimal ? { min: 0.001, step: 0.001 } : { min: 1, step: 1 }}
                helperText={!quantityAllowsDecimal ? tpurchases('integerQuantityOnly') : undefined}
              />
              <TextField
                fullWidth
                label={tf('unitPrice')}
                value={precioUnitario}
                onChange={(e) => setPrecioUnitario(e.target.value)}
                type="number"
                inputProps={{ min: 0, step: 0.01 }}
              />
            </Stack>

            <Stack direction={{ xs: 'column', md: 'row' }} spacing={2}>
              <TextField
                fullWidth
                label={tf('moistureOptional')}
                value={humedad}
                onChange={(e) => setHumedad(e.target.value)}
                type="number"
                inputProps={{ min: 0, max: 100, step: 0.01 }}
              />
            </Stack>

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
            <Typography variant="body2">{tpurchases('confirmAnnulReception')}</Typography>
            {annulTarget ? (
              <Typography variant="body2" color="text.secondary">
                #{annulTarget.recepcionCafeId} · {annulTarget.proveedorNombre} · {annulTarget.codigoLote}
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