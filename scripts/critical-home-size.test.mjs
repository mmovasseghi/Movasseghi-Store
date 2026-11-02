import { test } from 'node:test';
import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const critical = path.join(root, 'src/MovasseghiShop.Web/wwwroot/css/critical-home.css');

test('critical-home keeps versioned @font-face and stays under the TBT budget', () => {
  const built = spawnSync('node', ['scripts/build-critical-home.mjs'], {
    cwd: root,
    encoding: 'utf8',
  });
  assert.equal(built.status, 0, built.stderr || built.stdout);
  const css = fs.readFileSync(critical, 'utf8');
  assert.match(css, /@font-face/);
  assert.match(css, /\/fonts\/[^)'"]+\.woff2\?v=/);
  const bytes = Buffer.byteLength(css);
  assert.ok(bytes < 60_000, `inline critical too large for TBT window: ${bytes}`);
  assert.ok(bytes > 20_000, `inline critical suspiciously small: ${bytes}`);
});
