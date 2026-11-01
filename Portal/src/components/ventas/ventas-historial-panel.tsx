'use client';

import { useEffect, useState } from 'react';

import { useLocale, useTranslations } from '@/hooks/useTranslations';
import { apiFetch } from '@/lib/api/client';
import { type Cliente } from '@/lib/ventas/clientes';
import { type VentaHistorial } from '@/lib/ventas/ventas';

import Alert from '@mui/material/Alert';
import Button from '@mui/material/Button';
import Card from '@mui/material/Card';
import Chip from '@mui/material/Chip';
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
import TextField from '@mui/material/TextField';
import Typography from '@mui/material/Typography';

import { VentaDetalleDialog } from './venta-detalle-dialog';

export type VentasHistorialPanelProps = {
  canView: boolean;
  canQueryMovements: boolean;
  canRegisterPayments?: boolean;
  canUpdateDelivery?: boolean;
  canCancelSale?: boolean;
  canAnulateSale?: boolean;
};

export function VentasHistorialPanel({
  canView,
  canQueryMovements,
  canRegisterPayments = false,
  canUpdateDelivery = false,
  canCancelSale = false,
  canAnulateSale = false,
}: VentasHistorialPanelProps) {
  const tc = useTranslations('common');
  const tf = useTranslations('fields');
  const ts = useTranslations('sales');
  const locale = useLocale();

  const [items, setItems] = useState<VentaHistorial[]>([]);
  const [clientes, setClientes] = useState<Cliente[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [numeroFiltro, setNumeroFiltro] = useState('');
  const [clienteFiltro, setClienteFiltro] = useState<string>('all');
  const [estadoFiltro, setEstadoFiltro] = useState<string>('all');
  const [estadoPagoFiltro, setEstadoPagoFiltro] = useState<string>('all');
  const [estadoEntregaFiltro, setEstadoEntregaFiltro] = useState<string>('all');
  const [fechaDesde, setFechaDesde] = useState('');
  const [fechaHasta, setFechaHasta] = useState('');

  const [selectedVenta, setSelectedVenta] = useState<VentaHistorial | null>(null);
  const [detailOpen, setDetailOpen] = useState(false);

  async function loadVentas() {
    if (!canView) return;
    setLoading(true);
    setError(null);
    try {
      const params = new URLSearchParams();
      if (numeroFiltro.trim()) params.set('numeroVenta', numeroFiltro.trim());
      if (clienteFiltro !== 'all') params.set('clienteId', clienteFiltro);
      if (estadoFiltro !== 'all') params.set('estado', estadoFiltro);
      if (estadoPagoFiltro !== 'all') params.set('estadoPago', estadoPagoFiltro);
      if (estadoEntregaFiltro !== 'all') params.set('estadoEntrega', estadoEntregaFiltro);
      if (fechaDesde) params.set('fechaDesde', fechaDesde);
      if (fechaHasta) params.set('fechaHasta', fechaHasta);
      const query = params.toString();
      const url = query ? `/api/v1/ventas/ventas?${query}` : '/api/v1/ventas/ventas';
      const list = await apiFetch<VentaHistorial[]>(url);
      setItems(list);
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : ts('errorLoadingVentas'));
    } finally {
      setLoading(false);
    }
  }

  async function loadClientes() {
    try {
      const list = await apiFetch<Cliente[]>('/api/v1/ventas/clientes');
      setClientes(list);
    } catch {
      // Opcional para el filtro.
    }
  }

  useEffect(() => {
    void loadVentas();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [canView, numeroFiltro, clienteFiltro, estadoFiltro, estadoPagoFiltro, estadoEntregaFiltro, fechaDesde, fechaHasta]);

  useEffect(() => {
    void loadClientes();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [canView]);

  const filteredItems = items;

  function estadoLabel(estado: number) {
    if (estado === 1) return ts('statusRegistered');
    if (estado === 2) return ts('statusCancelled');
    return ts('statusAnnulled');
  }

  function estadoPagoLabel(estado: number) {
    if (estado === 2) return ts('paymentStatusPartial');
    if (estado === 3) return ts('paymentStatusPaid');
    return ts('paymentStatusPending');
  }

  function estadoEntregaLabel(estado: number) {
    if (estado === 2) return ts('deliveryStatusInPreparation');
    if (estado === 3) return ts('deliveryStatusReadyToDeliver');
    if (estado === 4) return ts('deliveryStatusDelivered');
    return ts('deliveryStatusPendingPreparation');
  }

  function openDetail(venta: VentaHistorial) {
    setSelectedVenta(venta);
    setDetailOpen(true);
  }

  if (!canView) {
    return <Alert severity="info">{ts('ventasHistoryNoPermission')}</Alert>;
  }

  return (
    <Card variant="outlined">
      <CardHeader
        title={ts('salesHistoryTitle')}
        subheader={ts('salesHistoryCount', { count: filteredItems.length })}
        action={
          <Button variant="outlined" size="small" onClick={() => void loadVentas()} disabled={loading}>
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
            <TextField
              size="small"
              label={ts('saleNumberFilter')}
              value={numeroFiltro}
              onChange={(event) => setNumeroFiltro(event.target.value)}
              sx={{ minWidth: { xs: '100%', md: 160 } }}
            />

            <FormControl size="small" sx={{ minWidth: { xs: '100%', md: 200 } }}>
              <InputLabel id="historial-cliente-label">{ts('clientLabel')}</InputLabel>
              <Select
                labelId="historial-cliente-label"
                label={ts('clientLabel')}
                value={clienteFiltro}
                onChange={(event: SelectChangeEvent) => setClienteFiltro(event.target.value)}
              >
                <MenuItem value="all">{ts('allClients')}</MenuItem>
                {clientes.map((cliente) => (
                  <MenuItem key={cliente.clienteId} value={String(cliente.clienteId)}>
                    {cliente.nombre}
                  </MenuItem>
                ))}
              </Select>
            </FormControl>

            <FormControl size="small" sx={{ minWidth: { xs: '100%', md: 160 } }}>
              <InputLabel id="historial-estado-label">{tf('status')}</InputLabel>
              <Select
                labelId="historial-estado-label"
                label={tf('status')}
                value={estadoFiltro}
                onChange={(event: SelectChangeEvent) => setEstadoFiltro(event.target.value)}
              >
                <MenuItem value="all">{ts('allStatuses')}</MenuItem>
                <MenuItem value="1">{ts('statusRegistered')}</MenuItem>
                <MenuItem value="2">{ts('statusCancelled')}</MenuItem>
                <MenuItem value="3">{ts('statusAnnulled')}</MenuItem>
              </Select>
            </FormControl>

            <FormControl size="small" sx={{ minWidth: { xs: '100%', md: 180 } }}>
              <InputLabel id="historial-estado-pago-label">{ts('paymentStatusLabel')}</InputLabel>
              <Select
                labelId="historial-estado-pago-label"
                label={ts('paymentStatusLabel')}
                value={estadoPagoFiltro}
                onChange={(event: SelectChangeEvent) => setEstadoPagoFiltro(event.target.value)}
              >
                <MenuItem value="all">{ts('allOption')}</MenuItem>
                <MenuItem value="1">{ts('paymentStatusPending')}</MenuItem>
                <MenuItem value="2">{ts('paymentStatusPartial')}</MenuItem>
                <MenuItem value="3">{ts('paymentStatusPaid')}</MenuItem>
              </Select>
            </FormControl>

            <FormControl size="small" sx={{ minWidth: { xs: '100%', md: 200 } }}>
              <InputLabel id="historial-estado-entrega-label">{ts('deliveryStatusLabel')}</InputLabel>
              <Select
                labelId="historial-estado-entrega-label"
                label={ts('deliveryStatusLabel')}
                value={estadoEntregaFiltro}
                onChange={(event: SelectChangeEvent) => setEstadoEntregaFiltro(event.target.value)}
              >
                <MenuItem value="all">{ts('allOption')}</MenuItem>
                <MenuItem value="1">{ts('deliveryStatusPendingPreparation')}</MenuItem>
                <MenuItem value="2">{ts('deliveryStatusInPreparation')}</MenuItem>
                <MenuItem value="3">{ts('deliveryStatusReadyToDeliver')}</MenuItem>
                <MenuItem value="4">{ts('deliveryStatusDelivered')}</MenuItem>
              </Select>
            </FormControl>

            <TextField
              size="small"
              type="date"
              label={ts('dateFrom')}
              value={fechaDesde}
              onChange={(event) => setFechaDesde(event.target.value)}
              slotProps={{ inputLabel: { shrink: true } }}
              sx={{ minWidth: { xs: '100%', md: 170 } }}
            />

            <TextField
              size="small"
              type="date"
              label={ts('dateTo')}
              value={fechaHasta}
              onChange={(event) => setFechaHasta(event.target.value)}
              slotProps={{ inputLabel: { shrink: true } }}
              sx={{ minWidth: { xs: '100%', md: 170 } }}
            />

            <Button
              variant="text"
              onClick={() => {
                setNumeroFiltro('');
                setClienteFiltro('all');
                setEstadoFiltro('all');
                setEstadoPagoFiltro('all');
                setEstadoEntregaFiltro('all');
                setFechaDesde('');
                setFechaHasta('');
              }}
            >
              {ts('clearFiltersLabel')}
            </Button>
          </Stack>

          {loading ? (
            <Typography variant="body2" color="text.secondary">
              {tc('loading')}
            </Typography>
          ) : filteredItems.length === 0 ? (
            <Alert severity="info">{ts('noSalesHistory')}</Alert>
          ) : (
            <TableContainer>
              <Table size="small" sx={{ minWidth: 1180 }}>
                <TableHead>
                  <TableRow>
                    <TableCell>{ts('saleNumberLabel')}</TableCell>
                    <TableCell>{tf('date')}</TableCell>
                    <TableCell>{ts('clientLabel')}</TableCell>
                    <TableCell align="right">{ts('productsCountLabel')}</TableCell>
                    <TableCell align="right">{ts('totalLabel')}</TableCell>
                    <TableCell align="right">{ts('paidLabel')}</TableCell>
                    <TableCell align="right">{ts('balanceLabel')}</TableCell>
                    <TableCell>{tf('status')}</TableCell>
                    <TableCell>{ts('paymentStatusLabel')}</TableCell>
                    <TableCell>{ts('deliveryStatusLabel')}</TableCell>
                    <TableCell align="right">{tf('actions')}</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {filteredItems.map((venta) => (
                    <TableRow key={venta.ventaId} hover>
                      <TableCell>{venta.numeroVenta}</TableCell>
                      <TableCell>{new Date(venta.fechaVenta).toLocaleString(locale)}</TableCell>
                      <TableCell>{venta.clienteNombre || ts('noCliente')}</TableCell>
                      <TableCell align="right">{venta.detalles.length}</TableCell>
                      <TableCell align="right">
                        {new Intl.NumberFormat(locale, {
                          style: 'currency',
                          currency: 'COP',
                          maximumFractionDigits: 0,
                        }).format(venta.total)}
                      </TableCell>
                      <TableCell align="right">
                        {new Intl.NumberFormat(locale, {
                          style: 'currency',
                          currency: 'COP',
                          maximumFractionDigits: 0,
                        }).format(venta.montoPagado)}
                      </TableCell>
                      <TableCell align="right">
                        {new Intl.NumberFormat(locale, {
                          style: 'currency',
                          currency: 'COP',
                          maximumFractionDigits: 0,
                        }).format(venta.saldoPendiente)}
                      </TableCell>
                      <TableCell>
                        <Chip
                          size="small"
                          color={venta.estado === 1 ? 'success' : 'default'}
                          label={estadoLabel(venta.estado)}
                        />
                      </TableCell>
                      <TableCell>{estadoPagoLabel(venta.estadoPago)}</TableCell>
                      <TableCell>{estadoEntregaLabel(venta.estadoEntrega)}</TableCell>
                      <TableCell align="right">
                        <Button size="small" variant="outlined" onClick={() => openDetail(venta)}>
                          {ts('viewDetailLabel')}
                        </Button>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </TableContainer>
          )}
        </Stack>
      </CardContent>

      <VentaDetalleDialog
        open={detailOpen}
        venta={selectedVenta}
        canQueryMovements={canQueryMovements}
        canRegisterPayments={canRegisterPayments}
        canUpdateDelivery={canUpdateDelivery}
        canCancelSale={canCancelSale}
        canAnulateSale={canAnulateSale}
        onUpdated={() => void loadVentas()}
        onClose={() => setDetailOpen(false)}
      />
    </Card>
  );
}