#!/usr/bin/env node
/**
 * GTmetrix-like lab: Chrome CPU 4×, mobile/desktop viewport, Lighthouse-ish Perf weights.
 * Weights (GTmetrix Perf): TBT 30%, LCP 25%, CLS 25%, FCP 10%, SI 10%.
 * SI approximated as max(FCP, LCP*0.85) when SI unavailable (conservative).
 */
import puppeteer from 'puppeteer-core';

const URL = process.env.MS_URL || 'https://movasseghistore.com/';
const RUNS = Number(process.env.MS_RUNS || 3);
const VIEWPORT = process.env.MS_VIEWPORT || 'mobile';
const CPU = Number(process.env.MS_CPU || 4);
const WAIT_MS = Number(process.env.MS_WAIT_MS || 9000);

const vp = VIEWPORT === 'mobile'
  ? { width: 412, height: 823, deviceScaleFactor: 1.75, isMobile: true, hasTouch: true }
  : { width: 1366, height: 768, deviceScaleFactor: 1 };

// Approximate LH v10 scoring control points (p10 / median) for mobile.
function SCORE_INTERP(value, p10, median) {
  // log-normal CDF used by LH — simplified piecewise for our lab.
  if (value <= 0) return 1;
  const t = Math.log(value);
  const m = Math.log(median);
  const p = Math.log(p10);
  // approximate: score 0.5 at median, 0.9 at p10
  const sigma = (m - p) / 1.2816; // normsinv(0.9)≈1.28 for distance p10→median if p10 is better
  // For timing metrics lower is better: p10 < median
  const z = (t - m) / sigma;
  // standard normal CDF
  const cdf = 0.5 * (1 + erf(z / Math.SQRT2));
  return Math.max(0, Math.min(1, 1 - cdf));
}
function erf(x) {
  const sign = x < 0 ? -1 : 1;
  x = Math.abs(x);
  const a1=0.254829592,a2=-0.284496736,a3=1.421413741,a4=-1.453152027,a5=1.061405429,p=0.3275911;
  const t=1/(1+p*x);
  const y=1-(((((a5*t+a4)*t)+a3)*t+a2)*t+a1)*t*Math.exp(-x*x);
  return sign*y;
}
function scoreFcp(ms){return SCORE_INTERP(ms, 1800, 3000);}
function scoreLcp(ms){return SCORE_INTERP(ms, 2500, 4000);}
function scoreTbt(ms){return SCORE_INTERP(Math.max(ms,1), 200, 600);}
function scoreCls(v){return SCORE_INTERP(Math.max(v,0.0001), 0.1, 0.25);}
function scoreSi(ms){return SCORE_INTERP(ms, 2300, 4000);}

function approxPerf({FCP,LCP,TBT,CLS,SI}) {
  const parts = {
    FCP: scoreFcp(FCP),
    SI: scoreSi(SI ?? Math.max(FCP, LCP * 0.9)),
    LCP: scoreLcp(LCP),
    TBT: scoreTbt(TBT),
    CLS: scoreCls(CLS),
  };
  const score = 100 * (0.10*parts.FCP + 0.10*parts.SI + 0.25*parts.LCP + 0.30*parts.TBT + 0.25*parts.CLS);
  return { score: Math.round(score*10)/10, parts };
}

async function oneRun(i) {
  const browser = await puppeteer.launch({
    executablePath: '/usr/local/bin/google-chrome',
    headless: true,
    args: ['--no-sandbox','--disable-setuid-sandbox','--disable-dev-shm-usage',`--window-size=${vp.width},${vp.height}`],
  });
  try {
    const page = await browser.newPage();
    await page.setViewport(vp);
    const client = await page.createCDPSession();
    await client.send('Network.clearBrowserCache');
    await client.send('Network.setCacheDisabled', { cacheDisabled: true });
    await client.send('Emulation.setCPUThrottlingRate', { rate: CPU });
    await page.evaluateOnNewDocument(() => {
      window.__lt = [];
      try {
        new PerformanceObserver((list) => {
          for (const e of list.getEntries()) window.__lt.push({ start: e.startTime, duration: e.duration });
        }).observe({ type: 'longtask', buffered: true });
      } catch {}
      window.__shifts = [];
      try {
        new PerformanceObserver((list) => {
          for (const e of list.getEntries()) if (!e.hadRecentInput) window.__shifts.push(e.value);
        }).observe({ type: 'layout-shift', buffered: true });
      } catch {}
      window.__lcp = [];
      try {
        new PerformanceObserver((list) => {
          for (const e of list.getEntries()) window.__lcp.push(e.startTime);
        }).observe({ type: 'largest-contentful-paint', buffered: true });
      } catch {}
    });
    const t0 = Date.now();
    const resp = await page.goto(URL, { waitUntil: 'networkidle2', timeout: 90000 });
    await new Promise((r) => setTimeout(r, WAIT_MS));
    const metrics = await page.evaluate(() => {
      const nav = performance.getEntriesByType('navigation')[0];
      const paints = performance.getEntriesByType('paint');
      const fcp = paints.find((p) => p.name === 'first-contentful-paint')?.startTime ?? null;
      const lcpEntries = performance.getEntriesByType('largest-contentful-paint');
      const lcpObs = window.__lcp || [];
      const lcp = (lcpObs.length ? lcpObs[lcpObs.length-1] : null) ?? (lcpEntries.length ? lcpEntries[lcpEntries.length - 1].startTime : null);
      const styles = [...document.querySelectorAll('style')].reduce((n, s) => n + (s.textContent?.length || 0), 0);
      const snap = (document.documentElement.innerHTML.match(/ms-home-snap:([a-f0-9]+)/) || [])[1] || null;
      const TBT = (window.__lt || [])
        .filter((t) => t.start > (fcp || 0))
        .reduce((s, t) => s + Math.max(0, t.duration - 50), 0);
      const CLS = (window.__shifts || []).reduce((s, v) => s + v, 0);
      return {
        FCP: fcp, LCP: lcp, TBT, CLS,
        ttfb: nav?.responseStart ?? null,
        html: nav?.transferSize ?? null,
        inlineCss: styles,
        snap,
        idle: document.querySelectorAll('script[data-ms-idle]').length,
      };
    });
    const scored = approxPerf(metrics);
    const row = { i, status: resp?.status(), wall: Date.now()-t0, cpu: CPU, viewport: VIEWPORT, ...metrics, approxPerf: scored.score, parts: scored.parts };
    console.log(JSON.stringify(row));
    return row;
  } finally {
    await browser.close();
  }
}

const rows = [];
for (let i = 1; i <= RUNS; i++) rows.push(await oneRun(i));
const med = (k) => {
  const v = rows.map((r) => r[k]).filter((x) => typeof x === 'number').sort((a,b)=>a-b);
  if (!v.length) return null;
  const m = Math.floor(v.length/2);
  return v.length % 2 ? v[m] : (v[m-1]+v[m])/2;
};
const summary = {
  url: URL, cpu: CPU, viewport: VIEWPORT, runs: RUNS,
  medians: { FCP: med('FCP'), LCP: med('LCP'), TBT: med('TBT'), CLS: med('CLS'), ttfb: med('ttfb'), inlineCss: med('inlineCss'), approxPerf: med('approxPerf') },
  snap: rows.map(r=>r.snap),
};
console.log('SUMMARY', JSON.stringify(summary, null, 2));
