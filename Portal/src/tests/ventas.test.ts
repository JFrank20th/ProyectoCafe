import assert from 'node:assert/strict';
import { afterEach, test } from 'node:test';

import { ApiError } from '@/lib/api/client';
import { anulateVenta, cancelVenta } from '@/lib/ventas/ventas';

const originalFetch = globalThis.fetch;
const originalWindow = (globalThis as { window?: unknown }).window;
const originalDocument = (globalThis as { document?: unknown }).document;

let calls: Array<{ url: string; init: RequestInit }> = [];

function installBrowser() {
  calls = [];
  (globalThis as { window?: unknown }).window = {
    __RUNTIME_ENV__: {
      API_URL: 'https://api.test/',
      APP_NAME: 'ProyectoCafe',
      APP_VERSION: 'test',
    },
    location: {
      pathname: '/dashboard/ventas',
      search: '',
      replace: () => undefined,
    },
  };
  (globalThis as { document?: unknown }).document = {
    cookie: 'pc_csrf=csrf-token',
  };
}

function respond(status: number, body: unknown = null) {
  globalThis.fetch = async (input, init) => {
    calls.push({ url: String(input), init: init ?? {} });
    return new Response(body === null ? null : JSON.stringify(body), {
      status,
      headers: body === null ? undefined : { 'content-type': 'application/json' },
    });
  };
}

afterEach(() => {
  globalThis.fetch = originalFetch;
  (globalThis as { window?: unknown }).window = originalWindow;
  (globalThis as { document?: unknown }).document = originalDocument;
});

test('cancelVenta calls the cancellation endpoint with the trimmed reason', async () => {
  installBrowser();
  respond(204);

  await cancelVenta(7, '  Motivo de prueba  ');

  assert.equal(calls.length, 1);
  assert.equal(calls[0].url, 'https://api.test/api/v1/ventas/ventas/7/cancelar');
  assert.equal(calls[0].init.method, 'POST');
  assert.deepEqual(JSON.parse(String(calls[0].init.body)), { motivo: '  Motivo de prueba  ' });
});

test('anulateVenta calls the annulment endpoint with the reason', async () => {
  installBrowser();
  respond(204);

  await anulateVenta(9, 'Motivo administrativo');

  assert.equal(calls.length, 1);
  assert.equal(calls[0].url, 'https://api.test/api/v1/ventas/ventas/9/anular');
  assert.equal(calls[0].init.method, 'POST');
  assert.deepEqual(JSON.parse(String(calls[0].init.body)), { motivo: 'Motivo administrativo' });
});

test('cancelVenta propagates backend validation errors', async () => {
  installBrowser();
  respond(400, { errors: { motivo: ['El motivo es requerido.'] } });

  await assert.rejects(
    () => cancelVenta(7, ''),
    (error: unknown) => {
      assert.ok(error instanceof ApiError);
      assert.equal(error.status, 400);
      return true;
    },
  );
});

test('anulateVenta propagates backend validation errors', async () => {
  installBrowser();
  respond(400, { errors: { motivo: ['El motivo no puede exceder 250 caracteres.'] } });

  await assert.rejects(
    () => anulateVenta(9, 'A'.repeat(251)),
    (error: unknown) => {
      assert.ok(error instanceof ApiError);
      assert.equal(error.status, 400);
      return true;
    },
  );
});