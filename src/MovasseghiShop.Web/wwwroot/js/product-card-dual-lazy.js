(function () {
  'use strict';
  var tag = document.currentScript;
  var src = tag && tag.dataset.src;
  if (!src) return;
  function load() {
    var s = document.createElement('script');
    s.src = src;
    s.defer = true;
    document.body.appendChild(s);
  }
  if ('requestIdleCallback' in window) requestIdleCallback(load, { timeout: 4000 });
  else setTimeout(load, 2000);
})();
