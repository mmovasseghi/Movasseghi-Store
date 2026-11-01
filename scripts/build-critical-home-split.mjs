#!/usr/bin/env node
/**
 * EXPERIMENT ONLY — writes sidecars; does NOT replace production critical-home.css.
 *
 * Outputs:
 *  - critical-home-first.css
 *  - storefront-home-remainder.css
 *  - critical-home-complete.css (snapshot of full inline, if missing)
 *
 * Cycle critical-split measured: smaller head inline reduced HTML bytes but did not
 * improve FCP/LCP vs complete inline under matched cold navigations; publishing aborted.
 */
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { transformSync } from 'esbuild';
import { mapStyleRules } from './home-css-rules.mjs';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const cssDir = path.join(root, 'src/MovasseghiShop.Web/wwwroot/css');

const belowFold = /(?:^|[^\w-])(?:\.ms-offers|\.ms-offer-|\.ms-offer\b|\.ms-cat-|\.ms-cat\b|\.ms-featured|\.ms-feat-card|\.ms-segments|\.ms-seg-|\.ms-seg\b|\.ms-news|\.ms-closing|\.ms-home-faq|\.ms-faq\b|\.ms-footer|\.ms-spark|\.ms-section-wave|\.ms-countdown|\.ms-product\b|\.ms-trust-marquee|\.ms-trending|\.ms-trend-)/;
const isBelowFoldOnly = (header) => {
  const selectors = header.split(',').map((s) => s.trim()).filter(Boolean);
  return selectors.length > 0 && selectors.every((sel) => belowFold.test(sel));
};
const isContainScaffold = (body) => /content-visibility|contain-intrinsic-size/.test(body);

const raw = fs.readFileSync(path.join(cssDir, 'storefront-home.bundle.css'), 'utf8')
  .replaceAll('../fonts/', '/fonts/');
if (/@import\b|<\/style/i.test(raw)) throw new Error('Home CSS must be self-contained');

const firstSrc = mapStyleRules(
  raw,
  (header, body, original) => {
    if (!isBelowFoldOnly(header)) return `${original}{${body}}`;
    if (isContainScaffold(body)) return `${original}{${body}}`;
    return '';
  },
  (header, body, original) => `${original}{${body}}`,
);
const remSrc = mapStyleRules(
  raw,
  (header, body, original) => {
    if (!isBelowFoldOnly(header)) return '';
    if (isContainScaffold(body)) return '';
    return `${original}{${body}}`;
  },
  (header) => (/^@(?:font-face|keyframes)\b/i.test(header) ? '' : ''),
);

const first = transformSync(firstSrc, { loader: 'css', minify: true });
const rem = transformSync(remSrc, { loader: 'css', minify: true });
const full = transformSync(raw, { loader: 'css', minify: true });
if (first.warnings.length || rem.warnings.length) {
  throw new Error([...first.warnings, ...rem.warnings].map((w) => w.text).join('\n'));
}

fs.writeFileSync(path.join(cssDir, 'critical-home-first.css'), `/* Experiment sidecar; not used in production Layout. */\n${first.code}`);
fs.writeFileSync(path.join(cssDir, 'storefront-home-remainder.css'), `/* Experiment sidecar remainder. */\n${rem.code}`);
fs.writeFileSync(path.join(cssDir, 'critical-home-complete.css'), `/* Snapshot of complete home inline CSS. */\n${full.code}`);

console.log(JSON.stringify({
  mode: 'experiment-sidecars-only',
  firstBytes: Buffer.byteLength(first.code),
  remainderBytes: Buffer.byteLength(rem.code),
  completeBytes: Buffer.byteLength(full.code),
  note: 'Does not modify critical-home.css (production)',
}, null, 2));
