(function () {
  'use strict';
  const tag = document.currentScript;
  if (!tag || tag.dataset.when !== 'load') return;

  const urls = [tag.dataset.gsap, tag.dataset.scroll].filter(Boolean);
  if (!urls.length) return;

  function loadNext(i) {
    if (i >= urls.length) {
      window.dispatchEvent(new Event('ms:gsap-ready'));
      return;
    }
    const s = document.createElement('script');
    s.src = urls[i];
    s.async = true;
    s.onload = () => loadNext(i + 1);
    s.onerror = () => loadNext(i + 1);
    document.head.appendChild(s);
  }

  function start() {
    loadNext(0);
  }

  if (document.readyState === 'complete') start();
  else window.addEventListener('load', start, { once: true });
})();
