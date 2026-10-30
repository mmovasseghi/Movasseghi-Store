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
// Keep chrome/progress visible — never opacity:0 here. Bundle adds the full
// progress-fill/glow polish; critical only prevents dual-slide CLS flash.
const dualCriticalStabilize = `
.ms-product-media--dual{position:relative;overflow:hidden}
.ms-product-dual__aurora{position:absolute;inset:-30%;opacity:0;pointer-events:none;z-index:0}
.ms-product-dual__slides{position:relative;z-index:1;width:100%;height:100%;min-height:100%;display:grid;place-items:center}
.ms-product-dual__slide{grid-area:1/1;width:100%;height:100%;display:grid;place-items:center;opacity:0;pointer-events:none}
.ms-product-media--dual[data-ms-dual-phase=studio] .ms-product-dual__slide--studio,
.ms-product-media--dual[data-ms-dual-phase=scene] .ms-product-dual__slide--scene{opacity:1;pointer-events:auto;z-index:2}
.ms-product-dual__scene-img{position:absolute;inset:0;width:100%!important;height:100%!important;max-width:100%!important;max-height:100%!important;margin:0!important;object-fit:cover;border-radius:0}
.ms-product-dual__chrome{position:absolute;left:.55rem;right:.55rem;bottom:.5rem;z-index:5;display:flex;flex-direction:column;gap:.38rem;pointer-events:none}
.ms-product-dual__progress{position:relative;height:5px;border-radius:999px;overflow:visible;background:rgba(12,20,16,.28);direction:ltr}
.ms-product-dual__progress-fill{position:absolute;z-index:1;inset:0;display:block;height:100%;width:100%;border-radius:inherit;background:linear-gradient(90deg,#5fa883 0%,#b8f0d4 42%,#fff 72%,#8fd4b0 100%);transform:scaleX(var(--ms-dual-progress,0));transform-origin:left center}
.ms-product-dual__progress-glow{position:absolute;z-index:2;top:50%;left:calc(var(--ms-dual-progress,0)*100%);width:18px;height:18px;transform:translate(-50%,-50%);border-radius:50%;background:radial-gradient(circle,rgba(255,255,255,.95) 0%,rgba(184,240,212,.45) 45%,transparent 70%)}
.ms-product-dual__pill{opacity:0}
.ms-product-media--dual[data-ms-dual-phase=studio] .ms-product-dual__pill--studio,
.ms-product-media--dual[data-ms-dual-phase=scene] .ms-product-dual__pill--scene{opacity:1}
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
