(function () {
  'use strict';

  const reduced = matchMedia('(prefers-reduced-motion: reduce)').matches;
  const isHome = document.body.classList.contains('ms-page-home');
  const root = document.documentElement;

  function show(el) {
    el.classList.add('is-visible');
    const staggerRoot = el.matches('[data-reveal-stagger]')
      ? el
      : el.querySelector('[data-reveal-stagger]');
    if (staggerRoot) {
      staggerRoot.querySelectorAll(':scope > *').forEach((child, i) => {
        child.style.setProperty('--ms-reveal-delay', `${Math.min(i * 0.04, 0.2)}s`);
        child.classList.add('is-visible');
      });
    }
  }

  function showHomeAboveFold() {
    document.querySelectorAll('.ms-stories .ms-story').forEach((el) => {
      el.classList.add('is-visible');
    });
    document.querySelectorAll('#ms-hero, .ms-stories, #ms-features').forEach((el) => {
      el.classList.add('is-visible');
    });
  }

  if (reduced) {
    root.classList.add('js', 'ms-motion');
    document.querySelectorAll('.ms-reveal, [data-reveal-stagger] > *, .ms-story').forEach((el) => {
      el.classList.add('is-visible');
    });
    return;
  }

  if (!('IntersectionObserver' in window)) {
    root.classList.add('js', 'ms-motion');
    document.querySelectorAll('.ms-reveal, .ms-story').forEach((el) => el.classList.add('is-visible'));
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
    { rootMargin: '0px 0px -4% 0px', threshold: 0.05 }
  );

  function observe() {
    if (isHome) showHomeAboveFold();

    const vh = window.innerHeight || document.documentElement.clientHeight;
    document.querySelectorAll('.ms-reveal:not(.is-visible)').forEach((el) => {
      const rect = el.getBoundingClientRect();
      if (rect.top < vh * 0.92 && rect.bottom > 0) {
        show(el);
        return;
      }
      observer.observe(el);
    });
  }

  function observeImages() {
    document.querySelectorAll('img[data-ms-lazy]:not([data-ms-lazy-done])').forEach((img) => {
      img.dataset.msLazyDone = '1';
      if (img.complete) {
        img.classList.add('is-loaded');
        return;
      }
      img.addEventListener('load', () => img.classList.add('is-loaded'), { once: true });
      img.addEventListener('error', () => img.classList.add('is-error'), { once: true });
    });
  }

  function start() {
    root.classList.add('js', 'ms-motion');
    observe();
    observeImages();
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', start);
  } else {
    start();
  }

  window.MsScrollReveal = { refresh: observe };
})();
