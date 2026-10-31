export type EstadoPrecioVenta = 1 | 2;

export type PrecioVenta = {
  precioVentaId: number;
  nombre: string;
  presentacionCafeId: number;
  presentacionCodigo: string;
  presentacionNombre: string;
  precio: number;
  estado: EstadoPrecioVenta | number;
  creadoEn: string;
  actualizadoEn?: string | null;
};

export type PrecioVentaSaved = {
  id: number;
  nombre: string;
  created: boolean;
};

export type PrecioVentaFormValues = {
  nombre: string;
  presentacionCafeId: string;
  precio: string;
  estado: EstadoPrecioVenta;
};

export function validatePrecioVentaInput(
  values: PrecioVentaFormValues,
): Partial<Record<keyof PrecioVentaFormValues, string>> {
  const errors: Partial<Record<keyof PrecioVentaFormValues, string>> = {};

  if (!values.nombre.trim()) errors.nombre = 'required';
  else if (values.nombre.trim().length > 80) errors.nombre = 'maxLength';

  if (!values.presentacionCafeId.trim()) errors.presentacionCafeId = 'required';

  const precio = Number(values.precio.trim().replace(',', '.'));
  if (!values.precio.trim()) errors.precio = 'required';
  else if (!Number.isFinite(precio) || precio <= 0) errors.precio = 'invalid';

  if (values.estado !== 1 && values.estado !== 2) errors.estado = 'invalid';

  return errors;
}