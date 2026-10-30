(function () {
  'use strict';
  var tag = document.currentScript;
  var src = tag && tag.dataset.src;
  if (!src) return;

  function inject() {
    var s = document.createElement('script');
    s.src = src;
    s.defer = true;
    document.body.appendChild(s);
  }

  function schedule() {
    var root = document.querySelector('[data-stories]');
    if (!root) return;
    var go = function () {
      inject();
    };
    root.addEventListener('pointerdown', go, { once: true, passive: true });
    if ('requestIdleCallback' in window) requestIdleCallback(go, { timeout: 5000 });
    else setTimeout(go, 3500);
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', schedule, { once: true });
  } else schedule();
})();
