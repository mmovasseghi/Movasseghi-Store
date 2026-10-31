// Local diagnostics only. Do not place this proxy in production.
// node scripts/local-latency-proxy.cjs UPSTREAM_PORT LISTEN_PORT [DOCUMENT_DELAY_MS]
// Lighthouse's DevTools network emulation supplies per-resource latency separately.
const http = require('node:http');
const upstream = Number(process.argv[2]);
const port = Number(process.argv[3]);
const delay = Number(process.argv[4] ?? 684);
if (![upstream, port].every(p => Number.isInteger(p) && p > 0 && p < 65536)
    || upstream === port || !Number.isFinite(delay) || delay < 0 || delay > 10000) {
  throw new Error('Expected two local ports and a document delay between 0 and 10000ms');
}
http.createServer((request, response) => {
  const forward = () => {
    const outgoing = http.request({ hostname: '127.0.0.1', port: upstream,
      path: request.url, method: request.method, headers: request.headers }, incoming => {
      response.writeHead(incoming.statusCode, incoming.headers);
      incoming.pipe(response);
    });
    outgoing.on('error', () => { response.writeHead(502); response.end(); });
    request.pipe(outgoing);
  };
  if (new URL(request.url, 'http://localhost').pathname === '/') setTimeout(forward, delay);
  else forward();
}).listen(port, '127.0.0.1');
