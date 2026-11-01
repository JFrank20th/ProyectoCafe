export type VentaEstado = 1 | 2 | 3;

export type VentaDetalleHistorial = {
  ventaDetalleId: number;
  presentacionCafeId: number;
  presentacionCodigo: string;
  presentacionNombre: string;
  tipoCafeId: number;
  tipoCafeCodigo: string;
  tipoCafeNombre: string;
  precioVentaId: number;
  cantidadPaquetes: number;
  precioUnitario: number;
  subtotal: number;
};

export type PagoVentaHistorial = {
  pagoVentaId: number;
  ventaId: number;
  monto: number;
  fechaPago: string;
  metodoPago: string;
  referencia?: string | null;
  usuarioId?: number | null;
  creadoEn: string;
};

export type VentaHistorial = {
  ventaId: number;
  numeroVenta: number;
  fechaVenta: string;
  clienteId?: number | null;
  clienteNombre?: string | null;
  estado: VentaEstado | number;
  estadoEntrega: number;
  canceladoEn?: string | null;
  motivoCancelacion?: string | null;
  anuladoEn?: string | null;
  motivoAnulacion?: string | null;
  estadoPago: number;
  montoPagado: number;
  saldoPendiente: number;
  observacion?: string | null;
  total: number;
  creadoEn: string;
  detalles: VentaDetalleHistorial[];
  pagos: PagoVentaHistorial[];
};

import { apiFetch } from '@/lib/api/client';

export type MovimientoAsociado = {
  movimientoLoteCafeId: number;
  loteCodigo: string;
  tipoMovimiento: number;
  cantidadPaquetes: number;
  fechaMovimiento: string;
  referencia?: string | null;
};

export async function cancelVenta(ventaId: number, motivo: string): Promise<void> {
  await apiFetch<void>(`/api/v1/ventas/ventas/${ventaId}/cancelar`, {
    method: 'POST',
    body: JSON.stringify({ motivo }),
  });
}

export async function anulateVenta(ventaId: number, motivo: string): Promise<void> {
  await apiFetch<void>(`/api/v1/ventas/ventas/${ventaId}/anular`, {
    method: 'POST',
    body: JSON.stringify({ motivo }),
  });
}