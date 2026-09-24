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

  if (reduced || typeof gsap === 'undefined') return;
  gsap.registerPlugin(ScrollTrigger);

  const reveal = (targets, vars, trigger, start = 'top 88%') =>
    gsap.from(targets, {
      immediateRender: false,
      scrollTrigger: { trigger, start, once: true },
      clearProps: 'opacity,transform',
      ...vars
    });

  /* Hero particles — static decorative dots only */
  const particleWrap = document.querySelector('[data-hero-particles]');
  if (particleWrap) {
    for (let i = 0; i < (isMobile() ? 14 : 22); i++) {
      const p = document.createElement('span');
      p.className = 'ms-hero-particle';
      p.style.left = `${Math.random() * 100}%`;
      p.style.top = `${Math.random() * 100}%`;
      particleWrap.appendChild(p);
    }
  }

  /* Hero entrance — one-time only, no continuous float.
     تصویر هیرو عنصر LCP صفحه است. هر انیمیشنی که opacity آن را از صفر شروع کند
     یا با تأخیر اجرا شود، لحظه ثبت LCP را همان‌قدر عقب می‌اندازد. برای همین
     تصویر و ظرفش فقط با transform جان می‌گیرند — بدون fade و بدون delay. */
  const heroStage = document.querySelector('.ms-hero-stage');
  if (heroStage && !isMobile()) {
    gsap.from('.ms-hero-stat', { opacity: 0, scale: 0.6, stagger: 0.15, duration: 0.7, ease: 'back.out(2.5)', delay: 0.45, clearProps: 'opacity,transform' });
    gsap.from('.ms-hero-slide.is-active .ms-hero-copy > *', {
      opacity: 0, y: 18, stagger: 0.08, duration: 0.55, ease: 'power3.out', delay: 0.35,
      clearProps: 'opacity,transform'
    });
    const activeImg = document.querySelector('.ms-hero-slide.is-active .ms-hero-product img');
    if (activeImg) {
      gsap.from(activeImg, { scale: 0.9, duration: 0.7, ease: 'back.out(1.6)', clearProps: 'transform' });
    }
  }

  /* Hero stat counter — once on load, NOT scroll */
  document.querySelectorAll('[data-hero-count]').forEach(el => {
    const target = parseInt(el.dataset.heroCount, 10);
    if (isNaN(target)) return;
    const obj = { val: 0 };
    gsap.to(obj, {
      val: target, duration: 1.4, delay: 0.8, ease: 'power2.out',
      onUpdate: () => { el.textContent = faDigits(Math.floor(obj.val)) + '+'; }
    });
  });

  /* Hero slider — one slide in layout, clean fade transitions */
  const slides = document.querySelectorAll('.ms-hero-slide[data-slide]');
  const dotsWrap = document.querySelector('[data-hero-dots]');
  const progressBar = document.querySelector('[data-hero-progress] span');
  const SLIDE_MS = 6000;

  function resetSlideStyles(slide) {
    if (!slide) return;
    gsap.set(slide, { clearProps: 'opacity,transform,visibility' });
    slide.querySelectorAll('.ms-hero-copy > *, .ms-hero-product img').forEach(el => {
      gsap.set(el, { clearProps: 'opacity,transform' });
    });
  }

  if (slides.length && dotsWrap) {
    let idx = 0, timer, animating = false;
    slides.forEach((slide, i) => {
      if (i !== 0) slide.classList.remove('is-active');
      else slide.classList.add('is-active');
      resetSlideStyles(slide);
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

      gsap.killTweensOf([prev, next]);

      const tl = gsap.timeline({
        onComplete: () => {
          resetSlideStyles(prev);
          resetSlideStyles(next);
          animating = false;
        }
      });
      tl.to(prev, { opacity: 0, duration: 0.28, ease: 'power2.in' });
      tl.add(() => {
        prev.classList.remove('is-active');
        next.classList.add('is-active');
        gsap.set(next, { opacity: 0 });
      });
      tl.to(next, { opacity: 1, duration: 0.38, ease: 'power3.out' });
      tl.from(next.querySelectorAll('.ms-hero-copy > *'), {
        opacity: 0, y: 16, stagger: 0.07, duration: 0.42, ease: 'power3.out',
      }, '-=0.22');
      const nextImg = next.querySelector('.ms-hero-product img');
      if (nextImg) {
        tl.from(nextImg, { opacity: 0, scale: 0.86, duration: 0.58, ease: 'back.out(1.5)' }, '-=0.38');
      }
    }

    function autoplay() {
      clearInterval(timer);
      if (progressBar) gsap.fromTo(progressBar, { width: '0%' }, { width: '100%', duration: SLIDE_MS / 1000, ease: 'none' });
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
  }

  if (!isMobile()) {
  /* Stories — flat fade-in only (no rotation/skew) */
  gsap.from('.ms-story', {
    opacity: 0, y: 10,
    stagger: 0.05, duration: 0.45, ease: 'power2.out', delay: 0.05,
    clearProps: 'opacity,transform'
  });
  }

  /* Closing counter only — NOT hero */
  document.querySelectorAll('[data-count]').forEach(el => {
    const target = parseInt(el.dataset.count, 10);
    if (isNaN(target)) return;
    ScrollTrigger.create({
      trigger: el, start: 'top 90%', once: true,
      onEnter: () => {
        const obj = { val: 0 };
        gsap.to(obj, { val: target, duration: 1.2, ease: 'power2.out', onUpdate: () => { el.textContent = faDigits(Math.floor(obj.val)) + '+'; } });
      }
    });
  });

  if (isMobile()) {
    window.addEventListener('load', () => { if (typeof ScrollTrigger !== 'undefined') ScrollTrigger.refresh(); });
    return;
  }

  reveal('.ms-features-band', { opacity: 0, y: 24, duration: 0.6, ease: 'power3.out' }, '.ms-features', 'top 92%');
  reveal('.ms-feature-card:not(.ms-feature-card--dup)', {
    opacity: 0, y: 28, scale: 0.92, stagger: 0.1, duration: 0.55, ease: 'back.out(1.7)', delay: 0.15
  }, '.ms-features');

  reveal('.ms-cat-chip', {
    opacity: 0, y: 24, scale: 0.85, stagger: 0.04, duration: 0.45, ease: 'back.out(1.8)'
  }, '.ms-cat-scroll');

  const catScroll = document.querySelector('[data-cat-scroll]');
  if (catScroll && isMobile()) {
    ScrollTrigger.create({
      trigger: catScroll, start: 'top 88%', once: true,
      onEnter: () => {
        gsap.to(catScroll, { scrollLeft: 80, duration: 0.9, ease: 'power2.inOut', onComplete: () =>
          gsap.to(catScroll, { scrollLeft: 0, duration: 0.6, ease: 'power2.out' }) });
      }
    });
  }

  reveal('.ms-featured-head > *', {
    opacity: 0, y: 24, stagger: 0.1, duration: 0.6, ease: 'power3.out'
  }, '.ms-featured-section');

  const featGrid = document.querySelector('[data-featured-grid]');
  if (featGrid) {
    reveal(featGrid.children, {
      opacity: 0, y: 28, scale: 0.94, stagger: 0.06, duration: 0.5, ease: 'back.out(1.6)'
    }, featGrid);
  }

  reveal('.ms-segments-head > *', {
    opacity: 0, y: 20, stagger: 0.08, duration: 0.5, ease: 'power3.out'
  }, '.ms-segments');

  reveal('.ms-seg-tile', {
    opacity: 0, y: 24, scale: 0.92, stagger: 0.08, duration: 0.5, ease: 'back.out(1.7)'
  }, '.ms-segments-grid');

  reveal('.ms-spark-divider-track > *', {
    opacity: 0, scale: 0.6, stagger: 0.1, duration: 0.55, ease: 'back.out(2)'
  }, '.ms-spark-divider', 'top 92%');

  reveal('.ms-spark-particle', {
    opacity: 0, y: 10, stagger: 0.05, duration: 0.4, ease: 'power2.out', delay: 0.2
  }, '.ms-spark-divider', 'top 92%');

  reveal('.ms-news-head > *', {
    opacity: 0, y: 22, stagger: 0.08, duration: 0.5, ease: 'power3.out'
  }, '.ms-news');

  const newsTrack = document.querySelector('[data-news-track]');
  if (newsTrack) {
    reveal(newsTrack.children, {
      opacity: 0, y: 32, scale: 0.94, stagger: 0.1, duration: 0.55, ease: 'power3.out', delay: 0.1
    }, newsTrack);
  }

  reveal('.ms-closing-card', {
    opacity: 0, y: 40, scale: 0.96, duration: 0.7, ease: 'power3.out'
  }, '.ms-closing');

  reveal('.ms-closing-stats li', {
    opacity: 0, scale: 0.8, stagger: 0.08, duration: 0.45, ease: 'back.out(2)'
  }, '.ms-closing-stats', 'top 92%');

  const offers = document.querySelector('[data-offers-scroll]');
  if (offers && offers.children.length) {
    reveal(offers.children, {
      opacity: 0, x: 36, stagger: 0.07, duration: 0.55, ease: 'power3.out'
    }, '.ms-offers', 'top 85%');
  }

  reveal('.ms-offers-head, .ms-countdown', {
    opacity: 0, y: 20, stagger: 0.1, duration: 0.5, ease: 'power3.out'
  }, '.ms-offers', 'top 90%');

  document.querySelectorAll('.ms-product, .ms-offer-card, .ms-cat-chip, .ms-seg-tile, .ms-feat-card, .ms-news-card').forEach(card => {
    card.addEventListener('touchstart', () => gsap.to(card, { scale: 0.96, duration: 0.1 }), { passive: true });
    card.addEventListener('touchend', () => gsap.to(card, { scale: 1, duration: 0.25, ease: 'back.out(2)' }), { passive: true });
  });

  if (!isMobile()) {
    /* Desktop hover lift handled by ux-desktop.css + desktop.js 3D tilt */
  }

  window.addEventListener('load', () => ScrollTrigger.refresh());
})();
