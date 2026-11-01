#!/usr/bin/env node
/**
 * Compare three paths to the same app (cold + warm), with TLS verify on:
 *   A) public CDN DNS for movasseghistore.com
 *   B) same hostname/SNI/Host/URL, TCP to origin via --resolve equivalent
 *   C) direct IP URL (secondary control only)
 *
 * Records DNS/TCP/TLS/TTFB/HTML body + browser FCP/LCP/SI (when Chrome available).
 * Does not invent missing metrics.
 */
import { spawnSync } from 'node:child_process';
import { writeFileSync, mkdirSync } from 'node:fs';
import { resolve } from 'node:path';
import { performance } from 'node:perf_hooks';

const DOMAIN = process.env.MS_DOMAIN || 'movasseghistore.com';
const ORIGIN_IP = process.env.MS_ORIGIN_IP || '185.93.88.81';
const OUT_DIR = resolve(process.env.MS_OUT || 'docs/performance/reference/domain-path-compare');
const RUNS = Number(process.env.MS_RUNS || 3);

function sh(cmd, args, opts = {}) {
  const r = spawnSync(cmd, args, { encoding: 'utf8', maxBuffer: 20e6, ...opts });
  return { code: r.status ?? 1, stdout: r.stdout || '', stderr: r.stderr || '' };
}

function digAll(host) {
  const a = sh('dig', ['+short', 'A', host]).stdout.trim().split(/\n/).filter(Boolean);
  const aaaa = sh('dig', ['+short', 'AAAA', host]).stdout.trim().split(/\n/).filter(Boolean);
  return { A: a, AAAA: aaaa };
}

/** curl timing for one URL; optional resolve host:443:ip keeps SNI/Host = host */
function curlTimings(url, { resolveHost = null, insecure = false, reuse = false } = {}) {
  const args = [
    '-sS', '-L', '--max-redirs', '5', '--max-time', '45',
    '-o', '/dev/null',
    '-w',
    [
      'http_code:%{http_code}',
      'url_effective:%{url_effective}',
      'num_redirects:%{num_redirects}',
      'remote_ip:%{remote_ip}',
      'namelookup:%{time_namelookup}',
      'connect:%{time_connect}',
      'appconnect:%{time_appconnect}',
      'pretransfer:%{time_pretransfer}',
      'starttransfer:%{time_starttransfer}',
      'total:%{time_total}',
      'size_download:%{size_download}',
      'speed_download:%{speed_download}',
      'ssl_verify_result:%{ssl_verify_result}',
    ].join('\\n') + '\\n',
  ];
  if (resolveHost) args.push('--resolve', resolveHost);
  if (insecure) args.push('-k');
  // Fresh connection each cold run; for warm reuse we still open new process but can use keepalive within one curl multi — use second request in same process via -::
  if (reuse) {
    // Two GETs in one curl process to observe connection reuse within keepalive.
    args.push(url, url);
  } else {
    args.push('--http2', url);
  }
  const started = performance.now();
  const r = sh('curl', args);
  const elapsed = performance.now() - started;
  const blocks = r.stdout.trim().split(/\n(?=http_code:)/);
  const parse = (block) => {
    const o = {};
    for (const line of block.split('\n')) {
      const i = line.indexOf(':');
      if (i > 0) o[line.slice(0, i)] = line.slice(i + 1);
    }
    const num = (k) => (o[k] !== undefined && o[k] !== '' ? Number(o[k]) : null);
    return {
      http_code: o.http_code || null,
      url_effective: o.url_effective || null,
      num_redirects: num('num_redirects'),
      remote_ip: o.remote_ip || null,
      namelookup_s: num('namelookup'),
      connect_s: num('connect'),
      appconnect_s: num('appconnect'),
      pretransfer_s: num('pretransfer'),
      starttransfer_s: num('starttransfer'),
      total_s: num('total'),
      size_download: num('size_download'),
      ssl_verify_result: num('ssl_verify_result'),
      // Derived phases (seconds)
      dns_s: num('namelookup'),
      tcp_s: num('connect') != null && num('namelookup') != null ? num('connect') - num('namelookup') : null,
      tls_s: num('appconnect') != null && num('connect') != null ? num('appconnect') - num('connect') : null,
      ttfb_after_tls_s:
        num('starttransfer') != null && num('appconnect') != null
          ? num('starttransfer') - num('appconnect')
          : null,
      html_after_ttfb_s:
        num('total') != null && num('starttransfer') != null ? num('total') - num('starttransfer') : null,
    };
  };
  return {
    ok: r.code === 0,
    code: r.code,
    stderr: r.stderr.slice(0, 500),
    wall_ms: Math.round(elapsed),
    cold: parse(blocks[0] || ''),
    warm_reuse: blocks[1] ? parse(blocks[1]) : null,
  };
}

function headers(url, { resolveHost = null } = {}) {
  const args = ['-sSI', '--max-time', '30', '--max-redirs', '0'];
  if (resolveHost) args.push('--resolve', resolveHost);
  args.push(url);
  const r = sh('curl', args);
  return { code: r.code, raw: r.stdout, stderr: r.stderr.slice(0, 300) };
}

async function browserMetrics(url, { hostResolverRules = null } = {}) {
  let puppeteer;
  try {
    puppeteer = await import('puppeteer');
  } catch {
    return { skipped: true, reason: 'puppeteer not installed' };
  }
  const args = ['--no-sandbox', '--disable-dev-shm-usage'];
  if (hostResolverRules) args.push(`--host-resolver-rules=${hostResolverRules}`);
  const browser = await puppeteer.default.launch({
    headless: true,
    args,
  });
  try {
    const page = await browser.newPage();
    await page.setCacheEnabled(false);
    const client = await page.createCDPSession();
    await client.send('Network.clearBrowserCache');
    const started = Date.now();
    const response = await page.goto(url, { waitUntil: 'networkidle2', timeout: 90000 });
    const nav = await page.evaluate(() => {
      const nav = performance.getEntriesByType('navigation')[0];
      const paints = performance.getEntriesByType('paint');
      const lcpEntries = performance.getEntriesByType('largest-contentful-paint');
      // SI is not a Performance API entry — leave null unless computed elsewhere.
      return {
        responseStatus: null,
        ttfb: nav ? nav.responseStart : null,
        responseEnd: nav ? nav.responseEnd : null,
        domContentLoaded: nav ? nav.domContentLoadedEventEnd : null,
        loadEventEnd: nav ? nav.loadEventEnd : null,
        transferSize: nav ? nav.transferSize : null,
        encodedBodySize: nav ? nav.encodedBodySize : null,
        decodedBodySize: nav ? nav.decodedBodySize : null,
        fp: paints.find((p) => p.name === 'first-paint')?.startTime ?? null,
        fcp: paints.find((p) => p.name === 'first-contentful-paint')?.startTime ?? null,
        lcp: lcpEntries.length ? lcpEntries[lcpEntries.length - 1].startTime : null,
        lcpSize: lcpEntries.length ? lcpEntries[lcpEntries.length - 1].size : null,
        lcpUrl: lcpEntries.length ? lcpEntries[lcpEntries.length - 1].url || null : null,
        redirectCount: nav ? nav.redirectCount : null,
      };
    });
    // Wait briefly for late LCP
    await new Promise((r) => setTimeout(r, 1500));
    const lcp2 = await page.evaluate(() => {
      const lcpEntries = performance.getEntriesByType('largest-contentful-paint');
      return lcpEntries.length
        ? {
            lcp: lcpEntries[lcpEntries.length - 1].startTime,
            lcpSize: lcpEntries[lcpEntries.length - 1].size,
            lcpUrl: lcpEntries[lcpEntries.length - 1].url || null,
          }
        : null;
    });
    return {
      skipped: false,
      wall_ms: Date.now() - started,
      http_status: response?.status() ?? null,
      remote: response?.remoteAddress?.() ?? null,
      security: response?.securityDetails?.()
        ? {
            subjectName: response.securityDetails().subjectName(),
            issuer: response.securityDetails().issuer(),
            protocol: response.securityDetails().protocol(),
            validFrom: response.securityDetails().validFrom(),
            validTo: response.securityDetails().validTo(),
          }
        : null,
      nav,
      lcp_final: lcp2,
      // Speed Index is not available from Performance API alone.
      speed_index: null,
    };
  } finally {
    await browser.close();
  }
}

function median(nums) {
  const a = nums.filter((n) => typeof n === 'number' && !Number.isNaN(n)).sort((x, y) => x - y);
  if (!a.length) return null;
  const m = Math.floor(a.length / 2);
  return a.length % 2 ? a[m] : (a[m - 1] + a[m]) / 2;
}

async function main() {
  mkdirSync(OUT_DIR, { recursive: true });
  const dns = digAll(DOMAIN);
  const paths = {
    A_cdn: {
      label: 'A public CDN DNS',
      url: `https://${DOMAIN}/`,
      resolveHost: null,
      browserRules: null,
    },
    B_origin_sni: {
      label: 'B domain hostname/SNI → origin IP',
      url: `https://${DOMAIN}/`,
      resolveHost: `${DOMAIN}:443:${ORIGIN_IP}`,
      browserRules: `MAP ${DOMAIN} ${ORIGIN_IP}`,
    },
    C_ip: {
      label: 'C direct IP (secondary control)',
      url: `https://${ORIGIN_IP}/`,
      resolveHost: null,
      // IP cert SAN is the IP; verify stays on.
      browserRules: null,
      // curl against IP URL uses IP SNI by default
    },
  };

  const report = {
    generated_utc: new Date().toISOString(),
    domain: DOMAIN,
    origin_ip: ORIGIN_IP,
    dns,
    notes: [
      'TLS verification enabled (no -k) for A/B/C.',
      'B keeps URL/Host/SNI as domain; only connect IP forced via --resolve / host-resolver-rules.',
      'Speed Index is null here (not in Performance API); do not invent SI.',
      'First Paint ≠ FCP; both recorded when present.',
    ],
    paths: {},
  };

  for (const [key, cfg] of Object.entries(paths)) {
    const curlCold = [];
    const curlWarm = [];
    const hdrs = [];
    const browser = [];
    for (let i = 0; i < RUNS; i++) {
      curlCold.push(curlTimings(cfg.url, { resolveHost: cfg.resolveHost, reuse: false }));
      curlWarm.push(curlTimings(cfg.url, { resolveHost: cfg.resolveHost, reuse: true }));
      hdrs.push(headers(cfg.url, { resolveHost: cfg.resolveHost }));
      try {
        browser.push(await browserMetrics(cfg.url, { hostResolverRules: cfg.browserRules }));
      } catch (e) {
        browser.push({ skipped: false, error: String(e) });
      }
    }
    const pick = (rows, sel) => rows.map(sel);
    report.paths[key] = {
      label: cfg.label,
      url: cfg.url,
      resolveHost: cfg.resolveHost,
      curl_cold: curlCold,
      curl_warm_keepalive: curlWarm,
      headers_samples: hdrs.map((h) => ({
        code: h.code,
        head: h.raw.split('\n').slice(0, 25).join('\n'),
      })),
      browser,
      medians: {
        cold_dns_s: median(pick(curlCold, (r) => r.cold?.dns_s)),
        cold_tcp_s: median(pick(curlCold, (r) => r.cold?.tcp_s)),
        cold_tls_s: median(pick(curlCold, (r) => r.cold?.tls_s)),
        cold_ttfb_s: median(pick(curlCold, (r) => r.cold?.starttransfer_s)),
        cold_ttfb_after_tls_s: median(pick(curlCold, (r) => r.cold?.ttfb_after_tls_s)),
        cold_html_after_ttfb_s: median(pick(curlCold, (r) => r.cold?.html_after_ttfb_s)),
        cold_total_s: median(pick(curlCold, (r) => r.cold?.total_s)),
        warm_ttfb_s: median(pick(curlWarm, (r) => r.warm_reuse?.starttransfer_s)),
        browser_fcp_ms: median(pick(browser, (r) => r.nav?.fcp)),
        browser_fp_ms: median(pick(browser, (r) => r.nav?.fp)),
        browser_lcp_ms: median(pick(browser, (r) => r.lcp_final?.lcp ?? r.nav?.lcp)),
        browser_ttfb_ms: median(pick(browser, (r) => r.nav?.ttfb)),
        browser_si_ms: null,
      },
    };
  }

  const outJson = resolve(OUT_DIR, `compare-${Date.now()}.json`);
  writeFileSync(outJson, JSON.stringify(report, null, 2));
  writeFileSync(resolve(OUT_DIR, 'latest.json'), JSON.stringify(report, null, 2));

  // Human table
  const lines = ['path | dns | tcp | tls | ttfb | ttfb_after_tls | html | total | fcp_ms | lcp_ms | remote'];
  for (const [key, p] of Object.entries(report.paths)) {
    const m = p.medians;
    const remote = p.curl_cold[0]?.cold?.remote_ip;
    lines.push(
      [
        key,
        m.cold_dns_s?.toFixed(3),
        m.cold_tcp_s?.toFixed(3),
        m.cold_tls_s?.toFixed(3),
        m.cold_ttfb_s?.toFixed(3),
        m.cold_ttfb_after_tls_s?.toFixed(3),
        m.cold_html_after_ttfb_s?.toFixed(3),
        m.cold_total_s?.toFixed(3),
        m.browser_fcp_ms?.toFixed?.(0) ?? m.browser_fcp_ms,
        m.browser_lcp_ms?.toFixed?.(0) ?? m.browser_lcp_ms,
        remote,
      ].join(' | '),
    );
  }
  const table = lines.join('\n');
  writeFileSync(resolve(OUT_DIR, 'latest-table.txt'), table + '\n');
  console.log(JSON.stringify({ outJson, dns, table }, null, 2));
}

main().catch((e) => {
  console.error(e);
  process.exit(1);
});
