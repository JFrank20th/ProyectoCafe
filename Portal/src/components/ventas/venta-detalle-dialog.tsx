'use client';

import { useEffect, useState } from 'react';

import { useLocale, useTranslations } from '@/hooks/useTranslations';
import { apiFetch } from '@/lib/api/client';
import { type LoteCafeResumen } from '@/lib/inventario/cafe';
import { type MovimientoAsociado, type VentaHistorial } from '@/lib/ventas/ventas';

import Alert from '@mui/material/Alert';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import Dialog from '@mui/material/Dialog';
import DialogActions from '@mui/material/DialogActions';
import DialogContent from '@mui/material/DialogContent';
import DialogTitle from '@mui/material/DialogTitle';
import Stack from '@mui/material/Stack';
import Table from '@mui/material/Table';
import TableBody from '@mui/material/TableBody';
import TableCell from '@mui/material/TableCell';
import TableContainer from '@mui/material/TableContainer';
import TableHead from '@mui/material/TableHead';
import TableRow from '@mui/material/TableRow';
import Typography from '@mui/material/Typography';

export type VentaDetalleDialogProps = {
  open: boolean;
  venta: VentaHistorial | null;
  canQueryMovements: boolean;
  onClose: () => void;
};

export function VentaDetalleDialog({
  open,
  venta,
  canQueryMovements,
  onClose,
}: VentaDetalleDialogProps) {
  const tc = useTranslations('common');
  const tf = useTranslations('fields');
  const ts = useTranslations('sales');
  const locale = useLocale();

  const [movimientos, setMovimientos] = useState<MovimientoAsociado[]>([]);
  const [loadingMovimientos, setLoadingMovimientos] = useState(false);
  const [movimientosError, setMovimientosError] = useState<string | null>(null);

  useEffect(() => {
    if (!open || !venta || !canQueryMovements) {
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
          .filter((movimiento) => movimiento.referencia === `VENTA-${venta.numeroVenta}`);

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
  }, [open, venta, canQueryMovements, ts]);

  if (!venta) return null;

  const estadoLabel =
    venta.estado === 1 ? ts('statusRegistered') : venta.estado === 2 ? ts('statusCancelled') : ts('statusAnnulled');

  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="md">
      <DialogTitle>{ts('ventaDetailTitle', { number: venta.numeroVenta })}</DialogTitle>
      <DialogContent dividers>
        <Stack spacing={2} sx={{ pt: 1 }}>
          <Stack direction={{ xs: 'column', sm: 'row' }} spacing={3} useFlexGap flexWrap="wrap">
            <BoxInfo label={ts('saleNumberLabel')} value={String(venta.numeroVenta)} />
            <BoxInfo label={tf('date')} value={new Date(venta.fechaVenta).toLocaleString(locale)} />
            <BoxInfo label={ts('clientLabel')} value={venta.clienteNombre || ts('noCliente')} />
            <BoxInfo label={tf('status')} value={estadoLabel} />
            <BoxInfo
              label={ts('totalLabel')}
              value={new Intl.NumberFormat(locale, {
                style: 'currency',
                currency: 'COP',
                maximumFractionDigits: 0,
              }).format(venta.total)}
            />
          </Stack>

          {venta.observacion ? (
            <Alert severity="info">{venta.observacion}</Alert>
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
                {venta.detalles.map((detalle) => (
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