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
    if ('requestIdleCallback' in window) requestIdleCallback(inject, { timeout: 2800 });
    else window.addEventListener('load', function () { setTimeout(inject, 400); }, { once: true });
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', schedule, { once: true });
  } else schedule();
})();
