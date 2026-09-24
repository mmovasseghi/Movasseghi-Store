(function () {
  'use strict';

  const mq = matchMedia('(min-width: 768px)');
  if (!mq.matches) return;

  const hasGsap = typeof gsap !== 'undefined';
  const reduced = matchMedia('(prefers-reduced-motion: reduce)').matches;

  document.body.classList.add('ms-desktop');

  /* Header shrink on scroll */
  let scrolled = false;
  function onScroll() {
    const now = window.scrollY > 48;
    if (now === scrolled) return;
    scrolled = now;
    document.body.classList.toggle('ms-dt-scrolled', now);
  }
  window.addEventListener('scroll', onScroll, { passive: true });
  onScroll();

  if (!hasGsap || reduced) return;

  gsap.registerPlugin(ScrollTrigger);

  /* Hero subtle parallax on mouse */
  const heroStage = document.querySelector('.ms-hero-stage');
  function bindHeroParallax() {
    const heroProduct = document.querySelector('.ms-hero-slide.is-active .ms-hero-product img');
    if (!heroStage || !heroProduct) return;
    heroStage.onmousemove = e => {
      const r = heroStage.getBoundingClientRect();
      const x = (e.clientX - r.left) / r.width - 0.5;
      const y = (e.clientY - r.top) / r.height - 0.5;
      gsap.to(heroProduct, { x: x * 18, y: y * 12, duration: 0.8, ease: 'power2.out' });
      gsap.to('.ms-hero-orb-a', { x: x * -24, y: y * -16, duration: 1, ease: 'power2.out' });
      gsap.to('.ms-hero-orb-b', { x: x * 20, y: y * 14, duration: 1, ease: 'power2.out' });
    };
    heroStage.onmouseleave = () => {
      gsap.to(heroProduct, { x: 0, y: 0, duration: 0.6, ease: 'power2.out' });
      gsap.to('.ms-hero-orb-a, .ms-hero-orb-b', { x: 0, y: 0, duration: 0.6, ease: 'power2.out' });
    };
  }
  bindHeroParallax();
  document.querySelector('[data-hero-dots]')?.addEventListener('click', () => setTimeout(bindHeroParallax, 400));

  /* Magnetic hover on primary CTAs */
  document.querySelectorAll('.ms-hero-cta, .ms-offers-more, .ms-drawer-cta').forEach(btn => {
    btn.addEventListener('mousemove', e => {
      const r = btn.getBoundingClientRect();
      const x = e.clientX - r.left - r.width / 2;
      const y = e.clientY - r.top - r.height / 2;
      gsap.to(btn, { x: x * 0.18, y: y * 0.18, duration: 0.35, ease: 'power2.out' });
    });
    btn.addEventListener('mouseleave', () => {
      gsap.to(btn, { x: 0, y: 0, duration: 0.5, ease: 'elastic.out(1, 0.6)' });
    });
  });

  /* Product cards 3D tilt */
  document.querySelectorAll('.ms-feat-card, .ms-offer-card, .ms-product').forEach(card => {
    card.addEventListener('mousemove', e => {
      const r = card.getBoundingClientRect();
      const rx = ((e.clientY - r.top) / r.height - 0.5) * -8;
      const ry = ((e.clientX - r.left) / r.width - 0.5) * 8;
      gsap.to(card, { rotateX: rx, rotateY: ry, transformPerspective: 800, duration: 0.4, ease: 'power2.out' });
    });
    card.addEventListener('mouseleave', () => {
      gsap.to(card, { rotateX: 0, rotateY: 0, duration: 0.55, ease: 'power2.out' });
    });
  });

  window.addEventListener('load', () => ScrollTrigger.refresh());
  mq.addEventListener('change', e => {
    if (!e.matches) location.reload();
  });
})();
