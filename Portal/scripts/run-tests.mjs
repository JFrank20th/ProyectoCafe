import { readdirSync, statSync } from 'node:fs';
import { join, relative } from 'node:path';
import { spawnSync } from 'node:child_process';

const root = process.cwd();
const sourceDir = join(root, 'src');

function collectTestFiles(directory) {
  const files = [];

  for (const entry of readdirSync(directory)) {
    const fullPath = join(directory, entry);
    const stats = statSync(fullPath);

    if (stats.isDirectory()) {
      files.push(...collectTestFiles(fullPath));
      continue;
    }

    if (entry.endsWith('.test.ts')) {
      files.push(relative(root, fullPath));
    }
  }

  return files;
}

const testFiles = collectTestFiles(sourceDir).sort();

if (testFiles.length === 0) {
  console.error('No se encontraron pruebas (*.test.ts) en src/.');
  process.exit(1);
}

const result = spawnSync(
  process.execPath,
  ['--import', 'tsx', '--test', ...testFiles],
  { stdio: 'inherit', cwd: root },
);

process.exit(result.status ?? 1);
