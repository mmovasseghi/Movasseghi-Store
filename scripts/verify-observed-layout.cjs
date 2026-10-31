// Preserve floating-call clearance and footer marquee speed after replacing
// eager geometry reads with ResizeObserver. All navigation is local/read-only.
const { chromium } = require('playwright');
const assert = require('node:assert/strict');
(async()=>{
  const b=await chromium.launch({executablePath:process.env.CHROMIUM_PATH||'/usr/bin/chromium',args:['--no-sandbox']});
  const rows=[];
  try {
    for(const route of ['/','/Catalog']) {
      const p=await b.newPage({viewport:{width:390,height:844}});
      const errors=[];p.on('pageerror',e=>errors.push(e.message));
      await p.addInitScript(()=>{window.__scripts=[];document.addEventListener('load',e=>{if(e.target instanceof HTMLScriptElement)__scripts.push(e.target.src)},true)});
      await p.goto((process.env.BASE_URL||'http://127.0.0.1:5080')+route,{waitUntil:'load'});
      await p.waitForFunction(()=>__scripts.some(u=>/\/site\.js(?:\?|$)/.test(u)));
      await p.evaluate(()=>document.fonts.ready);
      for(const width of [390,768,1366,390]) {
        await p.setViewportSize({width,height:844});
        await p.waitForFunction(()=>{
          const fab=document.querySelector('[data-fab-call]'),bar=document.querySelector('.ms-tabbar'),filter=document.querySelector('.mobile-filter-bar');
          const expected=getComputedStyle(bar).display==='none'?20:bar.offsetHeight+(filter&&getComputedStyle(filter).display!=='none'?filter.offsetHeight+8:0)+14;
          return Math.abs(parseFloat(getComputedStyle(fab).bottom)-expected)<1;
        });
        const footer=p.locator('[data-footer-trust]');await footer.evaluate(e=>e.closest('[data-footer]').scrollIntoView({block:'center',behavior:'instant'}));
        await p.waitForFunction(()=>{
          const t=document.querySelector('[data-footer-trust]'),row=t.querySelector('.ms-footer-trust-row');
          return Math.abs(parseFloat(getComputedStyle(t).animationDuration)-Math.max(18,Math.min(40,row.scrollWidth/30)))<.05;
        });
        if (width===390) {
          await p.locator('.ms-tabbar').evaluate(e=>e.style.paddingBottom='48px');
          await p.waitForFunction(()=>{
            const fab=document.querySelector('[data-fab-call]'),bar=document.querySelector('.ms-tabbar'),filter=document.querySelector('.mobile-filter-bar');
            const expected=bar.offsetHeight+(filter&&getComputedStyle(filter).display!=='none'?filter.offsetHeight+8:0)+14;
            return Math.abs(parseFloat(getComputedStyle(fab).bottom)-expected)<1;
          });
        }
        rows.push({route,width,footerSpeed:'PASS',floatingCallClearance:'PASS'});
      }
      assert.deepEqual(errors,[]);await p.close();
    }
    const legacy=await b.newPage({viewport:{width:390,height:844}});
    await legacy.addInitScript(()=>{delete ResizeObserverEntry.prototype.borderBoxSize});
    await legacy.goto((process.env.BASE_URL||'http://127.0.0.1:5080')+'/',{waitUntil:'load'});
    await legacy.locator('[data-menu-toggle]').first().click();
    await legacy.locator('#mobileNav').waitFor({state:'visible'});
    await legacy.evaluate(()=>dispatchEvent(new Event('resize')));
    await legacy.waitForFunction(()=>Math.abs(parseFloat(getComputedStyle(document.querySelector('[data-fab-call]')).bottom)-document.querySelector('.ms-tabbar').offsetHeight-14)<1);
    await legacy.close();rows.push({legacyBorderBoxUnavailable:'PASS'});
    console.log(JSON.stringify(rows));
  }finally{await b.close()}
})().catch(e=>{console.error(e);process.exit(1)});
