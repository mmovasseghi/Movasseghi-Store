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
    function setCountdownText(el, text) {
      if (el && el.textContent !== text) el.textContent = text;
    }
    function tick() {
      const diff = Math.max(0, end - Date.now());
      const pad = v => faDigits(String(v).padStart(2, '0'));
      setCountdownText(els.d, pad(Math.floor(diff / 86400000)));
      setCountdownText(els.h, pad(Math.floor((diff % 86400000) / 3600000)));
      setCountdownText(els.m, pad(Math.floor((diff % 3600000) / 60000)));
      setCountdownText(els.s, pad(Math.floor((diff % 60000) / 1000)));
    }
    tick();
    setInterval(tick, 1000);
  }

  /* Features — marquee speed on mobile */
  const featTrack = document.querySelector('[data-features-track]');
  if (featTrack && !reduced && !isMobile()) {
    const cards = featTrack.querySelectorAll('.ms-feature-card:not(.ms-feature-card--dup)');
    if (cards.length && getComputedStyle(featTrack).animationName !== 'none') {
      const half = featTrack.scrollWidth / 2;
      featTrack.style.animationDuration = Math.max(16, Math.min(30, half / 35)) + 's';
    }
  }

  /* Two equal, viewport-filling halves keep the trust strip seamless, even
     when the configured labels are shorter than the desktop viewport. */
  (function initHeroTrustMarquee() {
    const viewport = document.querySelector('.ms-hero-trust-track');
    const track = viewport?.querySelector('.ms-hero-trust-marquee');
    const groups = track?.querySelectorAll(':scope > .ms-hero-trust-set');
    if (!groups || groups.length !== 2) return;
    const originals = [...groups[0].children];
    if (!originals.length) return;
    const motion = matchMedia('(prefers-reduced-motion: reduce)');
    let copies = 1;
    let frame = 0;
    function fill() {
      frame = 0;
      const style = getComputedStyle(groups[0]);
      const gap = parseFloat(style.columnGap) || 0;
      const padding = (parseFloat(style.paddingLeft) || 0) + (parseFloat(style.paddingRight) || 0);
      const unit = originals.reduce((sum, item) => sum + item.getBoundingClientRect().width + gap, 0);
      if (!unit) return;
      const required = motion.matches ? 1 : Math.max(1, Math.ceil((viewport.clientWidth - padding + gap + 1) / unit));
      if (required === copies) return;
      const first = [...originals];
      for (let i = 1; i < required; i++) first.push(...originals.map(item => item.cloneNode(true)));
      const second = first.map(item => item.cloneNode(true));
      groups[0].replaceChildren(...first);
      groups[1].replaceChildren(...second);
      copies = required;
    }
    function schedule() {
      if (!frame) frame = requestAnimationFrame(fill);
    }
    fill();
    if ('ResizeObserver' in window) new ResizeObserver(schedule).observe(viewport);
    else window.addEventListener('resize', schedule, { passive: true });
    document.fonts?.ready.then(schedule);
    motion.addEventListener('change', schedule);
  })();

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
    let progressAnimation;
    let dotAnimations = [];
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
      const dotRects = !reduced ? [...dots].map(dot => dot.getBoundingClientRect()) : null;
      dotAnimations.forEach(animation => animation.cancel());
      const prev = slides[idx];
      const next = slides[(n + slides.length) % slides.length];
      dots[idx]?.classList.remove('is-active');
      idx = (n + slides.length) % slides.length;
      dots[idx]?.classList.add('is-active');
      if (dotRects) {
        // FLIP keeps the original changing pill widths and neighbour movement,
        // while only transform/opacity animate between the two layouts.
        const after = [...dots].map(dot => dot.getBoundingClientRect());
        dotAnimations = [...dots].map((dot, i) => dot.animate([
          { transform: `translateX(${dotRects[i].left - after[i].left}px) scaleX(${dotRects[i].width / after[i].width})` },
          { transform: 'translateX(0) scaleX(1)' }
        ], { duration: 350, easing: 'cubic-bezier(.22,1,.36,1)' }));
      }
      prev.classList.remove('is-active');
      next.classList.add('is-active');
      resetProgress();
      animating = false;
    }

    function resetProgress() {
      if (!progressBar) return;
      progressAnimation?.cancel();
      if (reduced) { progressBar.style.transform = 'scaleX(1)'; return; }
      progressAnimation = progressBar.animate(
        [{ transform: 'scaleX(0)' }, { transform: 'scaleX(1)' }],
        { duration: SLIDE_MS, easing: 'linear', fill: 'forwards' }
      );
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
