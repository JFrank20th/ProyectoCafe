import assert from 'node:assert/strict';
import test from 'node:test';

import { ApiError } from '@/lib/api/client';
import type { ExistenciaCafe, PresentacionCafe, TipoCafe } from '@/lib/inventario/cafe';
import {
  buildCreateLotePayload,
  calcularFechaVencimiento,
  buildMovimientoPayload,
  calcularPesoNetoKg,
  cantidadPaquetesError,
  getSaldoActual,
  movementRequiresBalance,
  movementRequiresObservation,
  resolveIngresoLineas,
  resolveInventoryError,
  todayIsoUtc,
  validateCreateLoteInput,
  validateMovimientoInput,
  type IngresoLineaInput,
  type LoteFormValues,
  type MovimientoFormValues,
} from '@/lib/inventario/operations';

const presentaciones: PresentacionCafe[] = [
  {
    presentacionCafeId: 1,
    codigo: 'P-250',
    nombre: 'Bolsa 250 g',
    pesoUnitarioGramos: 250,
    estado: 1,
    creadoEn: '2026-01-01T00:00:00Z',
  },
  {
    presentacionCafeId: 2,
    codigo: 'P-500',
    nombre: 'Bolsa 500 g',
    pesoUnitarioGramos: 500,
    estado: 2,
    creadoEn: '2026-01-01T00:00:00Z',
  },
];

const tiposCafe: TipoCafe[] = [
  {
    tipoCafeId: 10,
    codigo: 'T-ARABICA',
    nombre: 'Arábica',
    estado: 1,
    creadoEn: '2026-01-01T00:00:00Z',
  },
  {
    tipoCafeId: 11,
    codigo: 'T-ROBUSTA',
    nombre: 'Robusta',
    estado: 2,
    creadoEn: '2026-01-01T00:00:00Z',
  },
];

const existencias: ExistenciaCafe[] = [
  {
    loteCafeExistenciaId: 100,
    loteCafeId: 1,
    presentacionCafeId: 1,
    presentacionCodigo: 'P-250',
    presentacionNombre: 'Bolsa 250 g',
    presentacionEstado: 1,
    tipoCafeId: 10,
    tipoCafeCodigo: 'T-ARABICA',
    tipoCafeNombre: 'Arábica',
    tipoCafeEstado: 1,
    cantidadPaquetes: 10,
    pesoUnitarioGramos: 250,
    pesoNetoKg: 2.5,
  },
  {
    loteCafeExistenciaId: 101,
    loteCafeId: 1,
    presentacionCafeId: 2,
    presentacionCodigo: 'P-500',
    presentacionNombre: 'Bolsa 500 g',
    presentacionEstado: 2,
    tipoCafeId: 10,
    tipoCafeCodigo: 'T-ARABICA',
    tipoCafeNombre: 'Arábica',
    tipoCafeEstado: 1,
    cantidadPaquetes: 4,
    pesoUnitarioGramos: 500,
    pesoNetoKg: 2,
  },
];

const validLote: LoteFormValues = {
  codigoLote: ' lote-2026-010 ',
  fechaProduccion: '2026-10-01',
  fechaEnvasado: '2026-10-02',
  fechaVencimiento: '2027-04-02',
  observacion: ' Lote de prueba ',
  variedadCafeId: '',
  tipoTuesteCafeId: '',
  tipoProcesoCafeId: '',
};

const today = '2026-10-27';

function lineas(values: Partial<IngresoLineaInput>[]): IngresoLineaInput[] {
  return values.map((value, index) => ({
    id: `line-${index + 1}`,
    presentacionCafeId: '',
    tipoCafeId: '',
    cantidad: '',
    ...value,
  }));
}

function movimiento(values: Partial<MovimientoFormValues> = {}): MovimientoFormValues {
  return {
    tipoMovimiento: 1,
    presentacionCafeId: '1',
    tipoCafeId: '10',
    cantidad: '5',
    referencia: '',
    observacion: '',
    ...values,
  };
}

test('buildCreateLotePayload normalizes the code and optional text fields', () => {
  assert.deepEqual(buildCreateLotePayload(validLote), {
    codigoLote: 'LOTE-2026-010',
    fechaProduccion: '2026-10-01',
    fechaEnvasado: '2026-10-02',
    fechaVencimiento: '2027-01-01',
    observacion: 'Lote de prueba',
    variedadCafeId: null,
    tipoTuesteCafeId: null,
    tipoProcesoCafeId: null,
  });

  assert.deepEqual(
    buildCreateLotePayload({
      ...validLote,
      fechaEnvasado: '',
      fechaVencimiento: '',
      observacion: '   ',
      variedadCafeId: '7',
      tipoTuesteCafeId: '8',
      tipoProcesoCafeId: '9',
    }),
    {
      codigoLote: 'LOTE-2026-010',
      fechaProduccion: '2026-10-01',
      fechaEnvasado: null,
      fechaVencimiento: '2027-01-01',
      observacion: null,
      variedadCafeId: 7,
      tipoTuesteCafeId: 8,
      tipoProcesoCafeId: 9,
    },
  );
});

test('validateCreateLoteInput enforces backend lot and date rules', () => {
  assert.deepEqual(validateCreateLoteInput(validLote, today), {});

  assert.equal(
    validateCreateLoteInput({ ...validLote, codigoLote: '   ' }, today).codigoLote,
    'required',
  );
  assert.equal(
    validateCreateLoteInput(
      { ...validLote, codigoLote: 'A'.repeat(51) },
      today,
    ).codigoLote,
    'maxLength',
  );
  assert.equal(
    validateCreateLoteInput(
      { ...validLote, fechaProduccion: '2026-10-28' },
      today,
    ).fechaProduccion,
    'futureDate',
  );
  assert.equal(
    validateCreateLoteInput(
      { ...validLote, fechaProduccion: '2026-02-30' },
      today,
    ).fechaProduccion,
    'invalidDate',
  );
  assert.equal(
    validateCreateLoteInput(
      {
        ...validLote,
        fechaProduccion: '2026-10-10',
        fechaEnvasado: '2026-10-09',
      },
      today,
    ).fechaEnvasado,
    'beforeProduction',
  );
  assert.equal(
    validateCreateLoteInput(
      {
        ...validLote,
        fechaProduccion: '2026-10-10',
        fechaEnvasado: '2026-10-15',
        fechaVencimiento: '2026-10-12',
      },
      today,
    ).fechaVencimiento,
    'beforePackaging',
  );
  assert.equal(
    validateCreateLoteInput(
      {
        ...validLote,
        fechaProduccion: '2026-10-10',
        fechaEnvasado: '',
        fechaVencimiento: '2026-10-09',
      },
      today,
    ).fechaVencimiento,
    'beforeProduction',
  );
  assert.equal(
    validateCreateLoteInput(
      { ...validLote, observacion: 'A'.repeat(251) },
      today,
    ).observacion,
    'maxLength',
  );
});

test('quantity validation rejects empty, zero, negative, decimal and oversized values', () => {
  assert.equal(cantidadPaquetesError(''), 'qtyRequired');
  assert.equal(cantidadPaquetesError('   '), 'qtyRequired');
  assert.equal(cantidadPaquetesError('0'), 'qtyInvalid');
  assert.equal(cantidadPaquetesError('-1'), 'qtyInvalid');
  assert.equal(cantidadPaquetesError('1.5'), 'qtyInvalid');
  assert.equal(cantidadPaquetesError('abc'), 'qtyInvalid');
  assert.equal(cantidadPaquetesError('2147483648'), 'qtyTooLarge');
  assert.equal(cantidadPaquetesError(' 3 '), null);
  assert.equal(cantidadPaquetesError('2147483647'), null);
});

test('resolveIngresoLineas resolves active catalogs, independent quantities and totals', () => {
  const result = resolveIngresoLineas(
    lineas([
      { presentacionCafeId: '1', tipoCafeId: '10', cantidad: '2' },
      { presentacionCafeId: '1', tipoCafeId: '10', cantidad: '3' },
      { presentacionCafeId: '2', tipoCafeId: '10', cantidad: '1' },
      { presentacionCafeId: '1', tipoCafeId: '11', cantidad: '1' },
      { presentacionCafeId: '', tipoCafeId: '', cantidad: '' },
      { presentacionCafeId: '1', tipoCafeId: '10', cantidad: '0' },
    ]),
    presentaciones,
    tiposCafe,
  );

  assert.equal(result.lines.length, 2);
  assert.deepEqual(
    result.lines.map((line) => ({
      presentacion: line.presentacion.presentacionCafeId,
      tipo: line.tipoCafe.tipoCafeId,
      cantidad: line.cantidadPaquetes,
      peso: line.pesoNetoKg,
    })),
    [
      { presentacion: 1, tipo: 10, cantidad: 2, peso: 0.5 },
      { presentacion: 1, tipo: 10, cantidad: 3, peso: 0.75 },
    ],
  );
  assert.equal(result.totalPaquetes, 5);
  assert.equal(result.totalPesoNetoKg, 1.25);

  assert.equal(result.errors['line-3'].presentacionCafeId, 'catalogUnavailable');
  assert.equal(result.errors['line-4'].tipoCafeId, 'catalogUnavailable');
  assert.equal(result.errors['line-5'].presentacionCafeId, 'presentationRequired');
  assert.equal(result.errors['line-5'].tipoCafeId, 'coffeeTypeRequired');
  assert.equal(result.errors['line-5'].cantidad, 'qtyRequired');
  assert.equal(result.errors['line-6'].cantidad, 'qtyInvalid');
});

test('getSaldoActual only returns the balance for the exact presentation and coffee type', () => {
  assert.equal(getSaldoActual(existencias, 1, 10), 10);
  assert.equal(getSaldoActual(existencias, 2, 10), 4);
  assert.equal(getSaldoActual(existencias, 1, 11), 0);
  assert.equal(getSaldoActual(existencias, 99, 10), 0);
});

test('movement helpers identify operations that need balance or observation', () => {
  assert.equal(movementRequiresBalance(1), false);
  assert.equal(movementRequiresBalance(2), true);
  assert.equal(movementRequiresBalance(3), false);
  assert.equal(movementRequiresBalance(4), true);

  assert.equal(movementRequiresObservation(1), false);
  assert.equal(movementRequiresObservation(2), false);
  assert.equal(movementRequiresObservation(3), true);
  assert.equal(movementRequiresObservation(4), true);
});

test('validateMovimientoInput accepts inbound movements without observation', () => {
  const result = validateMovimientoInput(movimiento(), {
    presentaciones,
    tiposCafe,
    existencias,
    loteEstado: 1,
  });

  assert.deepEqual(result.errors, {});
  assert.equal(result.presentacion?.presentacionCafeId, 1);
  assert.equal(result.tipoCafe?.tipoCafeId, 10);
  assert.equal(result.cantidadPaquetes, 5);
  assert.equal(result.saldoActual, 10);
  assert.equal(result.pesoNetoKg, 1.25);
  assert.equal(result.requiresBalance, false);
  assert.equal(result.requiresObservation, false);
});

test('validateMovimientoInput blocks outbound and negative adjustments above the balance', () => {
  const salida = validateMovimientoInput(movimiento({ tipoMovimiento: 2, cantidad: '11' }), {
    presentaciones,
    tiposCafe,
    existencias,
    loteEstado: 1,
  });
  assert.equal(salida.errors.cantidad, 'insufficient');
  assert.equal(salida.saldoActual, 10);

  const ajusteNegativo = validateMovimientoInput(
    movimiento({ tipoMovimiento: 4, cantidad: '11', observacion: 'Conteo físico' }),
    { presentaciones, tiposCafe, existencias, loteEstado: 1 },
  );
  assert.equal(ajusteNegativo.errors.cantidad, 'insufficient');

  const salidaValida = validateMovimientoInput(
    movimiento({ tipoMovimiento: 2, cantidad: '10' }),
    { presentaciones, tiposCafe, existencias, loteEstado: 1 },
  );
  assert.deepEqual(salidaValida.errors, {});
});

test('validateMovimientoInput requires observations for adjustments and validates text limits', () => {
  const positivoSinObservacion = validateMovimientoInput(
    movimiento({ tipoMovimiento: 3 }),
    { presentaciones, tiposCafe, existencias, loteEstado: 1 },
  );
  assert.equal(positivoSinObservacion.errors.observacion, 'required');

  const positivoConObservacion = validateMovimientoInput(
    movimiento({ tipoMovimiento: 3, observacion: 'Ajuste por conteo' }),
    { presentaciones, tiposCafe, existencias, loteEstado: 1 },
  );
  assert.deepEqual(positivoConObservacion.errors, {});

  const negativoValido = validateMovimientoInput(
    movimiento({
      tipoMovimiento: 4,
      cantidad: '4',
      observacion: 'Merma detectada',
    }),
    { presentaciones, tiposCafe, existencias, loteEstado: 1 },
  );
  assert.deepEqual(negativoValido.errors, {});

  const textosLargos = validateMovimientoInput(
    movimiento({
      tipoMovimiento: 3,
      observacion: 'A'.repeat(251),
      referencia: 'R'.repeat(101),
    }),
    { presentaciones, tiposCafe, existencias, loteEstado: 1 },
  );
  assert.equal(textosLargos.errors.observacion, 'maxLength');
  assert.equal(textosLargos.errors.referencia, 'maxLength');
});

test('validateMovimientoInput rejects blocked lots, inactive catalogs and invalid quantities', () => {
  const bloqueado = validateMovimientoInput(movimiento(), {
    presentaciones,
    tiposCafe,
    existencias,
    loteEstado: 3,
  });
  assert.equal(bloqueado.errors.lote, 'blocked');

  const catalogoInactivo = validateMovimientoInput(
    movimiento({ presentacionCafeId: '2' }),
    { presentaciones, tiposCafe, existencias, loteEstado: 1 },
  );
  assert.equal(catalogoInactivo.errors.presentacionCafeId, 'catalogUnavailable');

  const cantidadCero = validateMovimientoInput(movimiento({ cantidad: '0' }), {
    presentaciones,
    tiposCafe,
    existencias,
    loteEstado: 1,
  });
  assert.equal(cantidadCero.errors.cantidad, 'qtyInvalid');
});

test('buildMovimientoPayload trims optional text and sends null when empty', () => {
  assert.deepEqual(
    buildMovimientoPayload(
      movimiento({
        tipoMovimiento: 4,
        cantidad: '3',
        referencia: '  REF-1 ',
        observacion: ' Ajuste ',
      }),
      1,
      10,
      3,
    ),
    {
      tipoMovimiento: 4,
      presentacionCafeId: 1,
      tipoCafeId: 10,
      cantidadPaquetes: 3,
      referencia: 'REF-1',
      observacion: 'Ajuste',
    },
  );

  assert.deepEqual(
    buildMovimientoPayload(movimiento({ referencia: ' ', observacion: ' ' }), 1, 10, 5),
    {
      tipoMovimiento: 1,
      presentacionCafeId: 1,
      tipoCafeId: 10,
      cantidadPaquetes: 5,
      referencia: null,
      observacion: null,
    },
  );
});

test('calcularPesoNetoKg rounds to grams and todayIsoUtc returns an ISO date', () => {
  assert.equal(calcularPesoNetoKg(3, 454), 1.362);
  assert.equal(calcularPesoNetoKg(0, 454), 0);
  assert.match(todayIsoUtc(), /^\d{4}-\d{2}-\d{2}$/);
});

test('calcularFechaVencimiento adds three calendar months with month-end handling', () => {
  assert.equal(calcularFechaVencimiento('2026-01-15'), '2026-04-15');
  assert.equal(calcularFechaVencimiento('2026-10-31'), '2027-01-31');
  assert.equal(calcularFechaVencimiento('2026-01-31'), '2026-04-30');
  assert.equal(calcularFechaVencimiento('2024-02-29'), '2024-05-29');
  assert.equal(calcularFechaVencimiento(''), '');
});

test('resolveInventoryError maps conflicts, permissions and backend validation messages', () => {
  const messages = {
    conflict: 'Conflicto de concurrencia',
    forbidden: 'Sin permiso',
    notFound: 'No encontrado',
  };

  assert.equal(
    resolveInventoryError(new ApiError('conflict', 409, '{}'), 'fallback', messages),
    'Conflicto de concurrencia',
  );
  assert.equal(
    resolveInventoryError(new ApiError('forbidden', 403, '{}'), 'fallback', messages),
    'Sin permiso',
  );
  assert.equal(
    resolveInventoryError(new ApiError('missing', 404, '{}'), 'fallback', messages),
    'No encontrado',
  );
  assert.equal(
    resolveInventoryError(
      new ApiError('El lote está bloqueado.', 400, '{}'),
      'fallback',
      messages,
    ),
    'El lote está bloqueado.',
  );
  assert.equal(resolveInventoryError(new Error('network'), 'fallback', messages), 'network');
  assert.equal(resolveInventoryError('unknown', 'fallback', messages), 'fallback');
});
