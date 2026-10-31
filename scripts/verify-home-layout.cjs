// Chrome trace regression: startup scripts must not synchronously lay out the
// whole homepage. This is a local rendering check, not a GTmetrix score.
const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const os = require('node:os');
const path = require('node:path');
(async () => {
  const browser = await chromium.launch({ executablePath: process.env.CHROMIUM_PATH || '/usr/bin/chromium', args: ['--no-sandbox'], ignoreDefaultArgs: ['--hide-scrollbars'] });
  const out = fs.mkdtempSync(path.join(os.tmpdir(), 'movasseghi-layout-'));
  try {
    const page = await browser.newPage({ viewport: { width: 1350, height: 940 } });
    const errors = []; page.on('pageerror', e => errors.push(e.message));
    await page.addInitScript(() => {
      window.__msStartupScripts = [];
      document.addEventListener('load', e => {
        if (e.target instanceof HTMLScriptElement) window.__msStartupScripts.push(e.target.src);
      }, true);
    });
    const client = await page.context().newCDPSession(page);
    await client.send('Emulation.setCPUThrottlingRate', { rate: 4 });
    const events = []; client.on('Tracing.dataCollected', data => events.push(...data.value));
    await client.send('Tracing.start', { categories: 'devtools.timeline', options: 'record-as-much-as-possible' });
    await page.goto(process.env.BASE_URL || 'http://127.0.0.1:5080/', { waitUntil: 'load' });
    await page.waitForFunction(() => typeof MsScrollReveal !== 'undefined' && window.__msStartupScripts.some(u => /\/site\.js(?:\?|$)/.test(u)));
    await page.evaluate(() => document.fonts.ready);
    const fontFaces = await page.evaluate(() => [...document.fonts].filter(f => f.family.replaceAll("'",'').replaceAll('"','') === 'Vazirmatn').length);
    await new Promise(async resolve => { client.once('Tracing.tracingComplete', resolve); await client.send('Tracing.end'); });
    const initializers = events.filter(e => e.ph === 'X' && e.name === 'EvaluateScript' && /\/(home|scroll-reveal|site)\.js(?:\?|$)/.test(e.args?.data?.url || ''));
    assert.ok(initializers.some(e => e.args.data.url.includes('/home.js')), 'trace must contain home initialization');
    assert.ok(initializers.some(e => e.args.data.url.includes('/scroll-reveal.js')), 'trace must contain reveal initialization');
    assert.ok(initializers.some(e => e.args.data.url.includes('/site.js')), 'trace must contain shared site initialization');
    const layouts = events.filter(e => e.ph === 'X' && e.name === 'Layout');
    const forced = initializers.flatMap(script => layouts.filter(e => e.tid === script.tid && e.ts >= script.ts && e.ts < script.ts + script.dur).map(e => ({ script: script.args.data.url, duration_ms: e.dur / 1000 })));
    const result = { fontFaces, cpuSlowdown: 4, forcedLayoutsDuringStartup: forced, initializerTimes: initializers.map(e => ({ url: e.args.data.url, ms: e.dur / 1000 })), errors };
    fs.writeFileSync(path.join(out, 'results.json'), JSON.stringify(result, null, 2));
    assert.deepEqual(errors, []);
    assert.equal(fontFaces, 2, 'register each full/subset font face once');
    assert.deepEqual(forced, [], 'startup must not force synchronous layout; inspect the trace');
    console.log(JSON.stringify({ forcedStartupLayouts: forced, out }));
  } finally { await browser.close(); }
})().catch(error => { console.error(error); process.exit(1); });
