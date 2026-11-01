import type {
  EstadoCatalogoCafe,
  PresentacionCafe,
  TipoCafe,
  VariedadCafe,
  TipoTuesteCafe,
  TipoProcesoCafe,
} from '@/lib/inventario/cafe';

export const CATALOGO_CODIGO_MAX_LENGTH = 20;
export const CATALOGO_NOMBRE_MAX_LENGTH = 80;
export const MAX_PESO_UNITARIO_GRAMOS = 2_147_483_647;

export type CatalogoCafeKind = 'presentacion' | 'tipoCafe' | 'variedad' | 'tipoTueste' | 'tipoProceso';

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

export function toVariedadCafeRow(variedadCafe: VariedadCafe): CatalogoCafeRow {
  return {
    id: variedadCafe.variedadCafeId,
    codigo: variedadCafe.codigo,
    nombre: variedadCafe.nombre,
    estado: variedadCafe.estado,
    actualizadoEn: variedadCafe.actualizadoEn,
  };
}

export function toTipoTuesteCafeRow(tipoTuesteCafe: TipoTuesteCafe): CatalogoCafeRow {
  return {
    id: tipoTuesteCafe.tipoTuesteCafeId,
    codigo: tipoTuesteCafe.codigo,
    nombre: tipoTuesteCafe.nombre,
    estado: tipoTuesteCafe.estado,
    actualizadoEn: tipoTuesteCafe.actualizadoEn,
  };
}

export function toTipoProcesoCafeRow(tipoProcesoCafe: TipoProcesoCafe): CatalogoCafeRow {
  return {
    id: tipoProcesoCafe.tipoProcesoCafeId,
    codigo: tipoProcesoCafe.codigo,
    nombre: tipoProcesoCafe.nombre,
    estado: tipoProcesoCafe.estado,
    actualizadoEn: tipoProcesoCafe.actualizadoEn,
  };
}

export function catalogoCafePath(kind: CatalogoCafeKind): string {
  if (kind === 'presentacion') return '/api/v1/inventario/presentaciones';
  if (kind === 'variedad') return '/api/v1/inventario/variedades';
  if (kind === 'tipoTueste') return '/api/v1/inventario/tipos-tueste';
  if (kind === 'tipoProceso') return '/api/v1/inventario/tipos-proceso';
  return '/api/v1/inventario/tipos-cafe';
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

export function validateVariedadCafeInput(
  input: CatalogoCafeFormValues,
): CatalogoCafeFieldErrors {
  return validateCatalogoCafeInput('variedad', input);
}

export function validateTipoTuesteCafeInput(
  input: CatalogoCafeFormValues,
): CatalogoCafeFieldErrors {
  return validateCatalogoCafeInput('tipoTueste', input);
}

export function validateTipoProcesoCafeInput(
  input: CatalogoCafeFormValues,
): CatalogoCafeFieldErrors {
  return validateCatalogoCafeInput('tipoProceso', input);
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

export type VariedadCafePayload = TipoCafePayload;

export function buildVariedadCafePayload(input: CatalogoCafeFormValues): VariedadCafePayload {
  return buildTipoCafePayload(input);
}

export type TipoTuesteCafePayload = TipoCafePayload;

export function buildTipoTuesteCafePayload(input: CatalogoCafeFormValues): TipoTuesteCafePayload {
  return buildTipoCafePayload(input);
}

export type TipoProcesoCafePayload = TipoCafePayload;

export function buildTipoProcesoCafePayload(input: CatalogoCafeFormValues): TipoProcesoCafePayload {
  return buildTipoCafePayload(input);
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
