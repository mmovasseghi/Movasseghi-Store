document.addEventListener('DOMContentLoaded', () => {
  /* راهنمای دسته — همان آکاردئون محصول، پیش‌فرض بسته */
  (function initCategoryGuideFold() {
    const fold = document.querySelector('[data-cat-guide-fold]');
    if (!fold) return;

    const sync = () => {
      fold.classList.toggle('pd-article-fold--open', fold.open);
      const badge = fold.querySelector('.pd-article-fold__badge');
      if (badge) {
        badge.textContent = fold.open ? 'باز' : 'بسته';
        badge.hidden = false;
        badge.dataset.foldState = fold.open ? 'open' : 'closed';
      }
    };

    fold.addEventListener('toggle', sync);
    sync();
  })();

  const sheet = document.getElementById('filterSheet');
  if (!sheet) return;

  const title = sheet.querySelector('[data-filter-sheet-title]');

  function openSheet(mode) {
    const panel = sheet.querySelector('.filter-sheet-panel');
    if (panel) panel.scrollTop = 0;
    if (title) title.textContent = mode === 'sort' ? 'مرتب‌سازی' : 'فیلتر محصولات';
    sheet.hidden = false;
    document.body.classList.add('ms-drawer-open');
    requestAnimationFrame(() => {
      if (panel) panel.scrollTop = 0;
    });
  }

  function closeSheet() {
    sheet.hidden = true;
    document.body.classList.remove('ms-drawer-open');
  }

  document.querySelector('[data-open-filters]')?.addEventListener('click', () => openSheet('filter'));
  document.querySelector('[data-open-sort]')?.addEventListener('click', () => openSheet('sort'));
  sheet.querySelectorAll('[data-close-filters]').forEach(btn => btn.addEventListener('click', closeSheet));

  document.addEventListener('keydown', e => {
    if (e.key === 'Escape' && !sheet.hidden) closeSheet();
  });

  /* Product grid reveal */
  const reduced = matchMedia('(prefers-reduced-motion: reduce)').matches;
  const grid = document.querySelector('[data-catalog-grid]');
  if (grid && typeof gsap !== 'undefined' && !reduced) {
    if (typeof ScrollTrigger !== 'undefined') gsap.registerPlugin(ScrollTrigger);
    gsap.from(grid.children, {
      immediateRender: false,
      scrollTrigger: { trigger: grid, start: 'top 90%', once: true },
      opacity: 0,
      y: 24,
      scale: 0.96,
      stagger: 0.04,
      duration: 0.5,
      ease: 'power3.out',
      clearProps: 'opacity,transform'
    });
  }

  /* 3D tilt on desktop */
  if (matchMedia('(min-width: 768px)').matches && typeof gsap !== 'undefined' && !reduced) {
    document.querySelectorAll('.ms-product').forEach(card => {
      card.addEventListener('mousemove', e => {
        const r = card.getBoundingClientRect();
        const rx = ((e.clientY - r.top) / r.height - 0.5) * -6;
        const ry = ((e.clientX - r.left) / r.width - 0.5) * 6;
        gsap.to(card, { rotateX: rx, rotateY: ry, transformPerspective: 800, duration: 0.35, ease: 'power2.out' });
      });
      card.addEventListener('mouseleave', () => {
        gsap.to(card, { rotateX: 0, rotateY: 0, duration: 0.45, ease: 'power2.out' });
      });
    });
  }
});
