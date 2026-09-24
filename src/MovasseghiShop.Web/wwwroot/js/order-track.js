(function () {
  const page = document.querySelector('[data-track-page]');
  if (!page) return;

  const reduced = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
  const hasGsap = typeof gsap !== 'undefined' && !reduced;

  if (page.dataset.trackPage === 'search') {
    const box = document.getElementById('ms-track-search');
    const form = page.querySelector('.ms-track-form');
    if (!hasGsap || !box) return;

    gsap.from(box, { y: 28, opacity: 0, duration: 0.65, ease: 'power3.out' });
    gsap.from(box.querySelectorAll('.ms-track-field, .ms-track-submit'), {
      y: 18,
      opacity: 0,
      duration: 0.5,
      stagger: 0.08,
      ease: 'power2.out',
      delay: 0.12
    });
    const err = document.getElementById('ms-track-error');
    if (err) {
      gsap.from(err, { scale: 0.96, opacity: 0, duration: 0.35, ease: 'back.out(2)' });
    }
    if (form) {
      form.addEventListener('submit', function () {
        const btn = form.querySelector('.ms-track-submit');
        if (btn) btn.disabled = true;
      });
    }
    return;
  }

  if (page.dataset.trackPage !== 'result') return;

  const step = parseInt(page.dataset.trackStep || '0', 10);
  const cancelled = page.dataset.trackCancelled === '1';
  const root = document.getElementById('ms-track-result');
  const fill = document.getElementById('ms-track-fill');

  if (!hasGsap || !root) {
    if (fill && !cancelled) {
      const pct = Math.max(0, Math.min(100, (step / 4) * 100));
      fill.style.width = pct + '%';
    }
    return;
  }

  const tl = gsap.timeline({ defaults: { ease: 'power3.out' } });

  tl.from(root.querySelector('.ms-track-result__hero'), {
    y: 36,
    opacity: 0,
    duration: 0.7
  })
    .from(
      root.querySelector('.ms-track-result__hero-icon'),
      { scale: 0.4, rotation: -12, duration: 0.55, ease: 'back.out(2)' },
      '-=0.45'
    )
    .from(
      root.querySelectorAll('.ms-track-result__kicker, .ms-track-result__number, .ms-track-result__status'),
      { y: 12, opacity: 0, stagger: 0.06, duration: 0.4 },
      '-=0.25'
    );

  if (!cancelled) {
    const steps = root.querySelectorAll('.ms-track-step');
    tl.from('.ms-track-timeline', { opacity: 0, y: 16, duration: 0.45 }, '-=0.1');
    tl.from(steps, {
      scale: 0.6,
      opacity: 0,
      stagger: 0.07,
      duration: 0.4,
      ease: 'back.out(1.8)'
    }, '-=0.2');

    if (fill) {
      const pct = Math.max(8, Math.min(100, (step / 4) * 100));
      tl.to(fill, { width: pct + '%', duration: 1.1, ease: 'power2.inOut' }, '-=0.35');
    }

    const activeDot = root.querySelector('.ms-track-step--active .ms-track-step__dot');
    if (activeDot) {
      gsap.to(activeDot, {
        scale: 1.08,
        duration: 1.2,
        ease: 'sine.inOut',
        yoyo: true,
        repeat: -1
      });
    }
  }

  tl.from(
    root.querySelectorAll('.ms-track-card'),
    { y: 20, opacity: 0, stagger: 0.06, duration: 0.45 },
    cancelled ? '-=0.15' : '-=0.5'
  )
    .from(root.querySelector('.ms-track-items'), { y: 22, opacity: 0, duration: 0.5 }, '-=0.25')
    .from(
      root.querySelectorAll('.ms-track-item'),
      { x: 16, opacity: 0, stagger: 0.05, duration: 0.38 },
      '-=0.3'
    )
    .from(
      root.querySelectorAll('.ms-track-result__actions .ms-order-success__btn'),
      { y: 10, opacity: 0, stagger: 0.06, duration: 0.35 },
      '-=0.2'
    );

  if (!cancelled && step >= 4) {
    const icon = root.querySelector('.ms-track-result__hero-icon');
    if (icon) {
      gsap.fromTo(
        icon,
        { boxShadow: '0 14px 36px rgba(45, 106, 79, 0.35)' },
        {
          boxShadow: '0 14px 42px rgba(82, 183, 136, 0.55)',
          duration: 1.4,
          repeat: 1,
          yoyo: true,
          ease: 'sine.inOut'
        }
      );
    }
  }
})();
