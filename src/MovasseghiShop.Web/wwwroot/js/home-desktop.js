(function () {
  'use strict';

  if (!matchMedia('(min-width: 768px)').matches) return;
  if (typeof gsap === 'undefined' || matchMedia('(prefers-reduced-motion: reduce)').matches) return;

  gsap.registerPlugin(ScrollTrigger);

  /* Hero keyboard navigation */
  const slides = document.querySelectorAll('.ms-hero-slide[data-slide]');
  const dotsWrap = document.querySelector('[data-hero-dots]');
  if (slides.length > 1 && dotsWrap) {
    document.addEventListener('keydown', e => {
      if (e.key !== 'ArrowLeft' && e.key !== 'ArrowRight') return;
      const dots = dotsWrap.querySelectorAll('button');
      const active = [...dots].findIndex(d => d.classList.contains('is-active'));
      if (active < 0) return;
      const next = e.key === 'ArrowLeft' ? active - 1 : active + 1;
      const idx = (next + dots.length) % dots.length;
      dots[idx]?.click();
    });
  }

  /* Features band shine sweep */
  const featBand = document.querySelector('.ms-features-band');
  if (featBand) {
    gsap.to('.ms-features-band-shine', {
      x: '120%',
      duration: 3,
      repeat: -1,
      ease: 'none',
      repeatDelay: 2
    });
  }

  /* Countdown subtle glow pulse */
  const countdown = document.querySelector('.ms-countdown');
  if (countdown) {
    gsap.fromTo(countdown,
      { boxShadow: '0 0 0 0 rgba(255,255,255,0.25)' },
      { boxShadow: '0 0 0 14px rgba(255,255,255,0)', duration: 2, repeat: -1, ease: 'power1.out' }
    );
  }

  /* Spark divider gems float */
  gsap.utils.toArray('.ms-spark-gem').forEach((gem, i) => {
    gsap.to(gem, {
      y: '+=8',
      duration: 2 + i * 0.3,
      repeat: -1,
      yoyo: true,
      ease: 'sine.inOut'
    });
  });

  /* Footer aurora drift */
  gsap.utils.toArray('.ms-footer-orb').forEach((orb, i) => {
    gsap.to(orb, {
      x: i % 2 ? 30 : -30,
      y: i % 2 ? -20 : 20,
      duration: 8 + i * 2,
      repeat: -1,
      yoyo: true,
      ease: 'sine.inOut'
    });
  });
})();
