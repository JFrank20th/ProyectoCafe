import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import test from 'node:test';

function readPortalSource(path: string) {
  return readFileSync(join(process.cwd(), path), 'utf8');
}

const authCardSource = readPortalSource('src/components/auth/AuthCardWrapper.tsx');
const loginSource = readPortalSource('src/app/login/page.tsx');
const registerSource = readPortalSource('src/app/register/page.tsx');

test('login and register share the same responsive card dimensions', () => {
  // La tarjeta debe ocupar el ancho disponible para que Login no se encoja
  // cuando Register tiene más contenido, manteniendo los máximos del diseño.
  assert.match(authCardSource, /width:\s*'100%'/);
  assert.match(authCardSource, /maxWidth:\s*\{\s*xs:\s*400,\s*lg:\s*475\s*\}/);

  for (const source of [loginSource, registerSource]) {
    assert.match(source, /<AuthCardWrapper>/);
    assert.match(source, /alignSelf:\s*'stretch'/);
    assert.match(source, /justifyContent:\s*'center'/);
  }
});
