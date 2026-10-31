// Diagnostic HTTP/1.1 model, NOT a GTmetrix/Seattle measurement or production proxy.
// node scripts/local-origin-profile.cjs UPSTREAM_PORT LISTEN_PORT
// 647ms document TTFB; 215ms RTT; first resource connection adds 215ms;
// each response sends an initial 14,600-byte window, doubling after every RTT.
// This declares slow-start costs absent from DevTools' localhost latency setting.
const http = require('node:http');
const upstream = Number(process.argv[2]);
const port = Number(process.argv[3]);
if (![upstream, port].every(p => Number.isInteger(p) && p > 0 && p < 65536) || upstream === port) {
  throw new Error('Expected two distinct local ports');
}
const connections = new WeakSet();
http.createServer((request, response) => {
  const first = !connections.has(request.socket);
  connections.add(request.socket);
  const root = new URL(request.url, 'http://localhost').pathname === '/';
  const ttfb = root ? 647 : first ? 430 : 215;
  let timer;
  response.once('close', () => clearTimeout(timer));
  const outgoing = http.request({ hostname: '127.0.0.1', port: upstream, path: request.url,
    method: request.method, headers: request.headers }, incoming => {
    const chunks = [];
    incoming.on('data', chunk => chunks.push(chunk));
    incoming.on('end', () => {
      const body = Buffer.concat(chunks);
      const headers = { ...incoming.headers, 'content-length': body.length };
      delete headers['transfer-encoding'];
      delete headers.connection;
      let offset = 0;
      let window = 14600;
      const send = () => {
        if (response.destroyed) return;
        const end = Math.min(body.length, offset + window);
        response.write(body.subarray(offset, end));
        offset = end;
        if (offset === body.length || request.method === 'HEAD') response.end();
        else { window *= 2; timer = setTimeout(send, 215); }
      };
      timer = setTimeout(() => {
        if (response.destroyed) return;
        response.writeHead(incoming.statusCode, headers);
        send();
      }, ttfb);
    });
    incoming.on('error', () => response.destroy());
  });
  outgoing.on('error', () => { if (!response.headersSent) response.writeHead(502); response.end(); });
  request.pipe(outgoing);
}).listen(port, '127.0.0.1');
