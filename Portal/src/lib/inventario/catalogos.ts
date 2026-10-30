import type {
  EstadoCatalogoCafe,
  PresentacionCafe,
  TipoCafe,
} from '@/lib/inventario/cafe';

export const CATALOGO_CODIGO_MAX_LENGTH = 20;
export const CATALOGO_NOMBRE_MAX_LENGTH = 80;
export const MAX_PESO_UNITARIO_GRAMOS = 2_147_483_647;

export type CatalogoCafeKind = 'presentacion' | 'tipoCafe';

export type EstadoCatalogoCafeEditable = 1 | 2;

export type CatalogoCafeRow = {
  id: number;
  codigo: string;
  nombre: string;
  estado: EstadoCatalogoCafe;
  pesoUnitarioGramos?: number | null;
  actualizadoEn?: string | null;
};

export type CatalogoCafeFormValues = {
  codigo: string;
  nombre: string;
  pesoUnitarioGramos: string;
  estado: EstadoCatalogoCafeEditable;
};

export type CatalogoCafeFieldError =
  | 'required'
  | 'maxLength'
  | 'invalidWeight'
  | 'weightTooLarge'
  | 'invalidState';

export type CatalogoCafeFieldErrors = Partial<
  Record<keyof CatalogoCafeFormValues, CatalogoCafeFieldError>
>;

export type PresentacionCafePayload = {
  codigo: string;
  nombre: string;
  pesoUnitarioGramos: number;
  estado: EstadoCatalogoCafe;
};

export type TipoCafePayload = {
  codigo: string;
  nombre: string;
  estado: EstadoCatalogoCafe;
};

export function toPresentacionCafeRow(presentacion: PresentacionCafe): CatalogoCafeRow {
  return {
    id: presentacion.presentacionCafeId,
    codigo: presentacion.codigo,
    nombre: presentacion.nombre,
    estado: presentacion.estado,
    pesoUnitarioGramos: presentacion.pesoUnitarioGramos,
    actualizadoEn: presentacion.actualizadoEn,
  };
}

export function toTipoCafeRow(tipoCafe: TipoCafe): CatalogoCafeRow {
  return {
    id: tipoCafe.tipoCafeId,
    codigo: tipoCafe.codigo,
    nombre: tipoCafe.nombre,
    estado: tipoCafe.estado,
    actualizadoEn: tipoCafe.actualizadoEn,
  };
}

export function catalogoCafePath(kind: CatalogoCafeKind): string {
  return kind === 'presentacion'
    ? '/api/v1/inventario/presentaciones'
    : '/api/v1/inventario/tipos-cafe';
}

export function validatePresentacionCafeInput(
  input: CatalogoCafeFormValues,
): CatalogoCafeFieldErrors {
  return validateCatalogoCafeInput('presentacion', input);
}

export function validateTipoCafeInput(
  input: CatalogoCafeFormValues,
): CatalogoCafeFieldErrors {
  return validateCatalogoCafeInput('tipoCafe', input);
}

export function validateCatalogoCafeInput(
  kind: CatalogoCafeKind,
  input: CatalogoCafeFormValues,
): CatalogoCafeFieldErrors {
  const errors: CatalogoCafeFieldErrors = {};
  const codigo = input.codigo.trim();
  const nombre = input.nombre.trim();

  if (!codigo) {
    errors.codigo = 'required';
  } else if (codigo.length > CATALOGO_CODIGO_MAX_LENGTH) {
    errors.codigo = 'maxLength';
  }

  if (!nombre) {
    errors.nombre = 'required';
  } else if (nombre.length > CATALOGO_NOMBRE_MAX_LENGTH) {
    errors.nombre = 'maxLength';
  }

  if (kind === 'presentacion') {
    const weightError = validatePesoUnitario(input.pesoUnitarioGramos);
    if (weightError) {
      errors.pesoUnitarioGramos = weightError;
    }
  }

  if (input.estado !== 1 && input.estado !== 2) {
    errors.estado = 'invalidState';
  }

  return errors;
}

export function buildPresentacionCafePayload(
  input: CatalogoCafeFormValues,
): PresentacionCafePayload {
  return {
    codigo: input.codigo.trim().toUpperCase(),
    nombre: input.nombre.trim(),
    pesoUnitarioGramos: Number(input.pesoUnitarioGramos.trim()),
    estado: input.estado,
  };
}

export function buildTipoCafePayload(input: CatalogoCafeFormValues): TipoCafePayload {
  return {
    codigo: input.codigo.trim().toUpperCase(),
    nombre: input.nombre.trim(),
    estado: input.estado,
  };
}

function validatePesoUnitario(value: string): CatalogoCafeFieldError | null {
  const trimmed = value.trim();
  if (!trimmed) return 'required';
  if (!/^\d+$/.test(trimmed)) return 'invalidWeight';

  const weight = Number(trimmed);
  if (!Number.isSafeInteger(weight) || weight <= 0) return 'invalidWeight';
  if (weight > MAX_PESO_UNITARIO_GRAMOS) return 'weightTooLarge';

  return null;
}
