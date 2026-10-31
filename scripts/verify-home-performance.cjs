// Local UX regression checks; these results are not GTmetrix scores.
const { chromium } = require('playwright');
const fs = require('fs');
const os = require('os');
const path = require('path');
const assert = require('node:assert/strict');
const base = process.env.BASE_URL || 'http://127.0.0.1:5274';
const output = fs.mkdtempSync(path.join(os.tmpdir(), 'movasseghi-home-'));
(async()=>{
const browser=await chromium.launch({executablePath:process.env.CHROMIUM_PATH || (fs.existsSync('/usr/bin/chromium') ? '/usr/bin/chromium' : undefined),args:['--no-sandbox']});
const results=[];
for(const [name,selector,state] of [['menu','[data-menu-toggle]','#mobileNav'],['cart','[data-open-cart]','#appModal'],['auth','[data-open-auth]','#appModal'],['story','.ms-story','#msStoryModal']]){
 const page=await browser.newPage({viewport:{width:390,height:844}});
 const errors=[];page.on('pageerror',e=>errors.push(e.message));
 await page.route('**/js/site.js*',async route=>{await new Promise(r=>setTimeout(r,250));await route.continue();});
 await page.goto(base + '/',{waitUntil:'domcontentloaded'});
 await page.locator(selector).first().click();
 const opened=await page.locator(state).waitFor({state:'visible',timeout:2000}).then(()=>true,()=>false);
 results.push({name,firstClickOpened:opened,errors});
 await page.close();
}
for(const [name,width,height] of [['mobile',390,844],['desktop',1366,768]]){
 const page=await browser.newPage({viewport:{width,height}});
 await page.addInitScript(()=>{window.__shifts=[];new PerformanceObserver(l=>{for(const e of l.getEntries())if(!e.hadRecentInput)window.__shifts.push(e.value)}).observe({type:'layout-shift',buffered:true})});
 await page.route('**/css/storefront*.bundle.css*',async route=>{await new Promise(r=>setTimeout(r,1500));await route.continue();});
 await page.goto(base + '/',{waitUntil:'domcontentloaded'});
 const read=()=>page.evaluate(()=>{const b=s=>{const e=document.querySelector(s);if(!e)return null;const r=e.getBoundingClientRect();return{x:r.x,y:r.y,width:r.width,height:r.height}};return{hero:b('.ms-hero'),story:b('.ms-stories'),heading:b('h1'),header:b('.ms-top'),menuHidden:document.querySelector('#mobileNav').hidden,modalHidden:document.querySelector('#appModal').hidden,horizontalOverflow:document.documentElement.scrollWidth>innerWidth}});
 const initial=await read();await page.screenshot({path:path.join(output, `${name}-critical.png`)});
 await page.waitForLoadState('load');
 const loaded=await read();await page.screenshot({path:path.join(output, `${name}-loaded.png`)});
 const counts=await page.evaluate(()=>({featured:document.querySelectorAll('.ms-feat-card').length,offers:document.querySelectorAll('.ms-offer-card').length,stories:document.querySelectorAll('.ms-story[data-story-id]').length,cls:window.__shifts.reduce((a,b)=>a+b,0)}));
 results.push({name,initial,loaded,...counts});await page.close();
}
fs.writeFileSync(path.join(output, 'home-audit.json'), JSON.stringify(results,null,2));
await browser.close();
for (const r of results) {
 if ('firstClickOpened' in r) { assert.equal(r.firstClickOpened, true, r.name + ': first click lost'); assert.deepEqual(r.errors, [], r.name); }
 else {
  assert.equal(r.featured, 8, r.name + ': featured count'); assert.equal(r.offers, 8, r.name + ': offer count'); assert.ok(r.stories > 0);
  assert.equal(r.initial.horizontalOverflow, false); assert.equal(r.loaded.horizontalOverflow, false);
  for (const area of ['hero', 'story', 'heading', 'header']) for (const key of ['x','y','width','height']) assert.ok(Math.abs(r.initial[area][key]-r.loaded[area][key]) < 2, `${r.name}: ${area} ${key} changed when CSS arrived`);
  assert.ok(r.cls < .01, r.name + ': delayed-CSS CLS regression: ' + r.cls);
 }
}
console.log(JSON.stringify(results.map(r => ({name:r.name, firstClickOpened:r.firstClickOpened, featured:r.featured, offers:r.offers, stories:r.stories, cls:r.cls})),null,2));
console.log('Local regression checks passed. Artifacts: ' + output);
})().catch(e=>{console.error(e);process.exit(1)});
