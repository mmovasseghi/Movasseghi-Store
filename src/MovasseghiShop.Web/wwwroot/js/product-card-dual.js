(function () {
  'use strict';

  const PHASE_MS = 6000;
  const reduced = window.matchMedia('(prefers-reduced-motion: reduce)').matches;

  function init() {
    const cards = document.querySelectorAll('[data-ms-product-dual]');
    if (!cards.length || reduced) return;

    let epoch = performance.now();
    let lastPhase = 'studio';
    let running = false;
    let rafId = null;

    function setPhase(phase) {
      if (phase === lastPhase) return;
      lastPhase = phase;
      cards.forEach((el) => {
        el.dataset.msDualPhase = phase;
        el.classList.remove('ms-product-dual--flip');
        void el.offsetWidth;
        el.classList.add('ms-product-dual--flip');
      });
      window.setTimeout(() => {
        cards.forEach((el) => el.classList.remove('ms-product-dual--flip'));
      }, 1300);
    }

    function tick(now) {
      if (!running) return;
      if (document.hidden) {
        rafId = requestAnimationFrame(tick);
        return;
      }

      const elapsed = now - epoch;
      const cycleLen = PHASE_MS * 2;
      const pos = ((elapsed % cycleLen) + cycleLen) % cycleLen;
      const phase = pos < PHASE_MS ? 'studio' : 'scene';
      const progress = (pos % PHASE_MS) / PHASE_MS;

      document.documentElement.style.setProperty('--ms-dual-progress', progress.toFixed(4));
      setPhase(phase);
      rafId = requestAnimationFrame(tick);
    }

    function start() {
      if (running) return;
      running = true;
      epoch = performance.now();
      document.documentElement.style.setProperty('--ms-dual-progress', '0');
      cards.forEach((el) => {
        el.dataset.msDualPhase = 'studio';
      });
      if (!rafId) rafId = requestAnimationFrame(tick);
    }

    function stop() {
      running = false;
    }

    if ('IntersectionObserver' in window) {
      const io = new IntersectionObserver(
        (entries) => {
          if (entries.some((e) => e.isIntersecting)) start();
          else stop();
        },
        { rootMargin: '80px', threshold: 0.01 }
      );
      cards.forEach((c) => io.observe(c));
    } else {
      start();
    }
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
  } else {
    init();
  }
})();
