(function () {
  'use strict';
  const form = document.getElementById('home-section-form');
  const out = document.getElementById('ConfigJsonField');
  const key = window.__HOME_SECTION_KEY;
  if (!form || !out || !key) return;

  function val(el, f) {
    return el.querySelector(`[data-f="${f}"]`)?.value?.trim() ?? '';
  }

  form.addEventListener('submit', () => {
    let config;
    if (key === 'hero') {
      const slides = [...form.querySelectorAll('[data-hero-slide]')].map(block => ({
        kicker: val(block, 'kicker'),
        title: val(block, 'title'),
        titleEm: val(block, 'titleEm') || null,
        desc: val(block, 'desc'),
        cta1Text: val(block, 'cta1Text'),
        cta1Url: val(block, 'cta1Url') || '/Catalog',
        cta2Text: val(block, 'cta2Text') || null,
        cta2Url: val(block, 'cta2Url') || null,
      }));
      const trustItems = [...form.querySelectorAll('[data-trust]')]
        .map(i => i.value.trim())
        .filter(Boolean);
      config = { slides, trustItems };
    } else if (key === 'features') {
      const bandText = document.getElementById('features-band')?.value?.trim() ?? '';
      const cards = [...form.querySelectorAll('[data-feature-card]')].map(block => ({
        icon: val(block, 'icon') || 'shield',
        title: val(block, 'title'),
        desc: val(block, 'desc'),
      }));
      config = { bandText, cards };
    } else if (key === 'wholesale') {
      const badge = document.getElementById('wholesale-badge')?.value?.trim() ?? '';
      const segments = [...form.querySelectorAll('[data-wholesale-seg]')].map(block => ({
        title: val(block, 'title'),
        desc: val(block, 'desc'),
        icon: val(block, 'icon') || 'store',
        url: val(block, 'url') || '/Catalog',
      }));
      config = { badge, segments };
    } else {
      const badge = document.getElementById('simple-badge')?.value?.trim() ?? '';
      config = { badge };
    }
    out.value = JSON.stringify(config);
  });
})();
