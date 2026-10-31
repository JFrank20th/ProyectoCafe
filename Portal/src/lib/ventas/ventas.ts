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

export type VentaHistorial = {
  ventaId: number;
  numeroVenta: number;
  fechaVenta: string;
  clienteId?: number | null;
  clienteNombre?: string | null;
  estado: VentaEstado | number;
  observacion?: string | null;
  total: number;
  creadoEn: string;
  detalles: VentaDetalleHistorial[];
};

export type MovimientoAsociado = {
  movimientoLoteCafeId: number;
  loteCodigo: string;
  tipoMovimiento: number;
  cantidadPaquetes: number;
  fechaMovimiento: string;
  referencia?: string | null;
};