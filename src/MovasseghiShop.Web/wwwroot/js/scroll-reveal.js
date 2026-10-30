(function () {
  'use strict';

  const reduced = matchMedia('(prefers-reduced-motion: reduce)').matches;
  const root = document.documentElement;
  root.classList.add('js', 'ms-motion');

  function show(el) {
    el.classList.add('is-visible');
    const staggerRoot = el.matches('[data-reveal-stagger]')
      ? el
      : el.querySelector('[data-reveal-stagger]');
    if (staggerRoot) {
      staggerRoot.querySelectorAll(':scope > *').forEach((child, i) => {
        child.style.setProperty('--ms-reveal-delay', `${Math.min(i * 0.07, 0.42)}s`);
        child.classList.add('is-visible');
      });
    }
  }

  if (reduced) {
    document.querySelectorAll('.ms-reveal, [data-reveal-stagger] > *').forEach(show);
    return;
  }

  if (!('IntersectionObserver' in window)) {
    document.querySelectorAll('.ms-reveal').forEach(show);
    return;
  }

  const observer = new IntersectionObserver(
    (entries) => {
      entries.forEach((entry) => {
        if (!entry.isIntersecting) return;
        show(entry.target);
        observer.unobserve(entry.target);
      });
    },
    { rootMargin: '0px 0px -6% 0px', threshold: 0.08 }
  );

  function observe() {
    document.querySelectorAll('.ms-reveal:not(.is-visible)').forEach((el) => observer.observe(el));
  }

  function observeImages() {
    document.querySelectorAll('img[data-ms-lazy]:not([data-ms-lazy-done])').forEach((img) => {
      img.dataset.msLazyDone = '1';
      if (img.complete) return;
      img.addEventListener(
        'load',
        () => img.classList.add('is-loaded'),
        { once: true }
      );
      img.addEventListener(
        'error',
        () => img.classList.add('is-error'),
        { once: true }
      );
    });
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => {
      observe();
      observeImages();
    });
  } else {
    observe();
    observeImages();
  }

  window.MsScrollReveal = { refresh: observe };
})();
