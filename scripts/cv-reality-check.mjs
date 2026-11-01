#!/usr/bin/env node
/**
 * content-visibility reality check WITHOUT reading geometry of skipped descendants
 * (which would force rendering). Uses IntersectionObserver + checkVisibility when available,
 * and computed style on the section roots only.
 */
import puppeteer from 'puppeteer-core';
import fs from 'fs';

const OUT = '/opt/cursor/artifacts/cycle-critical-split';
const URL = 'https://185.93.88.81/';

const browser = await puppeteer.launch({
  executablePath: '/usr/local/bin/google-chrome',
  headless: true,
  args: ['--no-sandbox', '--disable-setuid-sandbox', '--disable-dev-shm-usage'],
});
const page = await browser.newPage();
await page.setViewport({ width: 1366, height: 768, deviceScaleFactor: 1 });
await page.goto(URL, { waitUntil: 'networkidle2', timeout: 90000 });

const atTop = await page.evaluate(() => {
  const sels = ['.ms-offers', '.ms-cat-section', '#ms-featured', '#ms-segments', '.ms-closing', '.ms-news', '.ms-home-faq', 'footer.ms-footer', '#ms-features', '#ms-hero'];
  return sels.map((sel) => {
    const el = document.querySelector(sel);
    if (!el) return { sel, missing: true };
    const cs = getComputedStyle(el);
    const rect = el.getBoundingClientRect(); // section root only
    let checkVisibility = null;
    try { checkVisibility = el.checkVisibility?.({ checkOpacity: true, checkVisibilityCSS: true }) ?? null; } catch {}
    return {
      sel,
      contentVisibility: cs.contentVisibility,
      containIntrinsicSize: cs.containIntrinsicSize,
      top: Math.round(rect.top),
      height: Math.round(rect.height),
      inViewport: rect.top < innerHeight && rect.bottom > 0,
      nearViewport: rect.top < innerHeight + 100 && rect.bottom > -100,
      checkVisibility,
    };
  });
});

// Fast scroll to products/footer and back — watch for jumps via section root tops only
const scrollProbe = await page.evaluate(async () => {
  const notes = [];
  const featured = document.querySelector('#ms-featured');
  const footer = document.querySelector('footer.ms-footer');
  const y0 = scrollY;
  featured?.scrollIntoView({ behavior: 'instant', block: 'start' });
  await new Promise((r) => setTimeout(r, 200));
  notes.push({ at: 'featured', scrollY, featuredTop: featured?.getBoundingClientRect().top, cv: featured && getComputedStyle(featured).contentVisibility });
  footer?.scrollIntoView({ behavior: 'instant', block: 'start' });
  await new Promise((r) => setTimeout(r, 200));
  notes.push({ at: 'footer', scrollY, footerTop: footer?.getBoundingClientRect().top, cv: footer && getComputedStyle(footer).contentVisibility });
  scrollTo(0, 0);
  await new Promise((r) => setTimeout(r, 200));
  notes.push({ at: 'top', scrollY, heroTop: document.querySelector('#ms-hero')?.getBoundingClientRect().top });
  // anchor jump
  location.hash = '#ms-featured';
  await new Promise((r) => setTimeout(r, 200));
  notes.push({ at: 'hash-featured', scrollY, featuredTop: featured?.getBoundingClientRect().top });
  history.replaceState(null, '', location.pathname);
  scrollTo(0, y0);
  return notes;
});

// Images readiness when entering viewport (observe without measuring skipped kids first)
const imgProbe = await page.evaluate(async () => {
  const imgs = [...document.querySelectorAll('#ms-featured img, #ms-offers img')].slice(0, 6);
  return imgs.map((img) => ({
    src: (img.currentSrc || img.src || '').slice(-60),
    complete: img.complete,
    w: img.naturalWidth,
    loading: img.loading,
  }));
});

const report = { url: URL, atTop, scrollProbe, imgProbe };
fs.writeFileSync(`${OUT}/cv-reality.json`, JSON.stringify(report, null, 2));
console.log(JSON.stringify(report, null, 2));
await browser.close();
