(function () {
  'use strict';
  let loading = false;
  let loaded = false;

  function loadAuth() {
    if (loaded || loading) return;
    loading = true;
    const s = document.createElement('script');
    s.src = document.currentScript?.dataset.authSrc || '/js/auth.js';
    s.defer = true;
    s.onload = () => { loaded = true; loading = false; };
    s.onerror = () => { loading = false; };
    document.body.appendChild(s);
  }

  document.querySelectorAll('[data-open-auth]').forEach((el) => {
    el.addEventListener('mouseenter', loadAuth, { once: true, passive: true });
    el.addEventListener('touchstart', loadAuth, { once: true, passive: true });
    el.addEventListener('click', loadAuth, { once: true });
  });
})();
