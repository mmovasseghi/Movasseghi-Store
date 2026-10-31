// Integration check: no trailing blank or loop seam at any sampled animation phase.
const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const os = require('node:os');
const path = require('node:path');
const base = process.env.BASE_URL || 'http://127.0.0.1:5326';
const out = fs.mkdtempSync(path.join(os.tmpdir(), 'movasseghi-marquee-'));
(async () => {
  const browser = await chromium.launch({ executablePath: process.env.CHROMIUM_PATH || '/usr/bin/chromium', args: ['--no-sandbox'] });
  const rows = [];
  try {
    const page = await browser.newPage({ viewport: { width: 390, height: 844 } });
    const errors = [];
    page.on('pageerror', error => errors.push(error.message));
    await page.goto(base + '/', { waitUntil: 'load' });
    await page.evaluate(() => document.fonts.ready);
    assert.equal(await page.locator('.ms-hero-trust-set').count(), 2, 'a seamless loop needs two complete halves');
    const track = page.locator('.ms-hero-trust-marquee');
    const before = await track.evaluate(e => getComputedStyle(e).transform);
    await page.waitForTimeout(250);
    assert.notEqual(await track.evaluate(e => getComputedStyle(e).transform), before, 'strip must actually move');
    for (const width of [320, 390, 768, 1024, 1366, 1920, 390]) {
      await page.setViewportSize({ width, height: 844 });
      await page.waitForFunction(() => {
        const viewport = document.querySelector('.ms-hero-trust-track');
        return document.querySelector('.ms-hero-trust-set').getBoundingClientRect().width >= viewport.clientWidth;
      });
      // Allow the observer to shrink the copies after a desktop-to-mobile resize.
      await page.evaluate(() => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve))));
      const groups = await page.locator('.ms-hero-trust-set').evaluateAll(es => es.map(e => ({
        width: e.getBoundingClientRect().width,
        text: [...e.children].map(item => item.textContent.trim())
      })));
      assert.equal(groups.length, 2);
      assert.deepEqual(groups[0].text, groups[1].text, 'both halves must be identical');
      assert.ok(Math.abs(groups[0].width - groups[1].width) < 1, 'equal loop distances');
      for (const phase of [0, .125, .25, .5, .75, .875, .9999, 1]) {
        const gaps = await track.evaluate(async (e, phase) => {
          const animation = e.getAnimations().find(a => a.animationName === 'msHeroTrustMarquee');
          if (!animation) throw new Error('Missing marquee animation');
          animation.pause();
          animation.currentTime = phase * Number(animation.effect.getTiming().duration);
          await new Promise(resolve => requestAnimationFrame(resolve));
          const viewport = e.parentElement.getBoundingClientRect();
          const pills = [...e.querySelectorAll('.ms-hero-trust-item')].map(p => p.getBoundingClientRect())
            .filter(r => r.right > viewport.left && r.left < viewport.right).sort((a, b) => a.left - b.left);
          let end = viewport.left;
          const spaces = [];
          for (const r of pills) {
            spaces.push(Math.max(0, r.left - end));
            end = Math.max(end, Math.min(viewport.right, r.right));
          }
          spaces.push(viewport.right - end);
          return { maxGap: Math.max(...spaces), visible: pills.length };
        }, phase);
        assert.ok(gaps.visible > 0, `empty strip at ${width}px / ${phase}`);
        assert.ok(gaps.maxGap < 40, `blank ${gaps.maxGap}px at ${width}px / ${phase}`);
        rows.push({ width, phase, copiesPerHalf: groups[0].text.length, ...gaps });
      }
    }
    await page.emulateMedia({ reducedMotion: 'reduce' });
    await page.waitForFunction(() => getComputedStyle(document.querySelector('.ms-hero-trust-marquee')).animationName === 'none');
    await page.waitForTimeout(100);
    assert.equal(await page.locator('.ms-hero-trust-set').nth(1).isVisible(), false, 'reduced motion hides only duplicate half');
    const labels = await page.locator('.ms-hero-trust-set').first().locator('.ms-hero-trust-item').allTextContents();
    assert.equal(labels.length, new Set(labels).size, 'reduced motion removes generated repetitions');
    await page.emulateMedia({ reducedMotion: 'no-preference' });
    await page.waitForFunction(() => getComputedStyle(document.querySelector('.ms-hero-trust-marquee')).animationName !== 'none');
    assert.equal(await page.locator('.ms-hero-trust-set').nth(1).isVisible(), true);
    assert.deepEqual(errors, []);
    fs.writeFileSync(path.join(out, 'results.json'), JSON.stringify(rows, null, 2));
    console.log(JSON.stringify({ marquee: 'PASS', phaseChecks: rows.length, resize: 'PASS', reducedMotion: 'PASS', out }, null, 2));
  } finally { await browser.close(); }
})().catch(error => { console.error(error); process.exit(1); });
