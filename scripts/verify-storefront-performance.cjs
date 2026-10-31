// Browser integration checks. Local results are not GTmetrix grades.
const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const os = require('node:os');
const path = require('node:path');
const base = process.env.BASE_URL || 'http://127.0.0.1:5274';
const output = fs.mkdtempSync(path.join(os.tmpdir(), 'movasseghi-storefront-'));
(async () => {
  const browser = await chromium.launch({ executablePath: process.env.CHROMIUM_PATH || '/usr/bin/chromium', args: ['--no-sandbox'] });
  const results = [];
  try {
    const page = await browser.newPage({ viewport: { width: 1366, height: 768 } });
    const errors = [];
    page.on('pageerror', e => errors.push({ url: page.url(), message: e.message, stack: e.stack }));
    await page.goto(base + '/', { waitUntil: 'load' });
    const assets = await page.evaluate(() => {
      const hero = document.querySelector('.ms-hero-slide img');
      const preloads = [...document.querySelectorAll('link[rel=preload][as=image]')].map(e => e.href);
      const fonts = performance.getEntriesByType('resource').filter(e => e.name.includes('Vazirmatn-var.woff2')).map(e => e.name);
      const badPaintReferences = [...document.querySelectorAll('svg [fill],svg [stroke],svg [filter]')].flatMap(e => [...e.attributes].flatMap(a => [...a.value.matchAll(/url\(#([^)]*)\)/g)].map(m => m[1]))).filter(id => !document.getElementById(id));
      return { hero: hero.currentSrc, heroFallback: hero.src, heroSrcSet: hero.srcset, heroSizes: hero.sizes, preloadSrcSet: document.querySelector('link[rel=preload][as=image]').imageSrcset, preloadSizes: document.querySelector('link[rel=preload][as=image]').imageSizes, resources: performance.getEntriesByType('resource').map(e => e.name), preloads, fonts, badPaintReferences, dom: document.querySelectorAll('*').length,
        heroImages: [...document.querySelectorAll('.ms-hero-slide img')].map(e => ({ loading: e.loading, priority: e.fetchPriority })),
        logo: document.querySelector('.ms-logo').currentSrc, favicon: document.querySelector('link[rel=icon]').href,
        routes: [...document.querySelectorAll('a[href]')].map(e => e.getAttribute('href')).filter(h => /^\/(Shop\/Product|Blog\/Post|News\/Article)\//.test(h)) };
    });
    assert.ok(assets.preloads.includes(assets.heroFallback), 'fallback preload must match the img src');
    assert.equal(assets.preloadSrcSet, assets.heroSrcSet, 'responsive preload and image must share candidates');
    assert.equal(assets.preloadSizes, assets.heroSizes);
    if (assets.heroSrcSet && assets.hero !== assets.heroFallback) assert.ok(!assets.resources.includes(assets.heroFallback), 'do not download fallback as well as the responsive LCP');
    assert.ok(assets.hero.includes('?v='));
    assert.equal(assets.fonts.length, 1, 'font preload and CSS must share one URL');
    assert.ok(assets.fonts[0].includes('?v='));
    assert.ok(new URL(assets.logo).pathname.includes('originallogo'));
    assert.ok(new URL(assets.favicon).pathname.includes('originallogo'));
    assert.deepEqual(assets.badPaintReferences, [], 'shared SVG paint IDs must resolve');
    assert.ok(assets.heroImages.every(e => e.loading === 'eager'));
    assert.equal(assets.heroImages[0].priority, 'high');
    assert.ok(assets.heroImages.slice(1).every(e => e.priority === 'low'));
    results.push({ homeAssets: assets });
    const routes = ['/', '/Catalog', '/Blog', '/News', '/Page/About', '/Page/Faq', '/Cart', ...['/Shop/Product/', '/Blog/Post/', '/News/Article/'].map(prefix => assets.routes.find(h => h.startsWith(prefix))).filter(Boolean)];
    for (const route of routes) {
      const response = await page.goto(base + route, { waitUntil: 'load' });
      assert.equal(response.status(), 200, route);
      assert.ok(await page.locator('main').textContent(), route + ': missing SSR content');
      if (route.startsWith('/Shop/Product/')) {
        const first = page.locator('[data-pd-slide]').first().locator('img');
        assert.equal(await first.getAttribute('loading'), 'eager', 'first product gallery image must not be lazy');
        assert.equal(await first.getAttribute('fetchpriority'), 'high');
        await first.evaluate(image => image.decode());
      }
      assert.equal(await page.evaluate(() => document.documentElement.scrollWidth > innerWidth), false, route + ': overflow');
    }
    assert.deepEqual(errors, []);
    results.push({ publicPages: routes.length, errors });
    await page.close();

    const retry = await browser.newPage({ viewport: { width: 390, height: 844 } });
    let attempts = 0;
    let failed;
    const failure = new Promise(resolve => { failed = resolve; });
    await retry.route('**/js/site.js*', async route => {
      if (++attempts === 1) { await route.abort('failed'); failed(); }
      else await route.continue();
    });
    await retry.goto(base + '/', { waitUntil: 'load' });
    await failure;
    await retry.waitForTimeout(100);
    await retry.locator('[data-menu-toggle]').click();
    await retry.locator('#mobileNav').waitFor({ state: 'visible' });
    // A successful script must not be injected again after another script failed.
    assert.equal(await retry.locator('body > script:not([type="text/plain"])[src*="/js/auth.js"]').count(), 1);
    await retry.locator('button[data-menu-close]').click();
    await retry.locator('#mobileNav').waitFor({ state: 'hidden' });
    assert.ok(attempts >= 2);
    results.push({ scriptFailureRecovered: true, attempts });
    await retry.close();

    const dual = await browser.newPage({ viewport: { width: 500, height: 400 } });
    const card = i => `<div class="ms-product-media--dual" data-ms-product-dual data-ms-dual-phase="studio" style="width:220px;height:120px;margin-bottom:80px"><span class="ms-product-dual__slides"><span class="ms-product-dual__slide ms-product-dual__slide--studio">Studio ${i}</span><span class="ms-product-dual__slide ms-product-dual__slide--scene">Scene ${i}</span></span><span class="ms-product-dual__chrome"><span class="ms-product-dual__progress"><span class="ms-product-dual__progress-fill"></span><span class="ms-product-dual__progress-glow"></span></span></span></div>`;
    await dual.goto(base + '/');
    await dual.setContent(`<link rel="stylesheet" href="${base}/css/storefront.bundle.css"><style>body{margin:0;overflow:visible}html{overflow:visible}</style>${card(1)}${card(2)}<div style="height:1600px"></div><script src="${base}/js/product-card-dual.js"></script>`);
    const clock = () => dual.evaluate(() => { const e = document.querySelectorAll('.ms-product-dual__progress-fill')[1]; const a = e.getAnimations()[0]; return { time: a?.currentTime, state: a?.playState, phase: e.closest('[data-ms-product-dual]').dataset.msDualPhase }; });
    await dual.waitForTimeout(500);
    assert.equal((await clock()).state, 'running');
    await dual.evaluate(() => scrollTo(0, 260));
    await dual.waitForTimeout(300);
    const first = await clock();
    await dual.waitForTimeout(300);
    const second = await clock();
    assert.equal(second.state, 'running', 'one departing card must not stop another visible card');
    assert.ok(second.time > first.time + 150);
    await dual.evaluate(() => scrollTo(0, 900));
    await dual.waitForTimeout(300);
    assert.equal((await clock()).state, 'paused');
    await dual.evaluate(() => scrollTo(0, 0));
    await dual.waitForTimeout(6500);
    const resumed = await clock();
    assert.equal(resumed.state, 'running', 'scrolling back must resume the animation');
    assert.equal(resumed.phase, 'scene', 'the scene image transition must still run');
    assert.equal(await dual.evaluate(() => document.documentElement.style.getPropertyValue('--ms-dual-progress')), '');
    results.push({ dualCycle: 'phase, partial visibility, pause and resume passed' });
    await dual.close();
    fs.writeFileSync(path.join(output, 'results.json'), JSON.stringify(results, null, 2));
    console.log(JSON.stringify({ checks: results.map(r => Object.keys(r)[0]), output }, null, 2));
  } finally { await browser.close(); }
})().catch(e => { console.error(e); process.exit(1); });
