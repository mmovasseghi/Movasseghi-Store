#!/usr/bin/env node
/**
 * Reproducible cold navigation timeline for control vs experiment.
 * Always uses real page.goto navigation (never setContent / preloaded injection).
 *
 * Env:
 *   MS_URL          target URL (default https://185.93.88.81/)
 *   MS_LABEL        run label
 *   MS_RUNS         cold runs (default 3)
 *   MS_VIEWPORT     desktop|mobile (default desktop)
 *   MS_OUT          output dir
 *   MS_CLEAR_SERVER_CACHE  if 1, purge nginx page cache via SSH helper beforehand (caller)
 */
import puppeteer from 'puppeteer-core';
import fs from 'fs';
import path from 'path';

const URL = process.env.MS_URL || 'https://185.93.88.81/';
const LABEL = process.env.MS_LABEL || 'run';
const RUNS = Number(process.env.MS_RUNS || 3);
const VIEWPORT = process.env.MS_VIEWPORT || 'desktop';
const OUT = process.env.MS_OUT || '/opt/cursor/artifacts/cycle-critical-split/meas';
fs.mkdirSync(OUT, { recursive: true });

const vp = VIEWPORT === 'mobile'
  ? { width: 412, height: 823, deviceScaleFactor: 1.75 }
  : { width: 1366, height: 768, deviceScaleFactor: 1 };

const settings = {
  url: URL,
  label: LABEL,
  runs: RUNS,
  viewport: VIEWPORT,
  vp,
  chrome: '/usr/local/bin/google-chrome',
  method: 'puppeteer.page.goto cold navigation; Network.clearBrowserCache + setCacheDisabled; no setContent',
  createdAt: new Date().toISOString(),
};
fs.writeFileSync(path.join(OUT, `${LABEL}-settings.json`), JSON.stringify(settings, null, 2));

async function oneRun(i) {
  const browser = await puppeteer.launch({
    executablePath: settings.chrome,
    headless: true,
    args: ['--no-sandbox', '--disable-setuid-sandbox', '--disable-dev-shm-usage', `--window-size=${vp.width},${vp.height}`],
  });
  try {
    const page = await browser.newPage();
    await page.setViewport(vp);
    const client = await page.createCDPSession();
    await client.send('Network.clearBrowserCache');
    await client.send('Network.setCacheDisabled', { cacheDisabled: true });
    await client.send('Network.enable');
    await client.send('Performance.enable');

    const net = [];
    client.on('Network.responseReceived', (e) => {
      const u = e.response.url;
      if (!/185\.93\.88\.81|localhost|127\.0\.0\.1/.test(u) && !u.startsWith(URL)) return;
      net.push({
        requestId: e.requestId,
        url: u.split('?')[0],
        status: e.response.status,
        mime: e.response.mimeType,
        fromDiskCache: e.response.fromDiskCache,
        fromServiceWorker: e.response.fromServiceWorker,
        timing: e.response.timing,
      });
    });
    const finished = new Map();
    client.on('Network.loadingFinished', (e) => {
      finished.set(e.requestId, { encodedDataLength: e.encodedDataLength, timestamp: e.timestamp });
    });

    await page.evaluateOnNewDocument(() => {
      window.__msMarks = [];
      window.__cls = 0;
      window.__longTasks = [];
      try {
        new PerformanceObserver((list) => {
          for (const e of list.getEntries()) window.__cls += e.hadRecentInput ? 0 : e.value;
        }).observe({ type: 'layout-shift', buffered: true });
      } catch {}
      try {
        new PerformanceObserver((list) => {
          for (const e of list.getEntries()) {
            window.__longTasks.push({ start: e.startTime, duration: e.duration });
          }
        }).observe({ type: 'longtask', buffered: true });
      } catch {}
      try {
        new PerformanceObserver((list) => {
          for (const e of list.getEntries()) {
            window.__msMarks.push({ name: e.name, start: e.startTime, size: e.size, url: e.url });
          }
        }).observe({ type: 'largest-contentful-paint', buffered: true });
      } catch {}
    });

    // Start lightweight tracing for Style vs Layout before FCP
    await client.send('Tracing.start', {
      categories: '-*,devtools.timeline,disabled-by-default-devtools.timeline,loading',
      options: 'sampling-frequency=10000',
    });
    const events = [];
    client.on('Tracing.dataCollected', ({ value }) => events.push(...value));
    const traceDone = new Promise((r) => client.on('Tracing.tracingComplete', r));

    const tNav = Date.now();
    const resp = await page.goto(URL, { waitUntil: 'networkidle0', timeout: 120000 });
    await new Promise((r) => setTimeout(r, 1200));

    const metrics = await page.evaluate(() => {
      const nav = performance.getEntriesByType('navigation')[0];
      const paints = Object.fromEntries(performance.getEntriesByType('paint').map((p) => [p.name, p.startTime]));
      const lcp = (window.__msMarks || []).slice(-1)[0] || null;
      const resources = performance.getEntriesByType('resource').map((r) => ({
        name: r.name.replace(/\?.*$/, ''),
        initiator: r.initiatorType,
        start: r.startTime,
        responseStart: r.responseStart,
        responseEnd: r.responseEnd,
        transfer: r.transferSize,
        encoded: r.encodedBodySize,
        decoded: r.decodedBodySize,
      }));
      const inline = document.getElementById('msHomeInlineStyles');
      const rem = document.getElementById('msHomeRemainder');
      return {
        nav: nav && {
          type: nav.type,
          transferSize: nav.transferSize,
          encodedBodySize: nav.encodedBodySize,
          decodedBodySize: nav.decodedBodySize,
          // Navigation Timing (complete TTFB of document navigation)
          redirectStart: nav.redirectStart,
          redirectEnd: nav.redirectEnd,
          domainLookupStart: nav.domainLookupStart,
          domainLookupEnd: nav.domainLookupEnd,
          connectStart: nav.connectStart,
          secureConnectionStart: nav.secureConnectionStart,
          connectEnd: nav.connectEnd,
          requestStart: nav.requestStart,
          responseStart: nav.responseStart, // full navigation TTFB
          responseEnd: nav.responseEnd,     // HTML download end
          domInteractive: nav.domInteractive,
          domContentLoadedEventEnd: nav.domContentLoadedEventEnd,
          loadEventEnd: nav.loadEventEnd,
        },
        paints,
        FCP: paints['first-contentful-paint'],
        FP: paints['first-paint'],
        LCP: lcp?.start ?? null,
        lcpUrl: lcp?.url ?? null,
        CLS: window.__cls || 0,
        longTasks: window.__longTasks || [],
        inlineCssBytes: inline ? inline.textContent.length : 0,
        remainderLink: rem ? { href: rem.href, media: rem.media, failed: rem.dataset.msFailed || null } : null,
        currentSrc: document.querySelector('.ms-hero-slide.is-active .ms-hero-product img')?.currentSrc || null,
        resources,
      };
    });

    await client.send('Tracing.end');
    await traceDone;

    const fcp = metrics.FCP || 1e9;
    const navStartEv = events.find((e) => e.name === 'navigationStart' || e.name === 'FrameStartedLoading');
    const t0 = navStartEv ? navStartEv.ts : (events[0] && events[0].ts);
    const before = { Layout: 0, UpdateLayoutTree: 0, ParseAuthorStyleSheet: 0 };
    const after = { Layout: 0, UpdateLayoutTree: 0, ParseAuthorStyleSheet: 0 };
    let topLayout = null;
    for (const e of events) {
      if (!['Layout', 'UpdateLayoutTree', 'ParseAuthorStyleSheet'].includes(e.name)) continue;
      const dur = (e.dur || 0) / 1000;
      const start = (e.ts - t0) / 1000;
      const bucket = start < fcp ? before : after;
      bucket[e.name] = (bucket[e.name] || 0) + dur;
      if (e.name === 'Layout' && (!topLayout || dur > topLayout.dur)) {
        topLayout = {
          dur,
          start,
          dirty: e.args?.beginData?.dirtyObjects,
          total: e.args?.beginData?.totalObjects,
          partial: e.args?.beginData?.partialLayout,
          root: e.args?.endData?.layoutRoots?.[0]?.nodeName,
        };
      }
    }

    // Separate server-response-time style (waiting) from full navigation TTFB
    const docNet = net.find((n) => n.url.replace(/\/$/, '') === URL.replace(/\/$/, '') || /185\.93\.88\.81\/?$/.test(n.url));
    const docFinished = docNet && finished.get(docNet.requestId);
    const timing = docNet?.timing;
    // Chrome timing: requestTime is baseline; receiveHeadersEnd ≈ header TTFB from connection start of request
    const headerTTFB_ms = timing ? timing.receiveHeadersEnd : null; // relative to requestTime, not navigation
    const wait_ms = timing ? timing.sendEnd > 0 ? (timing.receiveHeadersEnd - timing.sendEnd) : null : null;

    const cssRes = metrics.resources.filter((r) => /storefront-home-remainder|critical-home|storefront-home\.bundle/.test(r.name));
    const fontRes = metrics.resources.filter((r) => /\.woff2/.test(r.name));
    const lcpRes = metrics.resources.find((r) => metrics.lcpUrl && r.name.includes(metrics.lcpUrl.replace(/^https?:\/\/[^/]+/, '').split('?')[0].split('/').pop() || '___'))
      || metrics.resources.find((r) => /display-v2-420w|01-lcp|01-384w/.test(r.name));

    const row = {
      label: `${LABEL}-${i}`,
      wallMs: Date.now() - tNav,
      httpStatus: resp?.status(),
      navigationTTFB_ms: metrics.nav?.responseStart ?? null, // full nav TTFB (includes DNS/TLS/queue)
      serverWaiting_ms: wait_ms, // approximate request→headers wait from Resource Timing internals
      headerReceiveEnd_relRequest_ms: headerTTFB_ms,
      htmlResponseEnd_ms: metrics.nav?.responseEnd ?? null,
      htmlTransfer: metrics.nav?.transferSize ?? null,
      htmlDecoded: metrics.nav?.decodedBodySize ?? null,
      FCP: metrics.FCP,
      FP: metrics.FP,
      LCP: metrics.LCP,
      lcpUrl: metrics.lcpUrl,
      CLS: metrics.CLS,
      TBT_approx: (metrics.longTasks || [])
        .filter((t) => t.start > (metrics.FCP || 0))
        .reduce((s, t) => s + Math.max(0, t.duration - 50), 0),
      longTasksBeforeFcp: (metrics.longTasks || []).filter((t) => t.start < (metrics.FCP || 0)),
      longTasksAfterFcp: (metrics.longTasks || []).filter((t) => t.start >= (metrics.FCP || 0)),
      styleLayoutBeforeFcp: before,
      styleLayoutAfterFcp: after,
      topLayout,
      inlineCssBytes: metrics.inlineCssBytes,
      remainderLink: metrics.remainderLink,
      currentSrc: metrics.currentSrc,
      cssResources: cssRes,
      fontResources: fontRes.map((r) => ({ name: r.name.slice(-40), start: r.start, responseEnd: r.responseEnd, transfer: r.transfer })),
      lcpResource: lcpRes || null,
      docFromCache: docNet ? { fromDiskCache: docNet.fromDiskCache, status: docNet.status, encoded: docFinished?.encodedDataLength } : null,
    };

    fs.writeFileSync(path.join(OUT, `${LABEL}-${i}.json`), JSON.stringify(row, null, 2));
    console.log(JSON.stringify({
      label: row.label,
      navTTFB: row.navigationTTFB_ms,
      htmlEnd: row.htmlResponseEnd_ms,
      htmlTransfer: row.htmlTransfer,
      inlineCss: row.inlineCssBytes,
      FCP: row.FCP,
      LCP: row.LCP,
      CLS: row.CLS,
      TBT_approx: row.TBT_approx,
      beforeLayout: row.styleLayoutBeforeFcp.Layout,
      beforeStyle: row.styleLayoutBeforeFcp.UpdateLayoutTree,
      dirty: row.topLayout?.dirty,
      remainderMedia: row.remainderLink?.media,
    }));
    return row;
  } finally {
    await browser.close();
  }
}

const rows = [];
for (let i = 1; i <= RUNS; i++) rows.push(await oneRun(i));
const summary = {
  settings,
  rows,
  median: (key) => {
    const vals = rows.map((r) => r[key]).filter((v) => typeof v === 'number').sort((a, b) => a - b);
    if (!vals.length) return null;
    const m = Math.floor(vals.length / 2);
    return vals.length % 2 ? vals[m] : (vals[m - 1] + vals[m]) / 2;
  },
};
summary.medians = {
  navigationTTFB_ms: summary.median('navigationTTFB_ms'),
  htmlResponseEnd_ms: summary.median('htmlResponseEnd_ms'),
  htmlTransfer: summary.median('htmlTransfer'),
  FCP: summary.median('FCP'),
  LCP: summary.median('LCP'),
  CLS: summary.median('CLS'),
  TBT_approx: summary.median('TBT_approx'),
};
fs.writeFileSync(path.join(OUT, `${LABEL}-summary.json`), JSON.stringify(summary, null, 2));
console.log('SUMMARY', JSON.stringify(summary.medians, null, 2));
