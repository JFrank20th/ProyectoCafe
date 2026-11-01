'use client';

import { useEffect, useState } from 'react';

import { useLocale, useTranslations } from '@/hooks/useTranslations';
import { apiFetch } from '@/lib/api/client';
import { type LoteCafeResumen } from '@/lib/inventario/cafe';
import { anulateVenta, cancelVenta, type MovimientoAsociado, type VentaHistorial } from '@/lib/ventas/ventas';

import Alert from '@mui/material/Alert';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import Dialog from '@mui/material/Dialog';
import DialogActions from '@mui/material/DialogActions';
import DialogContent from '@mui/material/DialogContent';
import DialogTitle from '@mui/material/DialogTitle';
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

export type VentaDetalleDialogProps = {
  open: boolean;
  venta: VentaHistorial | null;
  canQueryMovements: boolean;
  canRegisterPayments?: boolean;
  canUpdateDelivery?: boolean;
  canCancelSale?: boolean;
  canAnulateSale?: boolean;
  onUpdated?: () => void;
  onClose: () => void;
};

export function VentaDetalleDialog({
  open,
  venta,
  canQueryMovements,
  canRegisterPayments = false,
  canUpdateDelivery = false,
  canCancelSale = false,
  canAnulateSale = false,
  onUpdated,
  onClose,
}: VentaDetalleDialogProps) {
  const tc = useTranslations('common');
  const tf = useTranslations('fields');
  const ts = useTranslations('sales');
  const locale = useLocale();

  const [ventaActual, setVentaActual] = useState<VentaHistorial | null>(venta);
  const [movimientos, setMovimientos] = useState<MovimientoAsociado[]>([]);
  const [loadingMovimientos, setLoadingMovimientos] = useState(false);
  const [movimientosError, setMovimientosError] = useState<string | null>(null);

  const [paymentMonto, setPaymentMonto] = useState('');
  const [paymentFecha, setPaymentFecha] = useState('');
  const [paymentMetodo, setPaymentMetodo] = useState('Efectivo');
  const [paymentReferencia, setPaymentReferencia] = useState('');
  const [savingPayment, setSavingPayment] = useState(false);
  const [paymentError, setPaymentError] = useState<string | null>(null);
  const [paymentSuccess, setPaymentSuccess] = useState<string | null>(null);

  const [estadoEntrega, setEstadoEntrega] = useState<number>(venta?.estadoEntrega ?? 1);
  const [savingDelivery, setSavingDelivery] = useState(false);
  const [deliveryError, setDeliveryError] = useState<string | null>(null);
  const [deliverySuccess, setDeliverySuccess] = useState<string | null>(null);

  const [cancelMotivo, setCancelMotivo] = useState('');
  const [anulateMotivo, setAnulateMotivo] = useState('');
  const [savingCancel, setSavingCancel] = useState(false);
  const [savingAnulate, setSavingAnulate] = useState(false);
  const [cancelError, setCancelError] = useState<string | null>(null);
  const [cancelSuccess, setCancelSuccess] = useState<string | null>(null);
  const [anulateError, setAnulateError] = useState<string | null>(null);
  const [anulateSuccess, setAnulateSuccess] = useState<string | null>(null);

  useEffect(() => {
    setVentaActual(venta);
    setEstadoEntrega(venta?.estadoEntrega ?? 1);
    setPaymentMonto('');
    setPaymentReferencia('');
    setPaymentError(null);
    setPaymentSuccess(null);
    setDeliveryError(null);
    setDeliverySuccess(null);
    setCancelMotivo('');
    setAnulateMotivo('');
    setCancelError(null);
    setCancelSuccess(null);
    setAnulateError(null);
    setAnulateSuccess(null);
  }, [venta, open]);

  useEffect(() => {
    if (!open || !ventaActual || !canQueryMovements) {
      setMovimientos([]);
      setMovimientosError(null);
      return;
    }

    let cancelled = false;
    setLoadingMovimientos(true);
    setMovimientosError(null);

    void (async () => {
      try {
        const lotes = await apiFetch<LoteCafeResumen[]>('/api/v1/inventario/lotes');
        const responses = await Promise.all(
          lotes.map(async (lote) => {
            type InventarioMovimientoResponse = {
              movimientoLoteCafeId: number;
              tipoMovimiento: number;
              cantidadPaquetes: number;
              fechaMovimiento: string;
              referencia?: string | null;
            };

            const list = await apiFetch<InventarioMovimientoResponse[]>(
              `/api/v1/inventario/lotes/${lote.loteCafeId}/movimientos`,
            );
            return list.map((movimiento) => ({
              movimientoLoteCafeId: movimiento.movimientoLoteCafeId,
              loteCodigo: lote.codigoLote,
              tipoMovimiento: movimiento.tipoMovimiento,
              cantidadPaquetes: movimiento.cantidadPaquetes,
              fechaMovimiento: movimiento.fechaMovimiento,
              referencia: movimiento.referencia,
            }));
          }),
        );

        if (cancelled) return;

        const asociados = responses
          .flat()
          .filter((movimiento) => movimiento.referencia === `VENTA-${ventaActual.numeroVenta}`);

        setMovimientos(asociados);
      } catch (requestError) {
        if (!cancelled) {
          setMovimientosError(
            requestError instanceof Error ? requestError.message : ts('errorLoadingMovements'),
          );
        }
      } finally {
        if (!cancelled) setLoadingMovimientos(false);
      }
    })();

    return () => {
      cancelled = true;
    };
  }, [open, ventaActual, canQueryMovements, ts]);

  if (!ventaActual) return null;

  const isCancelledOrAnnulled = ventaActual.estado === 2 || ventaActual.estado === 3;
  const paymentDisabled = savingPayment || isCancelledOrAnnulled || ventaActual.saldoPendiente <= 0;

  async function refreshVenta() {
    if (!ventaActual) return;
    try {
      const updated = await apiFetch<VentaHistorial>(`/api/v1/ventas/ventas/${ventaActual.ventaId}`);
      setVentaActual(updated);
      setEstadoEntrega(updated.estadoEntrega);
    } catch (requestError) {
      setPaymentError(requestError instanceof Error ? requestError.message : ts('ventaRegisterError'));
    }
  }

  async function submitPayment() {
    if (!ventaActual || paymentDisabled) return;
    setPaymentError(null);
    setPaymentSuccess(null);

    const monto = Number(paymentMonto.trim());
    if (!Number.isFinite(monto) || monto <= 0) {
      setPaymentError(ts('paymentAmountInvalid'));
      return;
    }
    if (monto > ventaActual.saldoPendiente) {
      setPaymentError(ts('paymentExceedsBalance'));
      return;
    }

    setSavingPayment(true);
    try {
      await apiFetch(`/api/v1/ventas/ventas/${ventaActual.ventaId}/pagos`, {
        method: 'POST',
        body: JSON.stringify({
          monto,
          fechaPago: paymentFecha ? new Date(paymentFecha).toISOString() : null,
          metodoPago: paymentMetodo,
          referencia: paymentReferencia.trim() || null,
        }),
      });
      setPaymentSuccess(ts('paymentRegisteredSuccess'));
      setPaymentMonto('');
      setPaymentReferencia('');
      await refreshVenta();
    } catch (requestError) {
      setPaymentError(requestError instanceof Error ? requestError.message : ts('ventaRegisterError'));
    } finally {
      setSavingPayment(false);
    }
  }

  async function submitDelivery() {
    if (!ventaActual || savingDelivery) return;
    setDeliveryError(null);
    setDeliverySuccess(null);
    setSavingDelivery(true);
    try {
      await apiFetch(`/api/v1/ventas/ventas/${ventaActual.ventaId}/estado-entrega`, {
        method: 'PUT',
        body: JSON.stringify({ estadoEntrega: Number(estadoEntrega) }),
      });
      setDeliverySuccess(ts('deliveryUpdatedSuccess'));
      await refreshVenta();
    } catch (requestError) {
      setDeliveryError(requestError instanceof Error ? requestError.message : ts('ventaRegisterError'));
    } finally {
      setSavingDelivery(false);
    }
  }

  async function submitCancel() {
    if (!ventaActual || savingCancel) return;
    setCancelError(null);
    setCancelSuccess(null);
    if (!cancelMotivo.trim()) {
      setCancelError(ts('reasonRequired'));
      return;
    }
    if (cancelMotivo.trim().length > 250) {
      setCancelError(ts('reasonTooLong'));
      return;
    }
    if (!window.confirm(ts('confirmCancelSale'))) return;
    setSavingCancel(true);
    try {
      await cancelVenta(ventaActual.ventaId, cancelMotivo.trim());
      setCancelSuccess(ts('saleCancelled'));
      setCancelMotivo('');
      await refreshVenta();
      onUpdated?.();
    } catch (requestError) {
      setCancelError(requestError instanceof Error ? requestError.message : ts('cancelSaleError'));
    } finally {
      setSavingCancel(false);
    }
  }

  async function submitAnulate() {
    if (!ventaActual || savingAnulate) return;
    setAnulateError(null);
    setAnulateSuccess(null);
    if (!anulateMotivo.trim()) {
      setAnulateError(ts('reasonRequired'));
      return;
    }
    if (anulateMotivo.trim().length > 250) {
      setAnulateError(ts('reasonTooLong'));
      return;
    }
    if (!window.confirm(ts('confirmAnulateSale'))) return;
    setSavingAnulate(true);
    try {
      await anulateVenta(ventaActual.ventaId, anulateMotivo.trim());
      setAnulateSuccess(ts('saleAnnulled'));
      setAnulateMotivo('');
      await refreshVenta();
      onUpdated?.();
    } catch (requestError) {
      setAnulateError(requestError instanceof Error ? requestError.message : ts('anulateSaleError'));
    } finally {
      setSavingAnulate(false);
    }
  }

  function estadoPagoLabel(value: number) {
    if (value === 2) return ts('paymentStatusPartial');
    if (value === 3) return ts('paymentStatusPaid');
    return ts('paymentStatusPending');
  }

  function estadoEntregaLabel(value: number) {
    if (value === 2) return ts('deliveryStatusInPreparation');
    if (value === 3) return ts('deliveryStatusReadyToDeliver');
    if (value === 4) return ts('deliveryStatusDelivered');
    return ts('deliveryStatusPendingPreparation');
  }

  const estadoLabel =
    ventaActual.estado === 1
      ? ts('statusRegistered')
      : ventaActual.estado === 2
        ? ts('statusCancelled')
        : ts('statusAnnulled');

  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="md">
      <DialogTitle>{ts('ventaDetailTitle', { number: ventaActual.numeroVenta })}</DialogTitle>
      <DialogContent dividers>
        <Stack spacing={2} sx={{ pt: 1 }}>
          <Stack direction={{ xs: 'column', sm: 'row' }} spacing={3} useFlexGap flexWrap="wrap">
            <BoxInfo label={ts('saleNumberLabel')} value={String(ventaActual.numeroVenta)} />
            <BoxInfo label={tf('date')} value={new Date(ventaActual.fechaVenta).toLocaleString(locale)} />
            <BoxInfo label={ts('clientLabel')} value={ventaActual.clienteNombre || ts('noCliente')} />
            <BoxInfo label={tf('status')} value={estadoLabel} />
            <BoxInfo
              label={ts('totalLabel')}
              value={new Intl.NumberFormat(locale, {
                style: 'currency',
                currency: 'COP',
                maximumFractionDigits: 0,
              }).format(ventaActual.total)}
            />
          </Stack>

          {ventaActual.observacion ? <Alert severity="info">{ventaActual.observacion}</Alert> : null}

          <Typography variant="subtitle2">{ts('financialSummaryTitle')}</Typography>
          <Stack direction={{ xs: 'column', sm: 'row' }} spacing={3} useFlexGap flexWrap="wrap">
            <BoxInfo
              label={ts('totalPaidLabel')}
              value={new Intl.NumberFormat(locale, {
                style: 'currency',
                currency: 'COP',
                maximumFractionDigits: 0,
              }).format(ventaActual.montoPagado)}
            />
            <BoxInfo
              label={ts('pendingBalanceLabel')}
              value={new Intl.NumberFormat(locale, {
                style: 'currency',
                currency: 'COP',
                maximumFractionDigits: 0,
              }).format(ventaActual.saldoPendiente)}
            />
            <BoxInfo label={ts('paymentStatusLabel')} value={estadoPagoLabel(ventaActual.estadoPago)} />
            <BoxInfo label={ts('deliveryStatusLabel')} value={estadoEntregaLabel(ventaActual.estadoEntrega)} />
          </Stack>

          {ventaActual.estado === 2 && ventaActual.motivoCancelacion ? (
            <Alert severity="warning">
              {`${ts('statusCancelled')}${ventaActual.canceladoEn ? ` ${new Date(ventaActual.canceladoEn).toLocaleString(locale)}` : ''}: ${ventaActual.motivoCancelacion}`}
            </Alert>
          ) : null}
          {ventaActual.estado === 3 && ventaActual.motivoAnulacion ? (
            <Alert severity="error">
              {`${ts('statusAnnulled')}${ventaActual.anuladoEn ? ` ${new Date(ventaActual.anuladoEn).toLocaleString(locale)}` : ''}: ${ventaActual.motivoAnulacion}`}
            </Alert>
          ) : null}

          {canCancelSale || canAnulateSale ? (
            ventaActual.estado === 1 && ventaActual.estadoEntrega !== 4 ? (
              <>
                <Typography variant="subtitle2">Acciones de venta</Typography>
                {cancelError ? <Alert severity="error">{cancelError}</Alert> : null}
                {cancelSuccess ? <Alert severity="success">{cancelSuccess}</Alert> : null}
                {anulateError ? <Alert severity="error">{anulateError}</Alert> : null}
                {anulateSuccess ? <Alert severity="success">{anulateSuccess}</Alert> : null}
                <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} useFlexGap flexWrap="wrap">
                  {canCancelSale ? (
                    <Stack spacing={1} sx={{ minWidth: 260 }}>
                      <TextField
                        size="small"
                        label={ts('cancelReason')}
                        value={cancelMotivo}
                        onChange={(event) => setCancelMotivo(event.target.value)}
                        disabled={savingCancel || savingAnulate}
                        inputProps={{ maxLength: 250 }}
                        helperText={`${cancelMotivo.length}/250`}
                      />
                      <Button
                        color="warning"
                        variant="outlined"
                        onClick={() => void submitCancel()}
                        disabled={savingCancel || savingAnulate}
                      >
                        {savingCancel ? tc('saving') : ts('cancelSale')}
                      </Button>
                    </Stack>
                  ) : null}
                  {canAnulateSale ? (
                    <Stack spacing={1} sx={{ minWidth: 260 }}>
                      <TextField
                        size="small"
                        label={ts('anulateReason')}
                        value={anulateMotivo}
                        onChange={(event) => setAnulateMotivo(event.target.value)}
                        disabled={savingCancel || savingAnulate}
                        inputProps={{ maxLength: 250 }}
                        helperText={`${anulateMotivo.length}/250`}
                      />
                      <Button
                        color="error"
                        variant="outlined"
                        onClick={() => void submitAnulate()}
                        disabled={savingCancel || savingAnulate}
                      >
                        {savingAnulate ? tc('saving') : ts('anulateSale')}
                      </Button>
                    </Stack>
                  ) : null}
                </Stack>
              </>
            ) : null
          ) : null}

          <Typography variant="subtitle2">{ts('productsTitle')}</Typography>
          <TableContainer>
            <Table size="small" sx={{ minWidth: 720 }}>
              <TableHead>
                <TableRow>
                  <TableCell>{tf('presentation')}</TableCell>
                  <TableCell>{tf('coffeeType')}</TableCell>
                  <TableCell align="right">{ts('quantityLabel')}</TableCell>
                  <TableCell align="right">{ts('unitPriceLabel')}</TableCell>
                  <TableCell align="right">{ts('subtotalLabel')}</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {ventaActual.detalles.map((detalle) => (
                  <TableRow key={detalle.ventaDetalleId} hover>
                    <TableCell>
                      <Stack spacing={0.25}>
                        <Typography variant="body2">{detalle.presentacionNombre}</Typography>
                        <Typography variant="caption" color="text.secondary">
                          {detalle.presentacionCodigo}
                        </Typography>
                      </Stack>
                    </TableCell>
                    <TableCell>
                      <Stack spacing={0.25}>
                        <Typography variant="body2">{detalle.tipoCafeNombre}</Typography>
                        <Typography variant="caption" color="text.secondary">
                          {detalle.tipoCafeCodigo}
                        </Typography>
                      </Stack>
                    </TableCell>
                    <TableCell align="right">{detalle.cantidadPaquetes}</TableCell>
                    <TableCell align="right">
                      {new Intl.NumberFormat(locale, {
                        style: 'currency',
                        currency: 'COP',
                        maximumFractionDigits: 0,
                      }).format(detalle.precioUnitario)}
                    </TableCell>
                    <TableCell align="right">
                      {new Intl.NumberFormat(locale, {
                        style: 'currency',
                        currency: 'COP',
                        maximumFractionDigits: 0,
                      }).format(detalle.subtotal)}
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>

          {canRegisterPayments && !isCancelledOrAnnulled ? (
            <>
              <Typography variant="subtitle2">{ts('registerPayment')}</Typography>
              {paymentError ? <Alert severity="error">{paymentError}</Alert> : null}
              {paymentSuccess ? <Alert severity="success">{paymentSuccess}</Alert> : null}
              {ventaActual.saldoPendiente <= 0 ? (
                <Alert severity="info">{ts('paymentAlreadyPaid')}</Alert>
              ) : (
                <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} useFlexGap flexWrap="wrap">
                  <TextField
                    size="small"
                    type="number"
                    label={ts('amountLabel')}
                    value={paymentMonto}
                    onChange={(event) => setPaymentMonto(event.target.value)}
                    disabled={paymentDisabled}
                    sx={{ minWidth: 140 }}
                  />
                  <TextField
                    size="small"
                    type="date"
                    label={tf('date')}
                    value={paymentFecha}
                    onChange={(event) => setPaymentFecha(event.target.value)}
                    slotProps={{ inputLabel: { shrink: true } }}
                    sx={{ minWidth: 160 }}
                  />
                  <FormControl size="small" sx={{ minWidth: 180 }}>
                    <InputLabel id="metodo-pago-label">{ts('paymentMethodLabel')}</InputLabel>
                    <Select
                      labelId="metodo-pago-label"
                      label={ts('paymentMethodLabel')}
                      value={paymentMetodo}
                      onChange={(event: SelectChangeEvent) => setPaymentMetodo(event.target.value)}
                    >
                      <MenuItem value="Efectivo">Efectivo</MenuItem>
                      <MenuItem value="Transferencia">Transferencia</MenuItem>
                      <MenuItem value="Consignación">Consignación</MenuItem>
                      <MenuItem value="Otro">Otro</MenuItem>
                    </Select>
                  </FormControl>
                  <TextField
                    size="small"
                    label={tf('reference')}
                    value={paymentReferencia}
                    onChange={(event) => setPaymentReferencia(event.target.value)}
                    sx={{ minWidth: 180 }}
                  />
                  <Button
                    variant="contained"
                    onClick={() => void submitPayment()}
                    disabled={paymentDisabled || savingPayment}
                  >
                    {savingPayment ? tc('saving') : ts('registerPayment')}
                  </Button>
                </Stack>
              )}
            </>
          ) : null}

          <Typography variant="subtitle2">{ts('paymentHistoryTitle')}</Typography>
          {ventaActual.pagos.length === 0 ? (
            <Alert severity="info">{ts('noPaymentsRegistered')}</Alert>
          ) : (
            <TableContainer>
              <Table size="small" sx={{ minWidth: 640 }}>
                <TableHead>
                  <TableRow>
                    <TableCell>{tf('date')}</TableCell>
                    <TableCell>{ts('paymentMethodLabel')}</TableCell>
                    <TableCell>{tf('reference')}</TableCell>
                    <TableCell align="right">{ts('amountLabel')}</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {ventaActual.pagos.map((pago) => (
                    <TableRow key={pago.pagoVentaId} hover>
                      <TableCell>{new Date(pago.fechaPago).toLocaleString(locale)}</TableCell>
                      <TableCell>{pago.metodoPago}</TableCell>
                      <TableCell>{pago.referencia || '-'}</TableCell>
                      <TableCell align="right">
                        {new Intl.NumberFormat(locale, {
                          style: 'currency',
                          currency: 'COP',
                          maximumFractionDigits: 0,
                        }).format(pago.monto)}
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </TableContainer>
          )}

          {canUpdateDelivery ? (
            <>
              <Typography variant="subtitle2">{ts('deliveryStatusLabel')}</Typography>
              {deliveryError ? <Alert severity="error">{deliveryError}</Alert> : null}
              {deliverySuccess ? <Alert severity="success">{deliverySuccess}</Alert> : null}
              <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} useFlexGap flexWrap="wrap">
                <FormControl size="small" sx={{ minWidth: 220 }}>
                  <InputLabel id="estado-entrega-label">{ts('deliveryStatusLabel')}</InputLabel>
                  <Select
                    labelId="estado-entrega-label"
                    label={ts('deliveryStatusLabel')}
                    value={String(estadoEntrega)}
                    onChange={(event: SelectChangeEvent) => setEstadoEntrega(Number(event.target.value))}
                    disabled={savingDelivery || isCancelledOrAnnulled}
                  >
                    <MenuItem value="1">{ts('deliveryStatusPendingPreparation')}</MenuItem>
                    <MenuItem value="2">{ts('deliveryStatusInPreparation')}</MenuItem>
                    <MenuItem value="3">{ts('deliveryStatusReadyToDeliver')}</MenuItem>
                    <MenuItem value="4">{ts('deliveryStatusDelivered')}</MenuItem>
                  </Select>
                </FormControl>
                <Button
                  variant="contained"
                  onClick={() => void submitDelivery()}
                  disabled={savingDelivery || isCancelledOrAnnulled}
                >
                  {savingDelivery ? tc('saving') : ts('saveDeliveryButton')}
                </Button>
              </Stack>
            </>
          ) : null}

          <Typography variant="subtitle2">{ts('associatedMovementsTitle')}</Typography>
          {!canQueryMovements ? (
            <Alert severity="info">{ts('movementsNoPermission')}</Alert>
          ) : loadingMovimientos ? (
            <Typography variant="body2" color="text.secondary">
              {tc('loading')}
            </Typography>
          ) : movimientosError ? (
            <Alert severity="error">{movimientosError}</Alert>
          ) : movimientos.length === 0 ? (
            <Alert severity="info">{ts('noAssociatedMovements')}</Alert>
          ) : (
            <TableContainer>
              <Table size="small" sx={{ minWidth: 640 }}>
                <TableHead>
                  <TableRow>
                    <TableCell>{tf('lotCode')}</TableCell>
                    <TableCell>{ts('movementTypeLabel')}</TableCell>
                    <TableCell align="right">{ts('quantityLabel')}</TableCell>
                    <TableCell>{tf('date')}</TableCell>
                    <TableCell>{ts('referenceLabel')}</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {movimientos.map((movimiento) => (
                    <TableRow key={movimiento.movimientoLoteCafeId} hover>
                      <TableCell>{movimiento.loteCodigo}</TableCell>
                      <TableCell>
                        {movimiento.tipoMovimiento === 2 ? ts('movementOut') : movimiento.tipoMovimiento}
                      </TableCell>
                      <TableCell align="right">{movimiento.cantidadPaquetes}</TableCell>
                      <TableCell>{new Date(movimiento.fechaMovimiento).toLocaleString(locale)}</TableCell>
                      <TableCell>{movimiento.referencia || '-'}</TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </TableContainer>
          )}
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>{ts('closeLabel')}</Button>
      </DialogActions>
    </Dialog>
  );
}

function BoxInfo({ label, value }: { label: string; value: string }) {
  return (
    <Box sx={{ minWidth: 140 }}>
      <Typography variant="caption" color="text.secondary">
        {label}
      </Typography>
      <Typography variant="body2" sx={{ mt: 0.5 }}>
        {value}
      </Typography>
    </Box>
  );
}