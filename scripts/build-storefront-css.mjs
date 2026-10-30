import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { transformSync } from 'esbuild';

const root = path.join(path.dirname(fileURLToPath(import.meta.url)), '..');
const cssDir = path.join(root, 'src/MovasseghiShop.Web/wwwroot/css');
const parts = [
  'site.css',
  'ux-mobile.css',
  'ux-desktop.css',
  'ux-cart.css',
  'ux-product-dual.css',
  'ux-scroll-reveal.css',
];

let out = `/* storefront.bundle.css — generated ${new Date().toISOString().slice(0, 10)} */\n`;
for (const file of parts) {
  const p = path.join(cssDir, file);
  out += `\n/* === ${file} === */\n`;
  out += fs.readFileSync(p, 'utf8');
  out += '\n';
}

const { code, warnings } = transformSync(out, { loader: 'css', minify: true });
if (warnings.length) throw new Error(warnings.map((w) => w.text).join('\n'));

const target = path.join(cssDir, 'storefront.bundle.css');
const banner = `/* storefront.bundle.css — generated ${new Date().toISOString().slice(0, 10)}; minified */\n`;
fs.writeFileSync(target, banner + code, 'utf8');
console.log(`Wrote ${target} (${(Buffer.byteLength(banner + code) / 1024).toFixed(1)} KB)`);
