#!/usr/bin/env node
/**
 * Local reverse proxy for fair cold-nav A/B:
 *  - control: HTML passthrough from live origin
 *  - experiment: replace #msHomeInlineStyles with split critical + remainder link
 * Both modes proxy all other assets to the same origin so network path is comparable.
 *
 * Usage:
 *   MODE=control|experiment PORT=9050 node ab-css-proxy.mjs
 */
import http from 'http';
import https from 'https';
import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';

const MODE = process.env.MODE || 'control';
const PORT = Number(process.env.PORT || 9050);
const ORIGIN = process.env.ORIGIN || 'https://185.93.88.81';
const CSS = process.env.MS_CSS_DIR || '/workspace/src/MovasseghiShop.Web/wwwroot/css';

const criticalSplit = fs.readFileSync(path.join(CSS, 'critical-home.css'), 'utf8');
const remainder = fs.readFileSync(path.join(CSS, 'storefront-home-remainder.css'), 'utf8');
const criticalComplete = fs.existsSync(path.join(CSS, 'critical-home-complete.css'))
  ? fs.readFileSync(path.join(CSS, 'critical-home-complete.css'), 'utf8')
  : criticalSplit;

function fetchOrigin(reqPath, headers = {}) {
  return new Promise((resolve, reject) => {
    const url = new URL(reqPath, ORIGIN);
    const lib = url.protocol === 'https:' ? https : http;
    const r = lib.request(url, {
      method: 'GET',
      headers: {
        ...headers,
        host: url.host,
        accept: headers.accept || '*/*',
        'accept-encoding': 'identity', // simplify rewrite
        connection: 'close',
      },
      rejectUnauthorized: false,
    }, (res) => {
      const chunks = [];
      res.on('data', (c) => chunks.push(c));
      res.on('end', () => resolve({
        status: res.statusCode,
        headers: res.headers,
        body: Buffer.concat(chunks),
      }));
    });
    r.on('error', reject);
    r.end();
  });
}

function rewriteHtml(html) {
  if (MODE === 'control') {
    // Ensure control uses complete inline CSS even if live already changed.
    return html.replace(
      /<style id="msHomeInlineStyles">[\s\S]*?<\/style>/,
      `<style id="msHomeInlineStyles">${criticalComplete}</style>`,
    ).replace(
      /<link id="msHomeRemainder"[^>]*>\s*<noscript>[\s\S]*?<\/noscript>/,
      '',
    );
  }
  // experiment: split critical + immediate remainder (local proxied)
  const remLink =
    `<link id="msHomeRemainder" rel="stylesheet" href="/__ab/storefront-home-remainder.css" media="print" onload="this.media='all';this.onload=null" onerror="this.dataset.msFailed='1'" />` +
    `<noscript><link rel="stylesheet" href="/__ab/storefront-home-remainder.css" /></noscript>`;
  let out = html.replace(
    /<style id="msHomeInlineStyles">[\s\S]*?<\/style>(?:\s*<link id="msHomeRemainder"[\s\S]*?<\/noscript>)?/,
    `<style id="msHomeInlineStyles">${criticalSplit}</style>${remLink}`,
  );
  return out;
}

const server = http.createServer(async (req, res) => {
  try {
    const u = new URL(req.url, `http://127.0.0.1:${PORT}`);
    if (u.pathname === '/__ab/storefront-home-remainder.css') {
      res.writeHead(200, {
        'content-type': 'text/css; charset=utf-8',
        'cache-control': 'no-store',
      });
      res.end(remainder);
      return;
    }
    const upstream = await fetchOrigin(u.pathname + u.search, {
      accept: req.headers.accept,
      'user-agent': req.headers['user-agent'],
    });
    const ctype = String(upstream.headers['content-type'] || '');
    let body = upstream.body;
    const headers = { ...upstream.headers };
    delete headers['content-encoding'];
    delete headers['content-length'];
    delete headers['transfer-encoding'];
    headers['cache-control'] = 'no-store';

    if (ctype.includes('text/html') && (u.pathname === '/' || u.pathname === '')) {
      const html = rewriteHtml(body.toString('utf8'));
      body = Buffer.from(html, 'utf8');
      headers['content-type'] = 'text/html; charset=utf-8';
    }
    headers['content-length'] = String(body.length);
    res.writeHead(upstream.status || 200, headers);
    res.end(body);
  } catch (e) {
    res.writeHead(502, { 'content-type': 'text/plain' });
    res.end(String(e));
  }
});

server.listen(PORT, '127.0.0.1', () => {
  console.log(JSON.stringify({ mode: MODE, port: PORT, origin: ORIGIN, criticalSplit: criticalSplit.length, remainder: remainder.length, criticalComplete: criticalComplete.length }));
});
