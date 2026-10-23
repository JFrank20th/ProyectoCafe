import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import test from 'node:test';

const nextConfigSource = readFileSync(join(process.cwd(), 'next.config.js'), 'utf8');
const nextConfigCode = nextConfigSource
  .replace(/\/\*[\s\S]*?\*\//g, '')
  .replace(/\/\/.*$/gm, '');

test('next.config.js does not emit CORS headers', () => {
  // CORS se centraliza en la API. Si el Portal también respondiera con
  // Access-Control-*, se duplicarían o contradirían las cabeceras y el
  // navegador rechazaría la petición (especialmente con credenciales).
  assert.doesNotMatch(nextConfigCode, /Access-Control-/i);
});
