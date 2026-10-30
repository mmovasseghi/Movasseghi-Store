import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

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

const target = path.join(cssDir, 'storefront.bundle.css');
fs.writeFileSync(target, out, 'utf8');
console.log(`Wrote ${target} (${(out.length / 1024).toFixed(1)} KB)`);
