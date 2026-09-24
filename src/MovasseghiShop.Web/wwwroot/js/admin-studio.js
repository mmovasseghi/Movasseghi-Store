(function () {
  'use strict';

  const sidebar = document.querySelector('[data-st-sidebar]');
  const toggle = document.querySelector('[data-st-menu-toggle]');
  const overlay = document.querySelector('[data-st-overlay]');
  const navClose = document.querySelector('[data-st-nav-close]');
  const mobileMq = window.matchMedia('(max-width: 960px)');

  function isMobileNav() {
    return mobileMq.matches;
  }

  function setNavOpen(open) {
    sidebar?.classList.toggle('is-open', open);
    const useOverlay = open && !isMobileNav();
    overlay?.classList.toggle('is-visible', useOverlay);
    document.body.classList.toggle('st-nav-open', open);
    toggle?.setAttribute('aria-expanded', open ? 'true' : 'false');
    document.documentElement.classList.toggle('st-nav-open', open);
    if (open && isMobileNav()) {
      document.body.style.overflow = 'hidden';
    } else {
      document.body.style.overflow = '';
    }
  }

  function closeSidebar() {
    setNavOpen(false);
  }

  toggle?.setAttribute('aria-expanded', 'false');
  toggle?.addEventListener('click', () => {
    const open = !sidebar?.classList.contains('is-open');
    setNavOpen(open);
  });
  overlay?.addEventListener('click', closeSidebar);
  navClose?.addEventListener('click', closeSidebar);

  document.querySelectorAll('.st-nav-item').forEach((a) => {
    a.addEventListener('click', () => {
      if (window.matchMedia('(max-width: 960px)').matches) closeSidebar();
    });
  });

  document.addEventListener('keydown', (e) => {
    if (e.key === 'Escape' && sidebar?.classList.contains('is-open')) closeSidebar();
  });

  window.addEventListener('pageshow', () => {
    closeSidebar();
  });

  document.querySelectorAll('[data-count]').forEach((el) => {
    const target = parseInt(el.getAttribute('data-count') || '0', 10);
    if (isNaN(target)) return;
    const duration = 900;
    const start = performance.now();
    function tick(now) {
      const p = Math.min(1, (now - start) / duration);
      el.textContent = Math.round(target * (1 - Math.pow(1 - p, 3)));
      if (p < 1) requestAnimationFrame(tick);
    }
    requestAnimationFrame(tick);
  });
})();
