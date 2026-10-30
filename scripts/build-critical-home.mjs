import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { transformSync } from 'esbuild';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const cssDir = path.join(root, 'src/MovasseghiShop.Web/wwwroot/css');
// FAB/ai-brief wait for the deferred full bundle — they are not LCP/FCP critical.
const criticalClass = /\.ms-(?:promo|top|logo|icon|burger|cart-badge|search|stories|story|hero|tab|wrap|body|main|app|page-home|skip|sr-only)(?:[-\s.:#>+~\[,{]|$)/;

// Tiny dual stabilizer only — full flip/aurora CSS stays in the async bundle so
// critical HTML stays lean for GTmetrix while above-the-fold cards do not flash
// both Studio+Scene slides (CLS) before storefront.bundle.css arrives.
const dualCriticalStabilize = `
.ms-product-media--dual{position:relative;overflow:hidden}
.ms-product-dual__aurora{position:absolute;inset:-30%;opacity:0;pointer-events:none;z-index:0}
.ms-product-dual__slides{position:relative;z-index:1;width:100%;height:100%;min-height:100%;display:grid;place-items:center}
.ms-product-dual__slide{grid-area:1/1;width:100%;height:100%;display:grid;place-items:center;opacity:0;pointer-events:none}
.ms-product-media--dual[data-ms-dual-phase=studio] .ms-product-dual__slide--studio,
.ms-product-media--dual[data-ms-dual-phase=scene] .ms-product-dual__slide--scene{opacity:1;pointer-events:auto;z-index:2}
.ms-product-dual__scene-img{position:absolute;inset:0;width:100%!important;height:100%!important;max-width:100%!important;max-height:100%!important;margin:0!important;object-fit:cover;border-radius:0}
.ms-product-dual__chrome{position:absolute;inset:auto 0 0 0;z-index:3;pointer-events:none;opacity:0}
`;

// Retain the original cascade, selector groups and responsive conditions.
// Scan braces lexically so comments and quoted CSS values cannot split rules.
function rules(css, { includeAllKeyframes = false } = {}) {
  const result = [];
  let start = 0;
  let depth = 0;
  let opening = -1;
  let quote = '';
  for (let i = 0; i < css.length; i++) {
    const c = css[i];
    if (quote) {
      if (c === '\\') i++;
      else if (c === quote) quote = '';
      continue;
    }
    if (c === '/' && css[i + 1] === '*') {
      const end = css.indexOf('*/', i + 2);
      if (end < 0) throw new Error('Unterminated CSS comment');
      i = end + 1;
      continue;
    }
    if (c === '"' || c === "'") { quote = c; continue; }
    if (c === '{') {
      if (depth++ === 0) opening = i;
    } else if (c === '}') {
      if (--depth < 0) throw new Error('Unbalanced CSS');
      if (depth === 0) {
        const header = css.slice(start, opening).replace(/\/\*[\s\S]*?\*\//g, '').trim();
        const body = css.slice(opening + 1, i);
        if (/^@(media|supports|layer)\b/.test(header)) {
          const children = rules(body, { includeAllKeyframes });
          if (children) result.push(`${header}{${children}}`);
        } else if (/^@font-face\b/.test(header)
          || (includeAllKeyframes && /^@(?:-webkit-)?keyframes\b/.test(header))
          || criticalClass.test(header)
          || (!header.startsWith('@') && !/[.#][a-zA-Z_-]/.test(header))) {
          result.push(`${header}{${body}}`);
        }
        start = i + 1;
      }
    } else if (c === ';' && depth === 0) {
      // No @import is permitted in the generated inline stylesheet.
      if (css.slice(start, i).trim().startsWith('@import')) throw new Error('Unexpected CSS import');
      start = i + 1;
    }
  }
  if (depth !== 0 || quote) throw new Error('Unbalanced CSS');
  return result.join('\n');
}

function extractKeyframes(css) {
  const map = new Map();
  let start = 0;
  let depth = 0;
  let opening = -1;
  let quote = '';
  for (let i = 0; i < css.length; i++) {
    const c = css[i];
    if (quote) {
      if (c === '\\') i++;
      else if (c === quote) quote = '';
      continue;
    }
    if (c === '/' && css[i + 1] === '*') {
      const end = css.indexOf('*/', i + 2);
      if (end < 0) throw new Error('Unterminated CSS comment');
      i = end + 1;
      continue;
    }
    if (c === '"' || c === "'") { quote = c; continue; }
    if (c === '{') {
      if (depth++ === 0) opening = i;
    } else if (c === '}') {
      if (--depth < 0) throw new Error('Unbalanced CSS');
      if (depth === 0) {
        const header = css.slice(start, opening).replace(/\/\*[\s\S]*?\*\//g, '').trim();
        const body = css.slice(opening + 1, i);
        const match = header.match(/^@(?:-webkit-)?keyframes\s+([\w-]+)/i);
        if (match) map.set(match[1], `${header}{${body}}`);
        start = i + 1;
      }
    } else if (c === ';' && depth === 0) {
      start = i + 1;
    }
  }
  return map;
}

const bundle = fs.readFileSync(path.join(cssDir, 'storefront.bundle.css'), 'utf8');
// First pass: above-the-fold rules only (no blanket keyframes — that pulled cart/modal noise into HTML).
const selected = rules(bundle, { includeAllKeyframes: false }).replaceAll('../fonts/', '/fonts/');
const needed = new Set();
for (const match of selected.matchAll(/animation(?:-name)?\s*:\s*([^;}{]+)/gi)) {
  for (const part of match[1].split(',')) {
    const name = part.trim().split(/\s+/)[0];
    if (name && name !== 'none' && !/^(inherit|initial|unset|var\()/i.test(name)) needed.add(name);
  }
}
const keyframes = extractKeyframes(bundle);
const keyframeBlock = [...needed]
  .filter((name) => keyframes.has(name))
  .map((name) => keyframes.get(name))
  .join('\n');
const { code, warnings } = transformSync(`${selected}\n${keyframeBlock}\n${dualCriticalStabilize}`, { loader: 'css', minify: true });
if (warnings.length) throw new Error(warnings.map(w => w.text).join('\n'));
fs.writeFileSync(path.join(cssDir, 'critical-home.css'), `/* Generated by npm run build:critical-home; do not edit. */\n${code}`);
console.log(`Critical home CSS: ${Buffer.byteLength(code)} bytes (${needed.size} keyframes + dual stabilize)`);
