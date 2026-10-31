// Hold main HTML/CSS after the header. This tests real first-click intent while
// the document is still arriving, with classic scrollbars.
const http = require('node:http');
const { once } = require('node:events');
const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const upstream = new URL(process.env.BASE_URL || 'http://127.0.0.1:5080');
let release;
const server = http.createServer((request,response) => {
  const out = http.request({hostname:upstream.hostname,port:upstream.port,path:request.url,
    method:request.method,headers:{...request.headers,'accept-encoding':'identity'}},incoming => {
    if(request.url!=='/') { response.writeHead(incoming.statusCode,incoming.headers);incoming.pipe(response);return; }
    const chunks=[];incoming.on('data',c=>chunks.push(c));incoming.on('end',()=>{
      const body=Buffer.concat(chunks).toString('utf8');const split=body.indexOf('</header>')+9;
      if(split<9){response.writeHead(500);response.end('Missing header');return;}
      const headers={...incoming.headers};delete headers['content-length'];delete headers['transfer-encoding'];
      response.writeHead(incoming.statusCode,headers);response.write(body.slice(0,split));
      release=()=>{response.end(body.slice(split));release=undefined;};
    });
  });
  out.on('error',()=>response.destroy());request.pipe(out);
});
(async()=>{
  server.listen(0,'127.0.0.1');await once(server,'listening');
  const browser=await chromium.launch({executablePath:process.env.CHROMIUM_PATH||'/usr/bin/chromium',args:['--no-sandbox'],ignoreDefaultArgs:['--hide-scrollbars']});
  try {
    for(const [width,control,panel] of [[390,'menu','#mobileNav'],[390,'cart','#appModal'],[390,'auth','#appModal'],[1366,'cart','#appModal'],[1366,'auth','#appModal']]){
      const page=await browser.newPage({viewport:{width,height:844}});const errors=[];
      page.on('pageerror',e=>errors.push(e.message));
      await page.goto(`http://127.0.0.1:${server.address().port}`,{waitUntil:'commit'});
      const selector={menu:'[data-menu-toggle]',cart:'[data-open-cart]',auth:'[data-open-auth]'}[control];
      await page.locator(selector).first().waitFor({state:'visible'});await page.evaluate(()=>document.fonts.ready);
      assert.equal(await page.locator('#msHomeShellStyles').count(),1);
      assert.equal(await page.locator('#msHomeInlineStyles').count(),0);
      const snapshot=()=>page.locator('.ms-top,.ms-promo-bar,.ms-logo').evaluateAll(es=>es.map(e=>{
        const r=e.getBoundingClientRect(),c=getComputedStyle(e);
        return {x:r.x,y:r.y,width:r.width,height:r.height,color:c.color,background:c.backgroundColor,font:c.fontFamily};
      }));
      const before=await snapshot();release();await page.waitForLoadState('load');
      assert.deepEqual(await snapshot(),before,'main CSS must preserve header geometry/type/colour');
      await page.close();
      // A separate page queues a genuine tap before the normal loader exists.
      const tap=await browser.newPage({viewport:{width,height:844}});tap.on('pageerror',e=>errors.push(e.message));
      await tap.goto(`http://127.0.0.1:${server.address().port}`,{waitUntil:'commit'});
      await tap.locator(selector).first().click();
      assert.equal(await tap.locator(selector).first().getAttribute('aria-busy'),'true');
      release();await tap.locator(panel).waitFor({state:'visible',timeout:5000});
      assert.deepEqual(errors,[]);await tap.close();
    }
    console.log('PASS: streamed header styling, classic scrollbar stability and five early first-click intents.');
  }finally{if(release)release();await browser.close();server.closeAllConnections();server.close();}
})().catch(e=>{console.error(e);process.exit(1)});
