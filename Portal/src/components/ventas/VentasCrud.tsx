'use client';

import { useSearchParams } from 'next/navigation';

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

type Cliente = {
  clienteId: number;
  nombre: string;
  estado: boolean;
};

type MetodoPago = {
  metodoPagoId: number;
  nombre: string;
  estado: boolean;
};

type TipoVenta = {
  tipoVentaId: number;
  codigo: string;
  nombre: string;
  orden: number;
  estado: boolean;
};

type ProductoPrecioTipoVenta = {
  tipoVentaId: number;
  tipoVentaCodigo: string;
  tipoVentaNombre: string;
  precio: number;
  estado: boolean;
};

type Producto = {
  productoId: number;
  nombre: string;
  precio: number;
  precioInternet: number;
  precioMayorista: number;
  stock: number;
  manejaLotes: boolean;
  unidadMedida: string;
  permiteDecimal: boolean;
  estado: boolean;
  presentacionId?: number | null;
  presentacionNombre?: string | null;
  presentacionCantidad?: number | null;
  presentacionUnidadMedida?: string | null;
  preciosTipoVenta: ProductoPrecioTipoVenta[];
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

type Venta = {
  ventaId: number;
  clienteId: number;
  clienteNombre: string;
  fecha?: string | null;
  total: number;
  costoTotal?: number;
  margenBruto?: number;
  rentabilidadPorcentaje?: number;
  estadoPago: string;
  metodoPagoId?: number | null;
  metodoPagoNombre?: string | null;
  tipoPrecio: string;
  estado: boolean;
};

type VentaView = {
  venta: Venta;
  detalles: Array<{
    detalleId: number;
    ventaId: number;
    productoId: number;
    productoNombre: string;
    unidadMedida: string;
    presentacionId?: number | null;
    presentacionCodigo?: string | null;
    presentacionNombre?: string | null;
    presentacionCantidad?: number | null;
    presentacionUnidadMedida?: string | null;
    loteId?: number | null;
    codigoLote?: string | null;
    cantidad: number;
    precioUnitario: number;
    subtotal: number;
    costoUnitario?: number;
    costoTotal?: number;
    margenUnitario?: number;
    margenBruto?: number;
    rentabilidadPorcentaje?: number;
  }>;
  pagos: Array<{
    ventaPagoId: number;
    ventaId: number;
    metodoPagoId: number;
    metodoPagoNombre: string;
    monto: number;
  }>;
  loteMovimientos: Array<{
    movimientoId: number;
    loteId: number;
    productoId: number;
    productoNombre: string;
    unidadMedida: string;
    codigoLote: string;
    tipo: string;
    fecha: string;
    cantidad: number;
    observacion?: string | null;
  }>;
};

type VentaCreateRequest = {
  clienteId: number;
  estadoPago: string;
  metodoPagoId?: number;
  tipoPrecio: string;
  detalles: Array<{
    productoId: number;
    loteId?: number;
    cantidad: number;
    precioUnitario: number;
  }>;
};

type EditableDetalle = {
  key: number;
  productoId: string;
  loteId: string;
  cantidad: string;
};

const PAYMENT_STATUSES = ['Pagado', 'Pendiente', 'Parcial'] as const;

function toInputDate(value?: string | null) {
  if (!value) return '';
  return value.slice(0, 10);
}

function roundMoney(value: number) {
  return Math.round(value * 100) / 100;
}

function parseQuantity(value: string) {
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : 0;
}

function resolveLegacyPrice(producto: Producto, selectedPriceType: string) {
  switch (selectedPriceType) {
    case 'Internet':
      return producto.precioInternet;
    case 'Mayorista':
      return producto.precioMayorista;
    default:
      return producto.precio;
  }
}

function formatProductLabel(producto: Producto) {
  return producto.nombre;
}

function formatPresentationLabel(nombre?: string | null, cantidad?: number | null, unidadMedida?: string | null) {
  const normalizedNombre = nombre?.trim() ?? null;
  const detail = cantidad && unidadMedida ? `${cantidad} ${unidadMedida}` : null;

  if (detail && normalizedNombre && normalizedNombre.localeCompare(detail, 'es', { sensitivity: 'base' }) !== 0) {
    return `${normalizedNombre} · ${detail}`;
  }

  return detail ?? normalizedNombre;
}

function mapPriceTypeLabel(value: string, tiposVenta: TipoVenta[], tsales: (key: string) => string) {
  const tipoVenta = tiposVenta.find((item) => item.codigo === value || item.nombre === value);
  const code = tipoVenta?.codigo ?? value;

  switch (code) {
    case 'Internet':
      return tsales('priceTypeInternet');
    case 'Mayorista':
      return tsales('priceTypeMayorista');
    case 'Normal':
      return tsales('priceTypeNormal');
    default:
      return tipoVenta?.nombre ?? value;
  }
}

function mapPaymentStatusLabel(value: string, tsales: (key: string) => string) {
  switch (value) {
    case 'Pendiente':
      return tsales('paymentStatusPendiente');
    case 'Parcial':
      return tsales('paymentStatusParcial');
    default:
      return tsales('paymentStatusPagado');
  }
}

function mapSaleStatusLabel(value: boolean, tsales: (key: string) => string) {
  return value ? tsales('statusRegistered') : tsales('statusAnnulled');
}

export function VentasCrud() {
  const nav = useTranslations('navigation');
  const tc = useTranslations('common');
  const tf = useTranslations('fields');
  const tp = useTranslations('pages');
  const tcrud = useTranslations('crud');
  const tsales = useTranslations('sales');
  const searchParams = useSearchParams();

  const { has } = useAuth();
  const canView = has('Ventas.Ventas', 'Ver');
  const canCreate = has('Ventas.Ventas', 'Crear');
  const canAnnul = has('Ventas.Ventas', 'Eliminar');

  const [items, setItems] = useState<Venta[]>([]);
  const [clientes, setClientes] = useState<Cliente[]>([]);
  const [metodosPago, setMetodosPago] = useState<MetodoPago[]>([]);
  const [tiposVenta, setTiposVenta] = useState<TipoVenta[]>([]);
  const [productos, setProductos] = useState<Producto[]>([]);
  const [lotes, setLotes] = useState<Lote[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [createOpen, setCreateOpen] = useState(false);
  const [createError, setCreateError] = useState<string | null>(null);
  const [createSaving, setCreateSaving] = useState(false);
  const [lineSequence, setLineSequence] = useState(1);

  const [clienteId, setClienteId] = useState('');
  const [metodoPagoId, setMetodoPagoId] = useState('');
  const [estadoPago, setEstadoPago] = useState<(typeof PAYMENT_STATUSES)[number]>('Pagado');
  const [tipoPrecio, setTipoPrecio] = useState('Normal');
  const [detalles, setDetalles] = useState<EditableDetalle[]>([]);

  const [detailOpen, setDetailOpen] = useState(false);
  const [detailLoading, setDetailLoading] = useState(false);
  const [detailError, setDetailError] = useState<string | null>(null);
  const [detail, setDetail] = useState<VentaView | null>(null);

  const [annulOpen, setAnnulOpen] = useState(false);
  const [annulError, setAnnulError] = useState<string | null>(null);
  const [annulSaving, setAnnulSaving] = useState(false);
  const [annulTarget, setAnnulTarget] = useState<Venta | null>(null);
  const [queryHandled, setQueryHandled] = useState(false);

  const activeClientes = clientes.filter((item) => item.estado);
  const activeMetodosPago = metodosPago.filter((item) => item.estado);
  const activeTiposVenta = tiposVenta
    .filter((item) => item.estado)
    .sort((left, right) => left.orden - right.orden || left.tipoVentaId - right.tipoVentaId);
  const saleProducts = productos.filter((item) => item.estado && item.stock > 0);
  const availableLots = lotes.filter((item) => item.estado === 'Activo' && item.cantidadDisponible > 0);
  const sellableProducts = saleProducts.filter((item) => !item.manejaLotes || availableLots.some((lot) => lot.productoId === item.productoId));

  function findProduct(value: string) {
    return sellableProducts.find((item) => String(item.productoId) === value);
  }

  function findLot(value: string) {
    return availableLots.find((item) => String(item.loteId) === value);
  }

  function resolveDynamicPrice(producto: Producto, selectedPriceType: string) {
    const selectedTipoVenta = activeTiposVenta.find((item) => item.codigo === selectedPriceType || item.nombre === selectedPriceType);

    const dynamic = producto.preciosTipoVenta.find(
      (item) =>
        item.estado &&
        (
          item.tipoVentaId === selectedTipoVenta?.tipoVentaId ||
          item.tipoVentaCodigo === selectedPriceType ||
          item.tipoVentaNombre === selectedPriceType ||
          item.tipoVentaCodigo === selectedTipoVenta?.codigo ||
          item.tipoVentaNombre === selectedTipoVenta?.nombre
        )
    );

    if (dynamic) {
      return dynamic.precio;
    }

    return resolveLegacyPrice(producto, selectedTipoVenta?.codigo ?? selectedPriceType);
  }

  function buildLine(sequence: number, preferredProductId?: number): EditableDetalle {
    const producto = preferredProductId
      ? sellableProducts.find((item) => item.productoId === preferredProductId)
      : sellableProducts[0];
    const lote = producto?.manejaLotes ? availableLots.find((item) => item.productoId === producto.productoId) : undefined;

    return {
      key: sequence,
      productoId: producto ? String(producto.productoId) : '',
      loteId: producto?.manejaLotes ? String(lote?.loteId ?? '') : '',
      cantidad: '1',
    };
  }

  function allocateLine(preferredProductId?: number) {
    const sequence = lineSequence;
    setLineSequence((current) => current + 1);
    return buildLine(sequence, preferredProductId);
  }

  function getReservedLotQuantity(lineKey: number, loteId: number) {
    return detalles
      .filter((item) => item.key !== lineKey && Number(item.loteId) === loteId)
      .reduce((sum, item) => sum + parseQuantity(item.cantidad), 0);
  }

  function getReservedProductQuantity(lineKey: number, productoId: number) {
    return detalles
      .filter((item) => item.key !== lineKey && Number(item.productoId) === productoId && !item.loteId)
      .reduce((sum, item) => sum + parseQuantity(item.cantidad), 0);
  }

  function getAvailableForLine(line: EditableDetalle) {
    const producto = findProduct(line.productoId);
    if (!producto) {
      return 0;
    }

    if (producto.manejaLotes) {
      const lote = findLot(line.loteId);
      if (!lote) {
        return 0;
      }

      return lote.cantidadDisponible - getReservedLotQuantity(line.key, lote.loteId);
    }

    return producto.stock - getReservedProductQuantity(line.key, producto.productoId);
  }

  function isLineValid(line: EditableDetalle) {
    const producto = findProduct(line.productoId);
    const cantidad = parseQuantity(line.cantidad);
    if (!producto || cantidad <= 0) {
      return false;
    }

    if (!producto.permiteDecimal && !Number.isInteger(cantidad)) {
      return false;
    }

    if (producto.manejaLotes) {
      const lote = findLot(line.loteId);
      if (!lote || lote.productoId !== producto.productoId) {
        return false;
      }
    }

    return cantidad <= getAvailableForLine(line);
  }

  function getLineSubtotal(line: EditableDetalle) {
    const producto = findProduct(line.productoId);
    if (!producto) {
      return 0;
    }

    return roundMoney(parseQuantity(line.cantidad) * resolveDynamicPrice(producto, tipoPrecio));
  }

  const totalPreview = roundMoney(detalles.reduce((sum, item) => sum + getLineSubtotal(item), 0));
  const selectedCliente = activeClientes.find((item) => String(item.clienteId) === clienteId);
  const selectedMetodoPago = activeMetodosPago.find((item) => String(item.metodoPagoId) === metodoPagoId);
  const selectedTipoVenta = activeTiposVenta.find((item) => item.codigo === tipoPrecio);
  const invalidLineCount = detalles.filter((item) => !isLineValid(item)).length;
  const linesUsingLots = detalles.filter((item) => findProduct(item.productoId)?.manejaLotes).length;
  const canCreateRecord = canCreate && activeClientes.length > 0 && activeTiposVenta.length > 0 && sellableProducts.length > 0;
  const canSave = Boolean(clienteId) && Boolean(tipoPrecio) && detalles.length > 0 && detalles.every(isLineValid);

  async function reload() {
    setLoading(true);
    setError(null);
    try {
      const [ventasRes, clientesRes, metodosRes, tiposRes, productosRes, lotesRes] = await Promise.allSettled([
        apiFetch<Venta[]>('/api/v1/ventas'),
        apiFetch<Cliente[]>('/api/v1/clientes'),
        apiFetch<MetodoPago[]>('/api/v1/metodos-pago'),
        apiFetch<TipoVenta[]>('/api/v1/tipos-venta'),
        apiFetch<Producto[]>('/api/v1/productos'),
        apiFetch<Lote[]>('/api/v1/lotes?soloActivos=true&soloConStock=true'),
      ]);

      if (ventasRes.status === 'fulfilled') {
        setItems(ventasRes.value);
      } else {
        setItems([]);
        setError(ventasRes.reason instanceof Error ? ventasRes.reason.message : tcrud('errorLoadingData'));
      }

      const clientesData = clientesRes.status === 'fulfilled' ? clientesRes.value : [];
      const metodosData = metodosRes.status === 'fulfilled' ? metodosRes.value : [];
      const tiposData = tiposRes.status === 'fulfilled' ? tiposRes.value : [];
      const productosData = productosRes.status === 'fulfilled' ? productosRes.value : [];
      const lotesData = lotesRes.status === 'fulfilled' ? lotesRes.value : [];

      setClientes(clientesData);
      setMetodosPago(metodosData);
      setTiposVenta(tiposData);
      setProductos(productosData);
      setLotes(lotesData);

      const firstCliente = clientesData.find((item) => item.estado);
      if (!clienteId && firstCliente) {
        setClienteId(String(firstCliente.clienteId));
      }

      const firstTipoVenta = tiposData
        .filter((item) => item.estado)
        .sort((left, right) => left.orden - right.orden || left.tipoVentaId - right.tipoVentaId)[0];

      if (!tipoPrecio && firstTipoVenta) {
        setTipoPrecio(firstTipoVenta.codigo);
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

  useEffect(() => {
    if (loading || queryHandled) {
      return;
    }

    const requestedProductId = searchParams.get('productoId');
    const shouldOpen = searchParams.get('open') === '1';
    const validRequestedProductId = requestedProductId && sellableProducts.some((item) => String(item.productoId) === requestedProductId)
      ? Number(requestedProductId)
      : undefined;

    if (shouldOpen && validRequestedProductId && canCreateRecord) {
      openCreate(validRequestedProductId);
    }

    setQueryHandled(true);
  }, [canCreateRecord, loading, queryHandled, searchParams, sellableProducts]);

  function resetForm(preferredProductId?: number) {
    setClienteId(String(activeClientes[0]?.clienteId ?? ''));
    setMetodoPagoId('');
    setEstadoPago('Pagado');
    setTipoPrecio(activeTiposVenta[0]?.codigo ?? 'Normal');
    setDetalles([allocateLine(preferredProductId)]);
  }

  function openCreate(preferredProductId?: number) {
    if (!canCreateRecord) return;
    resetForm(preferredProductId);
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
    setDetalles((current) =>
      current.map((item) => {
        if (item.key !== lineKey) {
          return item;
        }

        const next = { ...item, ...changes };
        const producto = findProduct(next.productoId);
        const lote = findLot(next.loteId);

        if (producto?.manejaLotes) {
          if (!lote || lote.productoId !== producto.productoId) {
            next.loteId = String(availableLots.find((candidate) => candidate.productoId === producto.productoId)?.loteId ?? '');
          }
        } else {
          next.loteId = '';
        }

        return next;
      })
    );
  }

  async function saveCreate() {
    setCreateSaving(true);
    setCreateError(null);

    const payload: VentaCreateRequest = {
      clienteId: Number(clienteId),
      estadoPago,
      tipoPrecio,
      ...(metodoPagoId ? { metodoPagoId: Number(metodoPagoId) } : {}),
      detalles: detalles.map((item) => {
        const producto = findProduct(item.productoId);
        const cantidad = parseQuantity(item.cantidad);

        return {
          productoId: Number(item.productoId),
          ...(item.loteId ? { loteId: Number(item.loteId) } : {}),
          cantidad,
          precioUnitario: producto ? resolveDynamicPrice(producto, tipoPrecio) : 0,
        };
      }),
    };

    try {
      await apiFetch<{ ventaId: number }>('/api/v1/ventas', {
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

  async function openDetail(item: Venta) {
    setDetailOpen(true);
    setDetail(null);
    setDetailError(null);
    setDetailLoading(true);
    try {
      const data = await apiFetch<VentaView>(`/api/v1/ventas/${item.ventaId}`);
      setDetail(data);
    } catch (e) {
      setDetailError(e instanceof Error ? e.message : tcrud('errorLoadingData'));
    } finally {
      setDetailLoading(false);
    }
  }

  function closeDetail() {
    if (detailLoading) return;
    setDetailOpen(false);
    setDetail(null);
    setDetailError(null);
  }

  function openAnnul(item: Venta) {
    if (!canAnnul || !item.estado) return;
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
      await apiFetch<void>(`/api/v1/ventas/${annulTarget.ventaId}/anular`, { method: 'POST' });
      setAnnulOpen(false);
      setAnnulTarget(null);
      await reload();
      if (detail?.venta.ventaId === annulTarget.ventaId) {
        await openDetail({ ...annulTarget, estado: false });
      }
    } catch (e) {
      setAnnulError(e instanceof Error ? e.message : tsales('errorAnnullingSale'));
    } finally {
      setAnnulSaving(false);
    }
  }

  if (!canView) {
    return (
      <Stack spacing={1}>
        <Typography variant="h4">{nav('ventas')}</Typography>
        <Typography variant="body2" color="text.secondary">{tp('noPermission')}</Typography>
      </Stack>
    );
  }

  return (
    <Stack spacing={2}>
      <Stack direction="row" alignItems="center" justifyContent="space-between" spacing={2}>
        <Typography variant="h4">{nav('ventas')}</Typography>
        <Stack direction="row" spacing={1}>
          <Button variant="outlined" onClick={() => void reload()} disabled={loading}>{tc('refresh')}</Button>
          <Button variant="contained" onClick={() => openCreate()} disabled={!canCreateRecord}>{tcrud('new')}</Button>
        </Stack>
      </Stack>

      {error ? <Alert severity="error">{error}</Alert> : null}
      {!loading && activeClientes.length === 0 ? <Alert severity="warning">{tsales('missingActiveCustomers')}</Alert> : null}
      {!loading && saleProducts.length === 0 ? <Alert severity="warning">{tsales('missingActiveProducts')}</Alert> : null}
      {!loading && activeTiposVenta.length === 0 ? <Alert severity="warning">{tsales('missingSaleTypes')}</Alert> : null}
      {!loading && saleProducts.some((item) => item.manejaLotes) && availableLots.length === 0 ? <Alert severity="warning">{tsales('missingAvailableLots')}</Alert> : null}

      <Card variant="outlined">
        <CardHeader title={tc('listTitle')} />
        <CardContent sx={{ pt: 0 }}>
          {loading ? (
            <Typography variant="body2" color="text.secondary">{tc('loading')}</Typography>
          ) : (
            <TableContainer>
              <Table size="small">
                <TableHead>
                  <TableRow>
                    <TableCell>{tf('id')}</TableCell>
                    <TableCell>{tf('date')}</TableCell>
                    <TableCell>{tf('customer')}</TableCell>
                    <TableCell>{tf('paymentMethod')}</TableCell>
                    <TableCell>{tf('priceType')}</TableCell>
                    <TableCell>{tf('total')}</TableCell>
                    <TableCell>{tf('totalCost')}</TableCell>
                    <TableCell>{tf('grossMargin')}</TableCell>
                    <TableCell>{tf('profitabilityPercent')}</TableCell>
                    <TableCell>{tf('status')}</TableCell>
                    <TableCell align="right">{tf('actions')}</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {items.map((item) => (
                    <TableRow key={item.ventaId} hover>
                      <TableCell>{item.ventaId}</TableCell>
                      <TableCell>{toInputDate(item.fecha) || '—'}</TableCell>
                      <TableCell>{item.clienteNombre}</TableCell>
                      <TableCell>{item.metodoPagoNombre ?? '—'}</TableCell>
                      <TableCell>{mapPriceTypeLabel(item.tipoPrecio, activeTiposVenta, tsales)}</TableCell>
                      <TableCell>{item.total}</TableCell>
                      <TableCell>{item.costoTotal ?? 0}</TableCell>
                      <TableCell>{item.margenBruto ?? 0}</TableCell>
                      <TableCell>{item.rentabilidadPorcentaje ?? 0}%</TableCell>
                      <TableCell>
                        <Chip size="small" color={item.estado ? 'success' : 'default'} label={mapSaleStatusLabel(item.estado, tsales)} />
                      </TableCell>
                      <TableCell align="right">
                        <Stack direction="row" spacing={1} justifyContent="flex-end">
                          <Button size="small" variant="outlined" onClick={() => void openDetail(item)}>{tc('viewDetails')}</Button>
                          <Button size="small" color="warning" variant="outlined" onClick={() => openAnnul(item)} disabled={!canAnnul || !item.estado}>{tc('annul')}</Button>
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

      <Dialog open={createOpen} onClose={closeCreate} fullWidth maxWidth="md">
        <DialogTitle>{`${tcrud('new')} — ${nav('ventas')}`}</DialogTitle>
        <DialogContent dividers>
          <Stack spacing={2} sx={{ pt: 1 }}>
            {createError ? <Alert severity="error">{createError}</Alert> : null}

            <Card variant="outlined">
              <CardHeader title={`1. ${tf('customer')}`} />
              <CardContent sx={{ pt: 0 }}>
                <Stack spacing={2}>
                  <TextField select fullWidth label={tf('customer')} value={clienteId} onChange={(e) => setClienteId(e.target.value)}>
                    {activeClientes.map((item) => (
                      <MenuItem key={item.clienteId} value={String(item.clienteId)}>{item.nombre}</MenuItem>
                    ))}
                  </TextField>

                  <Stack direction={{ xs: 'column', md: 'row' }} spacing={2}>
                    <TextField select fullWidth label={tf('priceType')} value={tipoPrecio} onChange={(e) => setTipoPrecio(e.target.value)}>
                      {activeTiposVenta.map((item) => (
                        <MenuItem key={item.tipoVentaId} value={item.codigo}>{mapPriceTypeLabel(item.codigo, activeTiposVenta, tsales)}</MenuItem>
                      ))}
                    </TextField>
                    <TextField select fullWidth label={tf('paymentStatus')} value={estadoPago} onChange={(e) => setEstadoPago(e.target.value as (typeof PAYMENT_STATUSES)[number])}>
                      {PAYMENT_STATUSES.map((item) => (
                        <MenuItem key={item} value={item}>{mapPaymentStatusLabel(item, tsales)}</MenuItem>
                      ))}
                    </TextField>
                  </Stack>

                  <TextField select fullWidth label={tf('paymentMethod')} value={metodoPagoId} onChange={(e) => setMetodoPagoId(e.target.value)}>
                    <MenuItem value="">—</MenuItem>
                    {activeMetodosPago.map((item) => (
                      <MenuItem key={item.metodoPagoId} value={String(item.metodoPagoId)}>{item.nombre}</MenuItem>
                    ))}
                  </TextField>

                  <Alert severity="info">
                    {selectedCliente?.nombre ?? '—'} · {selectedTipoVenta ? mapPriceTypeLabel(selectedTipoVenta.codigo, activeTiposVenta, tsales) : '—'} · {mapPaymentStatusLabel(estadoPago, tsales)} · {selectedMetodoPago?.nombre ?? '—'}
                  </Alert>
                </Stack>
              </CardContent>
            </Card>

            <Card variant="outlined">
              <CardHeader title={`2. ${tsales('saleLinesTitle')}`} action={<Button onClick={addLine} disabled={sellableProducts.length === 0}>{tsales('addLine')}</Button>} />
              <CardContent sx={{ pt: 0 }}>
                <Stack spacing={2}>
                  {detalles.map((line, index) => {
                    const producto = findProduct(line.productoId);
                    const lote = findLot(line.loteId);
                    const available = getAvailableForLine(line);
                    const lineValid = isLineValid(line);
                    const presentation = formatPresentationLabel(producto?.presentacionNombre, producto?.presentacionCantidad, producto?.presentacionUnidadMedida);

                    return (
                      <Card key={line.key} variant="outlined">
                        <CardContent>
                          <Stack spacing={2}>
                            <Stack direction="row" alignItems="center" justifyContent="space-between" spacing={2}>
                              <Typography variant="subtitle2">{tsales('lineLabel')} {index + 1}</Typography>
                              <Button color="error" onClick={() => removeLine(line.key)} disabled={detalles.length === 1}>{tc('delete')}</Button>
                            </Stack>

                            <Stack direction={{ xs: 'column', md: 'row' }} spacing={2}>
                              <TextField
                                select
                                fullWidth
                                label={tf('product')}
                                value={line.productoId}
                                onChange={(e) => updateLine(line.key, { productoId: e.target.value, loteId: '' })}
                              >
                                {sellableProducts.map((item) => (
                                  <MenuItem key={item.productoId} value={String(item.productoId)}>{formatProductLabel(item)}</MenuItem>
                                ))}
                              </TextField>
                              <TextField
                                fullWidth
                                label={tf('quantity')}
                                value={line.cantidad}
                                onChange={(e) => updateLine(line.key, { cantidad: e.target.value })}
                                type="number"
                                inputProps={{ min: 0.001, step: producto?.permiteDecimal ? 0.001 : 1 }}
                                error={!lineValid}
                                helperText={producto ? `${tf('availableQuantity')}: ${Math.max(available, 0)} ${producto.unidadMedida}` : '—'}
                              />
                            </Stack>

                            {producto?.manejaLotes ? (
                              <TextField
                                select
                                fullWidth
                                label={tf('lot')}
                                value={line.loteId}
                                onChange={(e) => updateLine(line.key, { loteId: e.target.value })}
                              >
                                {availableLots
                                  .filter((item) => item.productoId === Number(line.productoId))
                                  .map((item) => (
                                    <MenuItem key={item.loteId} value={String(item.loteId)}>
                                      {item.codigoLote} · {item.cantidadDisponible} {item.unidadMedida}
                                    </MenuItem>
                                  ))}
                              </TextField>
                            ) : null}

                            <Alert severity={!producto ? 'info' : lineValid ? 'success' : 'warning'}>
                              <Stack spacing={0.5}>
                                <Typography variant="body2">
                                  {producto ? `${producto.nombre} · ${tf('unitPrice')}: ${resolveDynamicPrice(producto, tipoPrecio)} · ${tf('subtotal')}: ${getLineSubtotal(line)}` : tf('product')}
                                </Typography>
                                {presentation ? <Typography variant="caption">{tf('presentation')}: {presentation}</Typography> : null}
                                {producto?.manejaLotes && lote ? <Typography variant="caption">{tf('batchCode')}: {lote.codigoLote}</Typography> : null}
                                {producto?.manejaLotes && !lote ? <Typography variant="caption">{tsales('selectLotHint')}</Typography> : null}
                              </Stack>
                            </Alert>
                          </Stack>
                        </CardContent>
                      </Card>
                    );
                  })}
                </Stack>
              </CardContent>
            </Card>

            <Card variant="outlined">
              <CardHeader title={`3. ${tc('confirm')}`} />
              <CardContent sx={{ pt: 0 }}>
                <Stack spacing={2}>
                  <Alert severity={invalidLineCount === 0 ? 'success' : 'warning'}>
                    <Stack spacing={0.5}>
                      <Typography variant="body2">{tf('customer')}: {selectedCliente?.nombre ?? '—'} · {tf('total')}: {totalPreview}</Typography>
                      <Typography variant="caption">{tsales('lineLabel')}: {detalles.length} · {tf('lot')}: {linesUsingLots} · {tf('paymentStatus')}: {mapPaymentStatusLabel(estadoPago, tsales)}</Typography>
                      <Typography variant="caption">{invalidLineCount === 0 ? tsales('draftReady') : tsales('draftNeedsReview')}</Typography>
                    </Stack>
                  </Alert>

                  <Stack direction="row" alignItems="center" justifyContent="space-between" spacing={2}>
                    <Typography variant="subtitle1">{tf('total')}</Typography>
                    <Typography variant="h5">{totalPreview}</Typography>
                  </Stack>
                </Stack>
              </CardContent>
            </Card>
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={closeCreate} disabled={createSaving}>{tc('cancel')}</Button>
          <Button onClick={() => void saveCreate()} variant="contained" disabled={createSaving || !canSave}>{tc('save')}</Button>
        </DialogActions>
      </Dialog>

      <Dialog open={detailOpen} onClose={closeDetail} fullWidth maxWidth="lg">
        <DialogTitle>{detail ? `${nav('ventas')} #${detail.venta.ventaId}` : nav('ventas')}</DialogTitle>
        <DialogContent dividers>
          <Stack spacing={2} sx={{ pt: 1 }}>
            {detailError ? <Alert severity="error">{detailError}</Alert> : null}
            {detailLoading ? <Typography variant="body2" color="text.secondary">{tc('loading')}</Typography> : null}

            {detail ? (
              <>
                <Card variant="outlined">
                  <CardContent>
                    <Stack direction={{ xs: 'column', md: 'row' }} spacing={2} justifyContent="space-between">
                      <Stack spacing={0.5}>
                        <Typography variant="h6">{detail.venta.clienteNombre}</Typography>
                        <Typography variant="body2" color="text.secondary">{tf('date')}: {toInputDate(detail.venta.fecha) || '—'}</Typography>
                        <Typography variant="body2" color="text.secondary">{tf('paymentMethod')}: {detail.venta.metodoPagoNombre ?? '—'}</Typography>
                      </Stack>
                      <Stack spacing={0.5} alignItems={{ xs: 'flex-start', md: 'flex-end' }}>
                        <Chip size="small" color={detail.venta.estado ? 'success' : 'default'} label={mapSaleStatusLabel(detail.venta.estado, tsales)} />
                        <Typography variant="body2">{tf('paymentStatus')}: {mapPaymentStatusLabel(detail.venta.estadoPago, tsales)}</Typography>
                        <Typography variant="body2">{tf('priceType')}: {mapPriceTypeLabel(detail.venta.tipoPrecio, activeTiposVenta, tsales)}</Typography>
                        <Typography variant="h6">{detail.venta.total}</Typography>
                      </Stack>
                    </Stack>
                  </CardContent>
                </Card>

                <Card variant="outlined">
                  <CardHeader title={tsales('profitSummaryTitle')} />
                  <CardContent>
                    <Stack direction={{ xs: 'column', md: 'row' }} spacing={2}>
                      <TextField fullWidth label={tf('total')} value={detail.venta.total} InputProps={{ readOnly: true }} />
                      <TextField fullWidth label={tf('totalCost')} value={detail.venta.costoTotal ?? 0} InputProps={{ readOnly: true }} />
                      <TextField fullWidth label={tf('grossMargin')} value={detail.venta.margenBruto ?? 0} InputProps={{ readOnly: true }} />
                      <TextField fullWidth label={tf('profitabilityPercent')} value={`${detail.venta.rentabilidadPorcentaje ?? 0}%`} InputProps={{ readOnly: true }} />
                    </Stack>
                  </CardContent>
                </Card>

                <Card variant="outlined">
                  <CardHeader title={tsales('saleLinesTitle')} />
                  <CardContent sx={{ pt: 0 }}>
                    <TableContainer>
                      <Table size="small">
                        <TableHead>
                          <TableRow>
                            <TableCell>{tf('product')}</TableCell>
                            <TableCell>{tf('presentation')}</TableCell>
                            <TableCell>{tf('lot')}</TableCell>
                            <TableCell>{tf('quantity')}</TableCell>
                            <TableCell>{tf('unitPrice')}</TableCell>
                            <TableCell>{tf('unitCost')}</TableCell>
                            <TableCell>{tf('subtotal')}</TableCell>
                            <TableCell>{tf('totalCost')}</TableCell>
                            <TableCell>{tf('grossMargin')}</TableCell>
                            <TableCell>{tf('profitabilityPercent')}</TableCell>
                          </TableRow>
                        </TableHead>
                        <TableBody>
                          {detail.detalles.map((item) => (
                            <TableRow key={item.detalleId} hover>
                              <TableCell>{item.productoNombre}</TableCell>
                              <TableCell>{formatPresentationLabel(item.presentacionNombre, item.presentacionCantidad, item.presentacionUnidadMedida) ?? '—'}</TableCell>
                              <TableCell>{item.codigoLote ?? '—'}</TableCell>
                              <TableCell>{item.cantidad} {item.unidadMedida}</TableCell>
                              <TableCell>{item.precioUnitario}</TableCell>
                              <TableCell>{item.costoUnitario ?? 0}</TableCell>
                              <TableCell>{item.subtotal}</TableCell>
                              <TableCell>{item.costoTotal ?? 0}</TableCell>
                              <TableCell>{item.margenBruto ?? 0}</TableCell>
                              <TableCell>{item.rentabilidadPorcentaje ?? 0}%</TableCell>
                            </TableRow>
                          ))}
                        </TableBody>
                      </Table>
                    </TableContainer>
                  </CardContent>
                </Card>

                <Card variant="outlined">
                  <CardHeader title={tsales('lotOutputsTitle')} />
                  <CardContent sx={{ pt: 0 }}>
                    {detail.loteMovimientos.length === 0 ? (
                      <Typography variant="body2" color="text.secondary">{tsales('noLotOutputs')}</Typography>
                    ) : (
                      <TableContainer>
                        <Table size="small">
                          <TableHead>
                            <TableRow>
                              <TableCell>{tf('date')}</TableCell>
                              <TableCell>{tf('product')}</TableCell>
                              <TableCell>{tf('lot')}</TableCell>
                              <TableCell>{tf('origin')}</TableCell>
                              <TableCell>{tf('quantity')}</TableCell>
                              <TableCell>{tf('notes')}</TableCell>
                            </TableRow>
                          </TableHead>
                          <TableBody>
                            {detail.loteMovimientos.map((item) => (
                              <TableRow key={item.movimientoId} hover>
                                <TableCell>{toInputDate(item.fecha)}</TableCell>
                                <TableCell>{item.productoNombre}</TableCell>
                                <TableCell>{item.codigoLote}</TableCell>
                                <TableCell>{item.tipo}</TableCell>
                                <TableCell>{item.cantidad} {item.unidadMedida}</TableCell>
                                <TableCell>{item.observacion ?? '—'}</TableCell>
                              </TableRow>
                            ))}
                          </TableBody>
                        </Table>
                      </TableContainer>
                    )}
                  </CardContent>
                </Card>

                {detail.pagos.length > 0 ? (
                  <Card variant="outlined">
                    <CardHeader title={tf('paymentMethod')} />
                    <CardContent sx={{ pt: 0 }}>
                      <TableContainer>
                        <Table size="small">
                          <TableHead>
                            <TableRow>
                              <TableCell>{tf('paymentMethod')}</TableCell>
                              <TableCell>{tf('amount')}</TableCell>
                            </TableRow>
                          </TableHead>
                          <TableBody>
                            {detail.pagos.map((item) => (
                              <TableRow key={item.ventaPagoId} hover>
                                <TableCell>{item.metodoPagoNombre}</TableCell>
                                <TableCell>{item.monto}</TableCell>
                              </TableRow>
                            ))}
                          </TableBody>
                        </Table>
                      </TableContainer>
                    </CardContent>
                  </Card>
                ) : null}
              </>
            ) : null}
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={closeDetail} disabled={detailLoading}>{tc('close')}</Button>
        </DialogActions>
      </Dialog>

      <Dialog open={annulOpen} onClose={closeAnnul} fullWidth maxWidth="xs">
        <DialogTitle>{tc('annul')}</DialogTitle>
        <DialogContent dividers>
          <Stack spacing={2} sx={{ pt: 1 }}>
            {annulError ? <Alert severity="error">{annulError}</Alert> : null}
            <Typography variant="body2">{tsales('confirmAnnulSale')}</Typography>
            {annulTarget ? <Typography variant="body2" color="text.secondary">#{annulTarget.ventaId} · {annulTarget.clienteNombre}</Typography> : null}
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={closeAnnul} disabled={annulSaving}>{tc('cancel')}</Button>
          <Button onClick={() => void confirmAnnul()} color="warning" variant="contained" disabled={annulSaving || !annulTarget || !canAnnul}>{tc('annul')}</Button>
        </DialogActions>
      </Dialog>
    </Stack>
  );
}