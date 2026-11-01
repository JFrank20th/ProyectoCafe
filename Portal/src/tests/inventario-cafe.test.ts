import assert from 'node:assert/strict';
import test from 'node:test';

import {
  estadoLoteColor,
  estadoLoteKey,
  filterLotes,
  formatDate,
  formatDateTime,
  formatGrams,
  formatKg,
  formatNumber,
  isCatalogoActivo,
  tipoMovimientoColor,
  tipoMovimientoKey,
  type LoteCafeResumen,
} from '@/lib/inventario/cafe';

const lotes: LoteCafeResumen[] = [
  {
    loteCafeId: 1,
    codigoLote: 'LOTE-2026-001',
    fechaProduccion: '2026-01-10T00:00:00Z',
    fechaEnvasado: '2026-01-11T00:00:00Z',
    fechaVencimiento: '2026-07-10T00:00:00Z',
    estado: 1,
    creadoEn: '2026-01-10T08:00:00Z',
    totalPaquetes: 100,
    totalPesoNetoKg: 25,
  },
  {
    loteCafeId: 2,
    codigoLote: 'LOTE-2026-002',
    fechaProduccion: '2026-02-10T00:00:00Z',
    estado: 2,
    creadoEn: '2026-02-10T08:00:00Z',
    totalPaquetes: 0,
    totalPesoNetoKg: 0,
  },
  {
    loteCafeId: 3,
    codigoLote: 'CAFE-ESPECIAL',
    fechaProduccion: '2026-03-10T00:00:00Z',
    estado: 3,
    creadoEn: '2026-03-10T08:00:00Z',
    totalPaquetes: 5,
    totalPesoNetoKg: 1.25,
  },
];

test('filterLotes searches by lot code without case or surrounding spaces', () => {
  assert.deepEqual(
    filterLotes(lotes, '  lote-2026-002 ', 'all').map((lote) => lote.loteCafeId),
    [2],
  );
  assert.deepEqual(
    filterLotes(lotes, 'especial', 'all').map((lote) => lote.loteCafeId),
    [3],
  );
  assert.deepEqual(filterLotes(lotes, '', 'all'), lotes);
});

test('filterLotes combines code search and lot status', () => {
  assert.deepEqual(
    filterLotes(lotes, 'LOTE-2026', 1).map((lote) => lote.loteCafeId),
    [1],
  );
  assert.deepEqual(filterLotes(lotes, 'LOTE-2026', 3), []);
  assert.deepEqual(filterLotes(lotes, '', 2).map((lote) => lote.loteCafeId), [2]);
});

test('filterLotes filters by production date range', () => {
  assert.deepEqual(
    filterLotes(lotes, '', 'all', '2026-02-01', null).map((lote) => lote.loteCafeId),
    [2, 3],
  );
  assert.deepEqual(
    filterLotes(lotes, '', 'all', null, '2026-02-15').map((lote) => lote.loteCafeId),
    [1, 2],
  );
  assert.deepEqual(
    filterLotes(lotes, '', 'all', '2026-02-01', '2026-02-15').map((lote) => lote.loteCafeId),
    [2],
  );
  assert.deepEqual(filterLotes(lotes, '', 'all', '', '').map((lote) => lote.loteCafeId), [1, 2, 3]);
});

test('filterLotes combines code, status and production dates', () => {
  assert.deepEqual(
    filterLotes(lotes, 'LOTE-2026', 1, '2026-01-01', '2026-01-31').map((lote) => lote.loteCafeId),
    [1],
  );
  assert.deepEqual(
    filterLotes(lotes, 'LOTE-2026', 2, '2026-03-01', '2026-03-31').map((lote) => lote.loteCafeId),
    [],
  );
});

test('lot status helpers are exhaustive and map to chip colors', () => {
  assert.equal(estadoLoteKey(1), 'lotStateActive');
  assert.equal(estadoLoteKey(2), 'lotStateAgotado');
  assert.equal(estadoLoteKey(3), 'lotStateBloqueado');
  assert.equal(estadoLoteKey(4), 'lotStateEliminado');

  assert.equal(estadoLoteColor(1), 'success');
  assert.equal(estadoLoteColor(2), 'warning');
  assert.equal(estadoLoteColor(3), 'error');
  assert.equal(estadoLoteColor(4), 'default');
});

test('movement type helpers are exhaustive and map to chip colors', () => {
  assert.equal(tipoMovimientoKey(1), 'movementIngreso');
  assert.equal(tipoMovimientoKey(2), 'movementSalida');
  assert.equal(tipoMovimientoKey(3), 'movementAjustePositivo');
  assert.equal(tipoMovimientoKey(4), 'movementAjusteNegativo');

  assert.equal(tipoMovimientoColor(1), 'success');
  assert.equal(tipoMovimientoColor(2), 'warning');
  assert.equal(tipoMovimientoColor(3), 'info');
  assert.equal(tipoMovimientoColor(4), 'error');
});

test('catalog status is considered active only for the active enum value', () => {
  assert.equal(isCatalogoActivo(1), true);
  assert.equal(isCatalogoActivo(2), false);
  assert.equal(isCatalogoActivo(3), false);
});

test('inventory number and date formatting is locale-aware and safe for empty values', () => {
  assert.equal(formatNumber('en-US', 1250), '1,250');
  assert.equal(formatGrams('en-US', 250), '250 g');
  assert.equal(formatKg('en-US', 3.75), '3.75 kg');
  assert.equal(formatDate('en-US', null), '—');
  assert.equal(formatDate('en-US', 'not-a-date'), '—');
  assert.equal(formatDateTime('en-US', undefined), '—');
});
