#!/usr/bin/env node
/**
 * Prove ensureStoriesLoaded shares one in-flight fetch across boot + early intent + click.
 * Simulates slow Stories.json and asserts a single concurrent request + real image load.
 */
import puppeteer from 'puppeteer-core';

const TARGET = process.env.MS_VERIFY_URL || 'https://185.93.88.81/';
const chrome = process.env.CHROME_PATH || '/usr/local/bin/google-chrome';

const browser = await puppeteer.launch({
  executablePath: chrome,
  headless: true,
  args: ['--no-sandbox', '--disable-setuid-sandbox', '--disable-dev-shm-usage'],
});

const page = await browser.newPage();
await page.setViewport({ width: 390, height: 844, deviceScaleFactor: 2 });

await page.setRequestInterception(true);
let storiesStarts = 0;
let storiesInflight = 0;
let storiesMaxInflight = 0;
const storiesUrls = [];

page.on('request', async (req) => {
  const url = req.url();
  if (/\/Home\/Stories\.json(?:\?|$)/i.test(url)) {
    storiesStarts += 1;
    storiesInflight += 1;
    storiesMaxInflight = Math.max(storiesMaxInflight, storiesInflight);
    storiesUrls.push(url);
    // Hold the response so boot + early intent overlap.
    setTimeout(() => {
      req.continue().catch(() => {});
    }, 900);
    return;
  }
  req.continue().catch(() => {});
});

page.on('requestfinished', (req) => {
  if (/\/Home\/Stories\.json(?:\?|$)/i.test(req.url())) {
    storiesInflight = Math.max(0, storiesInflight - 1);
  }
});
page.on('requestfailed', (req) => {
  if (/\/Home\/Stories\.json(?:\?|$)/i.test(req.url())) {
    storiesInflight = Math.max(0, storiesInflight - 1);
  }
});

await page.evaluateOnNewDocument(() => {
  // Click the first story as soon as it appears, before stories.js finishes fetch.
  const mo = new MutationObserver(() => {
    const btn = document.querySelector('.ms-story[data-story-id]');
    if (btn && !window.__msVerifyStoryClicked) {
      window.__msVerifyStoryClicked = true;
      btn.click();
    }
  });
  mo.observe(document.documentElement, { childList: true, subtree: true });
});

await page.goto(TARGET, { waitUntil: 'domcontentloaded', timeout: 90000 });

const opened = await page.waitForFunction(() => {
  const modal = document.getElementById('msStoryModal');
  return modal && !modal.hidden && modal.classList.contains('is-open');
}, { timeout: 20000 }).then(() => true).catch(() => false);

const imgState = await page.evaluate(async () => {
  const img = document.querySelector('#msStoryModal img');
  if (!img) return { present: false };
  if (!img.complete) {
    await new Promise((resolve) => {
      img.addEventListener('load', resolve, { once: true });
      img.addEventListener('error', resolve, { once: true });
      setTimeout(resolve, 8000);
    });
  }
  return {
    present: true,
    src: img.currentSrc || img.src,
    complete: img.complete,
    naturalWidth: img.naturalWidth,
    naturalHeight: img.naturalHeight,
  };
});

const result = {
  target: TARGET,
  storiesStarts,
  storiesMaxInflight,
  storiesUrls,
  opened,
  imgState,
  pass:
    storiesStarts === 1 &&
    storiesMaxInflight === 1 &&
    opened === true &&
    imgState.present === true &&
    imgState.complete === true &&
    (imgState.naturalWidth || 0) > 0,
};

console.log(JSON.stringify(result, null, 2));
await browser.close();
process.exit(result.pass ? 0 : 1);
