import { ApiError } from '@/lib/api/client';

import {
  isCatalogoActivo,
  type EstadoLoteCafe,
  type ExistenciaCafe,
  type PresentacionCafe,
  type TipoCafe,
  type TipoMovimientoLoteCafe,
} from '@/lib/inventario/cafe';

export const LOTE_CODE_MAX_LENGTH = 50;
export const OBSERVACION_MAX_LENGTH = 250;
export const REFERENCIA_MAX_LENGTH = 100;
export const MAX_CANTIDAD_PAQUETES = 2_147_483_647;

export type LoteFormValues = {
  codigoLote: string;
  fechaProduccion: string;
  fechaEnvasado: string;
  fechaVencimiento: string;
  observacion: string;
  variedadCafeId: string;
  tipoTuesteCafeId: string;
  tipoProcesoCafeId: string;
};

export type LoteFieldError =
  | 'required'
  | 'maxLength'
  | 'invalidDate'
  | 'futureDate'
  | 'beforeProduction'
  | 'beforePackaging';

export type LoteFieldErrors = Partial<Record<keyof LoteFormValues, LoteFieldError>>;

export type CreateLotePayload = {
  codigoLote: string;
  fechaProduccion: string;
  fechaEnvasado: string | null;
  fechaVencimiento: string | null;
  observacion: string | null;
  variedadCafeId: number | null;
  tipoTuesteCafeId: number | null;
  tipoProcesoCafeId: number | null;
};

export type IngresoLineaInput = {
  id: string;
  presentacionCafeId: string;
  tipoCafeId: string;
  cantidad: string;
};

export type IngresoLineaFieldError =
  | 'presentationRequired'
  | 'coffeeTypeRequired'
  | 'qtyRequired'
  | 'qtyInvalid'
  | 'qtyTooLarge'
  | 'catalogUnavailable';

export type IngresoLineaFieldErrors = {
  presentacionCafeId?: IngresoLineaFieldError;
  tipoCafeId?: IngresoLineaFieldError;
  cantidad?: IngresoLineaFieldError;
};

export type IngresoLineaResolved = {
  id: string;
  presentacion: PresentacionCafe;
  tipoCafe: TipoCafe;
  cantidadPaquetes: number;
  pesoNetoKg: number;
};

export type IngresoLineasResolution = {
  errors: Record<string, IngresoLineaFieldErrors>;
  lines: IngresoLineaResolved[];
  totalPaquetes: number;
  totalPesoNetoKg: number;
};

export type IngresoLineaResult = {
  id: string;
  presentacionNombre: string;
  tipoCafeNombre: string;
  cantidadPaquetes: number;
  success: boolean;
  error?: string;
};

export type CreateLoteOperationResult = {
  loteCafeId: number;
  codigoLote: string;
  lineResults: IngresoLineaResult[];
};

export type MovimientoFormValues = {
  tipoMovimiento: TipoMovimientoLoteCafe;
  presentacionCafeId: string;
  tipoCafeId: string;
  cantidad: string;
  referencia: string;
  observacion: string;
};

export type MovimientoFieldErrors = {
  lote?: 'blocked';
  presentacionCafeId?: IngresoLineaFieldError;
  tipoCafeId?: IngresoLineaFieldError;
  cantidad?: IngresoLineaFieldError | 'insufficient';
  referencia?: 'maxLength';
  observacion?: 'required' | 'maxLength';
};

export type MovimientoValidation = {
  errors: MovimientoFieldErrors;
  presentacion: PresentacionCafe | null;
  tipoCafe: TipoCafe | null;
  cantidadPaquetes: number | null;
  pesoNetoKg: number | null;
  saldoActual: number;
  requiresBalance: boolean;
  requiresObservation: boolean;
};

export type InventoryErrorMessages = {
  conflict: string;
  forbidden?: string;
  notFound?: string;
};

export function buildCreateLotePayload(input: LoteFormValues): CreateLotePayload {
  const observacion = input.observacion.trim();

  function parseOptionalId(value: string): number | null {
    const trimmed = value.trim();
    if (!trimmed) return null;
    const parsed = Number(trimmed);
    return Number.isInteger(parsed) && parsed > 0 ? parsed : null;
  }

  return {
    codigoLote: input.codigoLote.trim().toUpperCase(),
    fechaProduccion: input.fechaProduccion,
    fechaEnvasado: input.fechaEnvasado.trim() || null,
    fechaVencimiento: calcularFechaVencimiento(input.fechaProduccion) || null,
    observacion: observacion || null,
    variedadCafeId: parseOptionalId(input.variedadCafeId),
    tipoTuesteCafeId: parseOptionalId(input.tipoTuesteCafeId),
    tipoProcesoCafeId: parseOptionalId(input.tipoProcesoCafeId),
  };
}

export function validateCreateLoteInput(
  input: LoteFormValues,
  todayIso: string,
): LoteFieldErrors {
  const errors: LoteFieldErrors = {};
  const codigo = input.codigoLote.trim();

  if (!codigo) {
    errors.codigoLote = 'required';
  } else if (codigo.length > LOTE_CODE_MAX_LENGTH) {
    errors.codigoLote = 'maxLength';
  }

  const produccion = input.fechaProduccion.trim();
  if (!produccion) {
    errors.fechaProduccion = 'required';
  } else if (!isIsoDate(produccion)) {
    errors.fechaProduccion = 'invalidDate';
  } else if (produccion > todayIso) {
    errors.fechaProduccion = 'futureDate';
  }

  const envasado = input.fechaEnvasado.trim();
  if (envasado) {
    if (!isIsoDate(envasado)) {
      errors.fechaEnvasado = 'invalidDate';
    } else if (!errors.fechaProduccion && envasado < produccion) {
      errors.fechaEnvasado = 'beforeProduction';
    }
  }

  const vencimiento = input.fechaVencimiento.trim();
  if (vencimiento) {
    if (!isIsoDate(vencimiento)) {
      errors.fechaVencimiento = 'invalidDate';
    } else if (!errors.fechaProduccion && vencimiento < produccion) {
      errors.fechaVencimiento = 'beforeProduction';
    } else if (!errors.fechaEnvasado && envasado && vencimiento < envasado) {
      errors.fechaVencimiento = 'beforePackaging';
    }
  }

  const observacion = input.observacion.trim();
  if (!observacion) {
    errors.observacion = 'required';
  } else if (observacion.length > OBSERVACION_MAX_LENGTH) {
    errors.observacion = 'maxLength';
  }

  if (!input.variedadCafeId.trim()) {
    errors.variedadCafeId = 'required';
  }
  if (!input.tipoTuesteCafeId.trim()) {
    errors.tipoTuesteCafeId = 'required';
  }
  if (!input.tipoProcesoCafeId.trim()) {
    errors.tipoProcesoCafeId = 'required';
  }

  return errors;
}

export function resolveIngresoLineas(
  lineas: readonly IngresoLineaInput[],
  presentaciones: readonly PresentacionCafe[],
  tiposCafe: readonly TipoCafe[],
): IngresoLineasResolution {
  const errors: Record<string, IngresoLineaFieldErrors> = {};
  const lines: IngresoLineaResolved[] = [];

  for (const linea of lineas) {
    const lineErrors: IngresoLineaFieldErrors = {};
    const presentacion = findActivePresentacion(presentaciones, linea.presentacionCafeId);
    const tipoCafe = findActiveTipoCafe(tiposCafe, linea.tipoCafeId);
    const qtyError = cantidadPaquetesError(linea.cantidad);
    const qty = qtyError ? null : Number(linea.cantidad.trim());

    if (!linea.presentacionCafeId.trim()) {
      lineErrors.presentacionCafeId = 'presentationRequired';
    } else if (!presentacion) {
      lineErrors.presentacionCafeId = 'catalogUnavailable';
    }

    if (!linea.tipoCafeId.trim()) {
      lineErrors.tipoCafeId = 'coffeeTypeRequired';
    } else if (!tipoCafe) {
      lineErrors.tipoCafeId = 'catalogUnavailable';
    }

    if (qtyError) {
      lineErrors.cantidad = qtyError;
    }

    if (Object.keys(lineErrors).length > 0) {
      errors[linea.id] = lineErrors;
      continue;
    }

    if (!presentacion || !tipoCafe || qty === null) {
      continue;
    }

    lines.push({
      id: linea.id,
      presentacion,
      tipoCafe,
      cantidadPaquetes: qty,
      pesoNetoKg: calcularPesoNetoKg(qty, presentacion.pesoUnitarioGramos),
    });
  }

  return {
    errors,
    lines,
    totalPaquetes: lines.reduce((total, line) => total + line.cantidadPaquetes, 0),
    totalPesoNetoKg: round3(lines.reduce((total, line) => total + line.pesoNetoKg, 0)),
  };
}

export function validateMovimientoInput(
  input: MovimientoFormValues,
  options: {
    presentaciones: readonly PresentacionCafe[];
    tiposCafe: readonly TipoCafe[];
    existencias: readonly ExistenciaCafe[];
    loteEstado: EstadoLoteCafe;
  },
): MovimientoValidation {
  const errors: MovimientoFieldErrors = {};
  const presentacion = findActivePresentacion(
    options.presentaciones,
    input.presentacionCafeId,
  );
  const tipoCafe = findActiveTipoCafe(options.tiposCafe, input.tipoCafeId);
  const qtyError = cantidadPaquetesError(input.cantidad);
  const cantidadPaquetes = qtyError ? null : Number(input.cantidad.trim());
  const requiresBalance = movementRequiresBalance(input.tipoMovimiento);
  const requiresObservation = movementRequiresObservation(input.tipoMovimiento);
  const saldoActual =
    presentacion && tipoCafe
      ? getSaldoActual(
          options.existencias,
          presentacion.presentacionCafeId,
          tipoCafe.tipoCafeId,
        )
      : 0;

  if (options.loteEstado === 3) {
    errors.lote = 'blocked';
  }

  if (!input.presentacionCafeId.trim()) {
    errors.presentacionCafeId = 'presentationRequired';
  } else if (!presentacion) {
    errors.presentacionCafeId = 'catalogUnavailable';
  }

  if (!input.tipoCafeId.trim()) {
    errors.tipoCafeId = 'coffeeTypeRequired';
  } else if (!tipoCafe) {
    errors.tipoCafeId = 'catalogUnavailable';
  }

  if (qtyError) {
    errors.cantidad = qtyError;
  } else if (cantidadPaquetes !== null && requiresBalance && cantidadPaquetes > saldoActual) {
    errors.cantidad = 'insufficient';
  }

  if (requiresObservation && !input.observacion.trim()) {
    errors.observacion = 'required';
  } else if (input.observacion.trim().length > OBSERVACION_MAX_LENGTH) {
    errors.observacion = 'maxLength';
  }

  if (input.referencia.trim().length > REFERENCIA_MAX_LENGTH) {
    errors.referencia = 'maxLength';
  }

  return {
    errors,
    presentacion,
    tipoCafe,
    cantidadPaquetes,
    pesoNetoKg:
      presentacion && cantidadPaquetes !== null
        ? calcularPesoNetoKg(cantidadPaquetes, presentacion.pesoUnitarioGramos)
        : null,
    saldoActual,
    requiresBalance,
    requiresObservation,
  };
}

export function buildMovimientoPayload(
  input: MovimientoFormValues,
  presentacionCafeId: number,
  tipoCafeId: number,
  cantidadPaquetes: number,
) {
  const referencia = input.referencia.trim();
  const observacion = input.observacion.trim();

  return {
    tipoMovimiento: input.tipoMovimiento,
    presentacionCafeId,
    tipoCafeId,
    cantidadPaquetes,
    referencia: referencia || null,
    observacion: observacion || null,
  };
}

export function getSaldoActual(
  existencias: readonly ExistenciaCafe[],
  presentacionCafeId: number,
  tipoCafeId: number,
): number {
  return (
    existencias.find(
      (existencia) =>
        existencia.presentacionCafeId === presentacionCafeId &&
        existencia.tipoCafeId === tipoCafeId,
    )?.cantidadPaquetes ?? 0
  );
}

export function movementRequiresBalance(tipo: TipoMovimientoLoteCafe): boolean {
  return tipo === 2 || tipo === 4;
}

export function movementRequiresObservation(tipo: TipoMovimientoLoteCafe): boolean {
  return tipo === 3 || tipo === 4;
}

export function cantidadPaquetesError(
  value: string,
): 'qtyRequired' | 'qtyInvalid' | 'qtyTooLarge' | null {
  const trimmed = value.trim();
  if (!trimmed) return 'qtyRequired';
  if (!/^\d+$/.test(trimmed)) return 'qtyInvalid';

  const quantity = Number(trimmed);
  if (!Number.isSafeInteger(quantity) || quantity <= 0) return 'qtyInvalid';
  if (quantity > MAX_CANTIDAD_PAQUETES) return 'qtyTooLarge';

  return null;
}

export function calcularPesoNetoKg(
  cantidadPaquetes: number,
  pesoUnitarioGramos: number,
): number {
  return round3((cantidadPaquetes * pesoUnitarioGramos) / 1000);
}

export function resolveInventoryError(
  error: unknown,
  fallback: string,
  messages: InventoryErrorMessages,
): string {
  if (error instanceof ApiError) {
    switch (error.status) {
      case 409:
        return messages.conflict;
      case 403:
        return messages.forbidden ?? fallback;
      case 404:
        return messages.notFound ?? fallback;
      default:
        return error.message || fallback;
    }
  }

  return error instanceof Error && error.message ? error.message : fallback;
}

export function todayIsoUtc(): string {
  return new Date().toISOString().slice(0, 10);
}

export function calcularFechaVencimiento(
  fechaTueste: string,
  meses = 3,
): string {
  if (!isIsoDate(fechaTueste)) return '';

  const [year, month, day] = fechaTueste.split('-').map(Number);
  const fecha = new Date(Date.UTC(year, month - 1, day));
  const mesDestino = fecha.getUTCMonth() + meses;
  const anioDestino = fecha.getUTCFullYear() + Math.floor(mesDestino / 12);
  const mesNormalizado = ((mesDestino % 12) + 12) % 12;
  const ultimoDia = new Date(Date.UTC(anioDestino, mesNormalizado + 1, 0)).getUTCDate();
  const diaFinal = Math.min(day, ultimoDia);

  return new Date(Date.UTC(anioDestino, mesNormalizado, diaFinal))
    .toISOString()
    .slice(0, 10);
}

export const addMonthsIso = calcularFechaVencimiento;

function findActivePresentacion(
  presentaciones: readonly PresentacionCafe[],
  value: string,
): PresentacionCafe | null {
  const id = Number(value);
  if (!Number.isInteger(id) || id <= 0) return null;

  return (
    presentaciones.find(
      (presentacion) =>
        presentacion.presentacionCafeId === id && isCatalogoActivo(presentacion.estado),
    ) ?? null
  );
}

function findActiveTipoCafe(tiposCafe: readonly TipoCafe[], value: string): TipoCafe | null {
  const id = Number(value);
  if (!Number.isInteger(id) || id <= 0) return null;

  return (
    tiposCafe.find(
      (tipoCafe) => tipoCafe.tipoCafeId === id && isCatalogoActivo(tipoCafe.estado),
    ) ?? null
  );
}

function isIsoDate(value: string): boolean {
  if (!/^\d{4}-\d{2}-\d{2}$/.test(value)) return false;

  const [year, month, day] = value.split('-').map(Number);
  const date = new Date(Date.UTC(year, month - 1, day));

  return (
    date.getUTCFullYear() === year &&
    date.getUTCMonth() === month - 1 &&
    date.getUTCDate() === day
  );
}

function round3(value: number): number {
  return Math.round(value * 1000) / 1000;
}
