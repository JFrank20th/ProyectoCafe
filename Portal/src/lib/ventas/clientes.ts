export type EstadoCliente = 1 | 2;

export type Cliente = {
  clienteId: number;
  tipoIdentificacion: string;
  numeroIdentificacion: string;
  nombre: string;
  telefono?: string | null;
  email?: string | null;
  direccion?: string | null;
  observacion?: string | null;
  estado: EstadoCliente | number;
  creadoEn: string;
  actualizadoEn?: string | null;
};

export type ClienteFormValues = {
  tipoIdentificacion: string;
  numeroIdentificacion: string;
  nombre: string;
  telefono: string;
  email: string;
  direccion: string;
  observacion: string;
  estado: EstadoCliente;
};

export type ClienteSaved = {
  id: number;
  nombre: string;
  created: boolean;
};

export function validateClienteInput(
  values: ClienteFormValues,
): Partial<Record<keyof ClienteFormValues, string>> {
  const errors: Partial<Record<keyof ClienteFormValues, string>> = {};

  if (!values.tipoIdentificacion.trim()) errors.tipoIdentificacion = 'required';
  else if (values.tipoIdentificacion.trim().length > 10) errors.tipoIdentificacion = 'maxLength';

  if (!values.numeroIdentificacion.trim()) errors.numeroIdentificacion = 'required';
  else if (values.numeroIdentificacion.trim().length > 30) errors.numeroIdentificacion = 'maxLength';

  if (!values.nombre.trim()) errors.nombre = 'required';
  else if (values.nombre.trim().length > 150) errors.nombre = 'maxLength';

  if (values.telefono.trim().length > 30) errors.telefono = 'maxLength';
  if (values.email.trim().length > 100) errors.email = 'maxLength';
  else if (values.email.trim() && !values.email.includes('@')) errors.email = 'invalid';
  if (values.direccion.trim().length > 200) errors.direccion = 'maxLength';
  if (values.observacion.trim().length > 250) errors.observacion = 'maxLength';

  if (values.estado !== 1 && values.estado !== 2) errors.estado = 'invalid';

  return errors;
}