#!/usr/bin/env node
/**
 * Fair A/B server for pre-built HTML variants.
 *
 * - Serves control.html.gz / experiment.html.gz from disk with Content-Encoding: gzip
 *   (gzip_comp_level 6 artifacts from prepare-ab-variants.mjs).
 * - Does NOT download origin HTML, rewrite, then send — HTML cost is only the variant.
 * - Proxies other assets to ORIGIN by streaming (pipe), preserving Accept-Encoding.
 * - Remainder CSS is pre-gzipped under /__ab/ (same cache: no-store as HTML).
 *
 * Usage:
 *   MODE=control|experiment PORT=9051 HTML_DIR=... node ab-prebuilt-server.mjs
 */
import http from 'http';
import https from 'https';
import fs from 'fs';
import path from 'path';
import zlib from 'zlib';
import { fileURLToPath } from 'url';

const MODE = process.env.MODE || 'control';
const PORT = Number(process.env.PORT || 9051);
const HOST = process.env.HOST || '127.0.0.1';
const ORIGIN = process.env.ORIGIN || 'https://185.93.88.81';
const ROOT = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const HTML_DIR = process.env.HTML_DIR || path.join(ROOT, 'docs/performance/reference/cycle-valid-ab/html');

if (!['control', 'experiment'].includes(MODE)) {
  console.error('MODE must be control|experiment');
  process.exit(1);
}

const htmlGzPath = path.join(HTML_DIR, `${MODE}.html.gz`);
const remGzPath = path.join(HTML_DIR, 'storefront-home-remainder.css.gz');
const remPath = path.join(HTML_DIR, 'storefront-home-remainder.css');
if (!fs.existsSync(htmlGzPath)) {
  console.error('Missing', htmlGzPath, '— run prepare-ab-variants.mjs first');
  process.exit(1);
}

const htmlGz = fs.readFileSync(htmlGzPath);
const remGz = fs.existsSync(remGzPath) ? fs.readFileSync(remGzPath) : null;
const remPlain = fs.existsSync(remPath) ? fs.readFileSync(remPath) : null;

function proxyAsset(req, res, targetPath) {
  const url = new URL(targetPath, ORIGIN);
  const lib = url.protocol === 'https:' ? https : http;
  const headers = {
    accept: req.headers.accept || '*/*',
    'accept-encoding': req.headers['accept-encoding'] || 'gzip, deflate, br',
    'user-agent': req.headers['user-agent'] || 'ab-prebuilt-server',
    host: url.host,
  };
  if (req.headers.range) headers.range = req.headers.range;

  const upstream = lib.request(url, {
    method: 'GET',
    headers,
    rejectUnauthorized: false,
  }, (up) => {
    const outHeaders = {};
    for (const [k, v] of Object.entries(up.headers)) {
      if (v == null) continue;
      const key = k.toLowerCase();
      if (['transfer-encoding', 'connection', 'keep-alive'].includes(key)) continue;
      outHeaders[k] = v;
    }
    outHeaders['cache-control'] = 'no-store';
    res.writeHead(up.statusCode || 502, outHeaders);
    up.pipe(res);
  });
  upstream.on('error', (e) => {
    if (!res.headersSent) res.writeHead(502, { 'content-type': 'text/plain' });
    res.end(String(e));
  });
  upstream.end();
}

const server = http.createServer((req, res) => {
  try {
    const u = new URL(req.url || '/', `http://127.0.0.1:${PORT}`);

    if (u.pathname === '/' || u.pathname === '') {
      const wantsGzip = /\bgzip\b/.test(String(req.headers['accept-encoding'] || ''));
      if (!wantsGzip) {
        const plain = zlib.gunzipSync(htmlGz);
        res.writeHead(200, {
          'content-type': 'text/html; charset=utf-8',
          'content-length': String(plain.length),
          'cache-control': 'no-store',
          'x-ab-mode': MODE,
          'x-ab-variant': 'prebuilt-plain-fallback',
        });
        res.end(plain);
        return;
      }
      res.writeHead(200, {
        'content-type': 'text/html; charset=utf-8',
        'content-encoding': 'gzip',
        'content-length': String(htmlGz.length),
        'cache-control': 'no-store',
        vary: 'Accept-Encoding',
        'x-ab-mode': MODE,
        'x-ab-variant': 'prebuilt-gzip6',
      });
      res.end(htmlGz);
      return;
    }

    if (u.pathname === '/__ab/storefront-home-remainder.css') {
      const wantsGzip = /\bgzip\b/.test(String(req.headers['accept-encoding'] || ''));
      if (wantsGzip && remGz) {
        res.writeHead(200, {
          'content-type': 'text/css; charset=utf-8',
          'content-encoding': 'gzip',
          'content-length': String(remGz.length),
          'cache-control': 'no-store',
          vary: 'Accept-Encoding',
        });
        res.end(remGz);
        return;
      }
      if (!remPlain) {
        res.writeHead(404).end('no remainder');
        return;
      }
      res.writeHead(200, {
        'content-type': 'text/css; charset=utf-8',
        'content-length': String(remPlain.length),
        'cache-control': 'no-store',
      });
      res.end(remPlain);
      return;
    }

    proxyAsset(req, res, u.pathname + u.search);
  } catch (e) {
    res.writeHead(500, { 'content-type': 'text/plain' });
    res.end(String(e));
  }
});

server.listen(PORT, HOST, () => {
  console.log(JSON.stringify({
    mode: MODE,
    host: HOST,
    port: PORT,
    origin: ORIGIN,
    htmlGzBytes: htmlGz.length,
    remGzBytes: remGz?.length ?? null,
    htmlPath: htmlGzPath,
    method: 'prebuilt-gzip6-disk + stream-proxy-assets',
  }));
});
