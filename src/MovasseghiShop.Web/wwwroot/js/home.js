(function () {
  'use strict';
  const reduced = matchMedia('(prefers-reduced-motion: reduce)').matches;
  const mobileMq = matchMedia('(max-width: 767px)');
  const isMobile = () => mobileMq.matches;
  const faDigits = n => String(n).replace(/\d/g, d => '۰۱۲۳۴۵۶۷۸۹'[d]);

  /* Countdown */
  const cd = document.querySelector('[data-countdown]');
  if (cd) {
    const end = new Date();
    end.setDate(end.getDate() + ((7 - end.getDay()) || 7));
    end.setHours(23, 59, 59, 0);
    const els = { d: cd.querySelector('[data-cd-d]'), h: cd.querySelector('[data-cd-h]'), m: cd.querySelector('[data-cd-m]'), s: cd.querySelector('[data-cd-s]') };
    function tick() {
      const diff = Math.max(0, end - Date.now());
      const pad = v => faDigits(String(v).padStart(2, '0'));
      if (els.d) els.d.textContent = pad(Math.floor(diff / 86400000));
      if (els.h) els.h.textContent = pad(Math.floor((diff % 86400000) / 3600000));
      if (els.m) els.m.textContent = pad(Math.floor((diff % 3600000) / 60000));
      if (els.s) els.s.textContent = pad(Math.floor((diff % 60000) / 1000));
    }
    tick();
    setInterval(tick, 1000);
  }

  /* Features — marquee speed on mobile */
  const featTrack = document.querySelector('[data-features-track]');
  if (featTrack && !reduced && !isMobile()) {
    const cards = featTrack.querySelectorAll('.ms-feature-card:not(.ms-feature-card--dup)');
    if (cards.length) {
      const half = featTrack.scrollWidth / 2;
      featTrack.style.animationDuration = Math.max(16, Math.min(30, half / 35)) + 's';
    }
  }

  function scheduleHeroParticles() {
    const particleWrap = document.querySelector('[data-hero-particles]');
    if (!particleWrap || reduced) return;
    const add = () => {
      for (let i = 0; i < (isMobile() ? 8 : 14); i++) {
        const p = document.createElement('span');
        p.className = 'ms-hero-particle';
        p.style.left = `${Math.random() * 100}%`;
        p.style.top = `${Math.random() * 100}%`;
        particleWrap.appendChild(p);
      }
    };
    if ('requestIdleCallback' in window) requestIdleCallback(add, { timeout: 2500 });
    else setTimeout(add, 800);
  }
  scheduleHeroParticles();

  /* Hero slider — vanilla (بدون GSAP) تا TBT/LCP برای GTmetrix پایین بماند */
  (function initVanillaHeroSlider() {
    const slides = document.querySelectorAll('.ms-hero-slide[data-slide]');
    const dotsWrap = document.querySelector('[data-hero-dots]');
    const progressBar = document.querySelector('[data-hero-progress] span');
    if (!slides.length || !dotsWrap) return;
    const SLIDE_MS = 6000;
    let idx = 0;
    let timer;
    let animating = false;

    slides.forEach((slide, i) => {
      slide.classList.toggle('is-active', i === 0);
      const dot = document.createElement('button');
      dot.type = 'button';
      dot.setAttribute('aria-label', `اسلاید ${i + 1}`);
      if (i === 0) dot.classList.add('is-active');
      dot.addEventListener('click', () => go(i));
      dotsWrap.appendChild(dot);
    });
    const dots = dotsWrap.querySelectorAll('button');

    function go(n) {
      if (animating || n === idx) return;
      animating = true;
      const prev = slides[idx];
      const next = slides[(n + slides.length) % slides.length];
      dots[idx]?.classList.remove('is-active');
      idx = (n + slides.length) % slides.length;
      dots[idx]?.classList.add('is-active');
      prev.classList.remove('is-active');
      next.classList.add('is-active');
      animating = false;
    }

    function resetProgress() {
      if (!progressBar) return;
      progressBar.style.transition = 'none';
      progressBar.style.width = '0%';
      requestAnimationFrame(() => {
        progressBar.style.transition = `width ${SLIDE_MS}ms linear`;
        progressBar.style.width = '100%';
      });
    }

    function autoplay() {
      clearInterval(timer);
      resetProgress();
      timer = setInterval(() => { if (!animating) go(idx + 1); }, SLIDE_MS);
    }

    autoplay();
    dotsWrap.addEventListener('click', autoplay);

    const viewport = document.querySelector('.ms-hero-viewport');
    if (viewport && isMobile()) {
      let sx = 0;
      viewport.addEventListener('touchstart', e => { sx = e.touches[0].clientX; }, { passive: true });
      viewport.addEventListener('touchend', e => {
        const dx = e.changedTouches[0].clientX - sx;
        if (Math.abs(dx) > 45) { go(dx > 0 ? idx - 1 : idx + 1); autoplay(); }
      }, { passive: true });
    }
  })();

  document.querySelectorAll('[data-hero-count]').forEach(el => {
    const target = parseInt(el.dataset.heroCount, 10);
    if (!isNaN(target)) el.textContent = faDigits(target) + '+';
  });
})();
