#!/usr/bin/env node
/**
 * Pre-build control/experiment HTML (+ gzip6) BEFORE any browser transfer.
 * Does NOT fetch-through-proxy-at-request-time; variants are static artifacts.
 *
 * Env:
 *   MS_HTML_SRC   path to live HTML snapshot (default: fetch ORIGIN once)
 *   ORIGIN        https://185.93.88.81
 *   MS_OUT        output dir
 *   MS_CSS_DIR    css dir with critical-home-*.css sidecars
 */
import fs from 'fs';
import path from 'path';
import https from 'https';
import http from 'http';
import zlib from 'zlib';
import { fileURLToPath } from 'url';

const ROOT = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const OUT = process.env.MS_OUT || path.join(ROOT, 'docs/performance/reference/cycle-valid-ab/html');
const CSS = process.env.MS_CSS_DIR || path.join(ROOT, 'src/MovasseghiShop.Web/wwwroot/css');
const ORIGIN = process.env.ORIGIN || 'https://185.93.88.81';
const GZIP_LEVEL = Number(process.env.MS_GZIP_LEVEL || 6);

fs.mkdirSync(OUT, { recursive: true });

function fetchText(url) {
  return new Promise((resolve, reject) => {
    const u = new URL(url);
    const lib = u.protocol === 'https:' ? https : http;
    const req = lib.request(u, {
      method: 'GET',
      headers: { 'accept-encoding': 'identity', accept: 'text/html', connection: 'close' },
      rejectUnauthorized: false,
    }, (res) => {
      const chunks = [];
      res.on('data', (c) => chunks.push(c));
      res.on('end', () => resolve(Buffer.concat(chunks).toString('utf8')));
    });
    req.on('error', reject);
    req.end();
  });
}

function stripCssComment(css) {
  return css.replace(/^\/\*[\s\S]*?\*\/\s*/, '');
}

function gzipFile(buf, dest) {
  const gz = zlib.gzipSync(buf, { level: GZIP_LEVEL });
  fs.writeFileSync(dest, gz);
  return gz.length;
}

const criticalComplete = stripCssComment(
  fs.readFileSync(path.join(CSS, 'critical-home-complete.css'), 'utf8'),
);
const criticalFirst = stripCssComment(
  fs.readFileSync(path.join(CSS, 'critical-home-first.css'), 'utf8'),
);
const remainder = stripCssComment(
  fs.readFileSync(path.join(CSS, 'storefront-home-remainder.css'), 'utf8'),
);

let html;
if (process.env.MS_HTML_SRC) {
  html = fs.readFileSync(process.env.MS_HTML_SRC, 'utf8');
} else {
  html = await fetchText(ORIGIN + '/');
}

const styleRe = /<style id="msHomeInlineStyles">[\s\S]*?<\/style>(?:\s*<link id="msHomeRemainder"[\s\S]*?<\/noscript>)?/;
if (!styleRe.test(html)) throw new Error('msHomeInlineStyles not found in HTML snapshot');

const remLink =
  `<link id="msHomeRemainder" rel="stylesheet" href="/__ab/storefront-home-remainder.css" media="print" onload="this.media='all';this.onload=null" onerror="this.dataset.msFailed='1'" />` +
  `<noscript><link rel="stylesheet" href="/__ab/storefront-home-remainder.css" /></noscript>`;

const controlHtml = html
  .replace(styleRe, `<style id="msHomeInlineStyles">${criticalComplete}</style>`);
const experimentHtml = html
  .replace(styleRe, `<style id="msHomeInlineStyles">${criticalFirst}</style>${remLink}`);

const controlBuf = Buffer.from(controlHtml, 'utf8');
const experimentBuf = Buffer.from(experimentHtml, 'utf8');
const remainderBuf = Buffer.from(remainder, 'utf8');

fs.writeFileSync(path.join(OUT, 'control.html'), controlBuf);
fs.writeFileSync(path.join(OUT, 'experiment.html'), experimentBuf);
fs.writeFileSync(path.join(OUT, 'storefront-home-remainder.css'), remainderBuf);

const meta = {
  createdAt: new Date().toISOString(),
  origin: ORIGIN,
  gzipLevel: GZIP_LEVEL,
  note: 'Variants built offline before transfer; servers must stream these files with Content-Encoding:gzip',
  control: {
    decoded: controlBuf.length,
    gzip: gzipFile(controlBuf, path.join(OUT, 'control.html.gz')),
    inlineCss: criticalComplete.length,
  },
  experiment: {
    decoded: experimentBuf.length,
    gzip: gzipFile(experimentBuf, path.join(OUT, 'experiment.html.gz')),
    inlineCss: criticalFirst.length,
    remainderDecoded: remainderBuf.length,
    remainderGzip: gzipFile(remainderBuf, path.join(OUT, 'storefront-home-remainder.css.gz')),
  },
};
meta.deltaGzipBytes = meta.control.gzip - meta.experiment.gzip;
fs.writeFileSync(path.join(OUT, 'variants-meta.json'), JSON.stringify(meta, null, 2));
console.log(JSON.stringify(meta, null, 2));
