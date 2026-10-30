import assert from 'node:assert/strict';
import test from 'node:test';

import {
  estadoCatalogoColor,
  estadoCatalogoKey,
  type PresentacionCafe,
  type TipoCafe,
} from '@/lib/inventario/cafe';
import {
  buildPresentacionCafePayload,
  buildTipoCafePayload,
  CATALOGO_CODIGO_MAX_LENGTH,
  CATALOGO_NOMBRE_MAX_LENGTH,
  catalogoCafePath,
  MAX_PESO_UNITARIO_GRAMOS,
  toPresentacionCafeRow,
  toTipoCafeRow,
  validatePresentacionCafeInput,
  validateTipoCafeInput,
  type CatalogoCafeFormValues,
} from '@/lib/inventario/catalogos';

function values(patch: Partial<CatalogoCafeFormValues> = {}): CatalogoCafeFormValues {
  return {
    codigo: '250G',
    nombre: '250 g',
    pesoUnitarioGramos: '250',
    estado: 1,
    ...patch,
  };
}

test('presentation validation accepts a complete active catalog', () => {
  assert.deepEqual(validatePresentacionCafeInput(values()), {});
  assert.deepEqual(validatePresentacionCafeInput(values({ estado: 2 })), {});
});

test('presentation validation checks code, name and weight', () => {
  assert.deepEqual(validatePresentacionCafeInput(values({ codigo: ' ' })), {
    codigo: 'required',
  });
  assert.deepEqual(validatePresentacionCafeInput(values({ nombre: ' ' })), {
    nombre: 'required',
  });
  assert.deepEqual(validatePresentacionCafeInput(values({ pesoUnitarioGramos: '' })), {
    pesoUnitarioGramos: 'required',
  });
  assert.deepEqual(validatePresentacionCafeInput(values({ pesoUnitarioGramos: '0' })), {
    pesoUnitarioGramos: 'invalidWeight',
  });
  assert.deepEqual(validatePresentacionCafeInput(values({ pesoUnitarioGramos: '1.5' })), {
    pesoUnitarioGramos: 'invalidWeight',
  });
  assert.deepEqual(
    validatePresentacionCafeInput(
      values({ pesoUnitarioGramos: String(MAX_PESO_UNITARIO_GRAMOS + 1) }),
    ),
    { pesoUnitarioGramos: 'weightTooLarge' },
  );
});

test('catalog validation enforces code and name lengths', () => {
  assert.deepEqual(
    validateTipoCafeInput(values({ codigo: 'X'.repeat(CATALOGO_CODIGO_MAX_LENGTH + 1) })),
    { codigo: 'maxLength' },
  );
  assert.deepEqual(
    validateTipoCafeInput(values({ nombre: 'X'.repeat(CATALOGO_NOMBRE_MAX_LENGTH + 1) })),
    { nombre: 'maxLength' },
  );
});

test('coffee type validation does not require a weight', () => {
  assert.deepEqual(validateTipoCafeInput(values({ pesoUnitarioGramos: '' })), {});
});

test('presentation payload normalizes code, name and numeric weight', () => {
  assert.deepEqual(
    buildPresentacionCafePayload(
      values({
        codigo: ' 250g ',
        nombre: '  250 gramos  ',
        pesoUnitarioGramos: ' 250 ',
        estado: 2,
      }),
    ),
    {
      codigo: '250G',
      nombre: '250 gramos',
      pesoUnitarioGramos: 250,
      estado: 2,
    },
  );
});

test('coffee type payload normalizes text and keeps the selected state', () => {
  const payload = buildTipoCafePayload(
    values({ codigo: ' grano ', nombre: ' Grano ', estado: 2 }),
  );

  assert.deepEqual(payload, {
    codigo: 'GRANO',
    nombre: 'Grano',
    estado: 2,
  });
  assert.equal('pesoUnitarioGramos' in payload, false);
});

test('catalog rows expose the common id and weight fields', () => {
  const presentacion: PresentacionCafe = {
    presentacionCafeId: 7,
    codigo: '250G',
    nombre: '250 g',
    pesoUnitarioGramos: 250,
    estado: 1,
    creadoEn: '2026-01-01T00:00:00Z',
    actualizadoEn: '2026-01-02T00:00:00Z',
  };
  const tipoCafe: TipoCafe = {
    tipoCafeId: 9,
    codigo: 'GRANO',
    nombre: 'Grano',
    estado: 2,
    creadoEn: '2026-01-01T00:00:00Z',
    actualizadoEn: null,
  };

  assert.deepEqual(toPresentacionCafeRow(presentacion), {
    id: 7,
    codigo: '250G',
    nombre: '250 g',
    estado: 1,
    pesoUnitarioGramos: 250,
    actualizadoEn: '2026-01-02T00:00:00Z',
  });
  assert.deepEqual(toTipoCafeRow(tipoCafe), {
    id: 9,
    codigo: 'GRANO',
    nombre: 'Grano',
    estado: 2,
    actualizadoEn: null,
  });
});

test('catalog endpoints and state labels match the API contract', () => {
  assert.equal(catalogoCafePath('presentacion'), '/api/v1/inventario/presentaciones');
  assert.equal(catalogoCafePath('tipoCafe'), '/api/v1/inventario/tipos-cafe');
  assert.equal(estadoCatalogoKey(1), 'catalogStateActive');
  assert.equal(estadoCatalogoKey(2), 'catalogStateInactive');
  assert.equal(estadoCatalogoKey(3), 'catalogStateDeleted');
  assert.equal(estadoCatalogoColor(1), 'success');
  assert.equal(estadoCatalogoColor(2), 'default');
  assert.equal(estadoCatalogoColor(3), 'error');
});
