// Delay reveal initialization while a reader scrolls ahead: visible content
// must never disappear, and later offscreen sections still need their reveal.
const { chromium }=require('playwright'),assert=require('node:assert/strict');
(async()=>{const b=await chromium.launch({executablePath:process.env.CHROMIUM_PATH||'/usr/bin/chromium',args:['--no-sandbox']});try{
 const p=await b.newPage({viewport:{width:390,height:844}});let release;
 await p.route('**/js/scroll-reveal.js*',route=>new Promise(resolve=>{release=async()=>{await route.continue();resolve()}}));
 await p.goto(process.env.BASE_URL||'http://127.0.0.1:5080/',{waitUntil:'commit'});
 const offers=p.locator('#ms-offers');await offers.waitFor({state:'visible'});
 await p.waitForFunction(()=>document.querySelector('#ms-featured'));
 await offers.evaluate(e=>e.scrollIntoView({block:'center',behavior:'instant'}));
 await p.evaluate(()=>{window.__opacity=[];window.__sample=true;function read(){__opacity.push(parseFloat(getComputedStyle(document.querySelector('#ms-offers')).opacity));if(__sample)requestAnimationFrame(read)}requestAnimationFrame(read)});
 await p.waitForFunction(()=>document.querySelector('script[src*="scroll-reveal.js"]'));
 assert.ok(release,'reveal request must be deliberately held');await release();
 await p.waitForFunction(()=>typeof MsScrollReveal!=='undefined'&&document.querySelector('#ms-offers').classList.contains('is-visible'));
 await p.evaluate(()=>new Promise(r=>requestAnimationFrame(()=>requestAnimationFrame(r))));
 const opacity=await p.evaluate(()=>{__sample=false;return __opacity});assert.ok(opacity.length>0);assert.ok(opacity.every(x=>x>=.99),'late initialization hid visible content: '+opacity);
 const next=p.locator('#ms-featured');await p.waitForFunction(()=>document.querySelector('#ms-featured').classList.contains('ms-home-reveal'));
 assert.equal(await next.evaluate(e=>getComputedStyle(e).opacity),'0');
 await next.evaluate(e=>e.scrollIntoView({block:'center',behavior:'instant'}));await p.waitForFunction(()=>document.querySelector('#ms-featured').classList.contains('is-visible'));
 assert.ok((await next.evaluate(e=>getComputedStyle(e).transitionProperty)).includes('opacity'));
 await p.waitForFunction(()=>Number(getComputedStyle(document.querySelector('#ms-featured')).opacity)>.99);
 console.log('PASS: delayed reveal never hides onscreen content; offscreen sections retain their fade on scroll.');
 }finally{await b.close()}})().catch(e=>{console.error(e);process.exit(1)});
