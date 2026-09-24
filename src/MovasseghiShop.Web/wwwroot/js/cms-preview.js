(function () {
  'use strict';

  const panel = document.querySelector('[data-cms-preview-panel]');
  if (!panel) return;

  const scaler = panel.querySelector('[data-preview-scaler]');
  const iframe = panel.querySelector('[data-preview-iframe]');
  const refreshBtn = panel.querySelector('[data-preview-refresh]');
  const fabToPreview = document.querySelector('[data-fab-to-preview]');
  const fabIframeTop = panel.querySelector('[data-fab-iframe-top]');

  const DESKTOP_W = 1280;
  const DESKTOP_H = 2400;
  const mobileMq = window.matchMedia('(max-width: 960px)');

  function scaleIframe() {
    if (!scaler || !iframe) return;
    const containerW = scaler.clientWidth;
    if (containerW <= 0) return;

    const scale = Math.min(1, containerW / DESKTOP_W);
    iframe.style.width = DESKTOP_W + 'px';
    iframe.style.height = DESKTOP_H + 'px';
    iframe.style.transform = 'scale(' + scale + ')';
    iframe.style.transformOrigin = 'top right';
    scaler.style.height = Math.round(DESKTOP_H * scale) + 'px';
  }

  function refreshPreview() {
    if (!iframe) return;
    try {
      const url = new URL(iframe.getAttribute('src') || '/', window.location.origin);
      url.searchParams.set('_pv', Date.now().toString());
      iframe.src = url.pathname + url.search + url.hash;
    } catch {
      iframe.src = iframe.src;
    }
  }

  function scrollIframeTop() {
    if (!iframe) return;
    try {
      iframe.contentWindow?.scrollTo({ top: 0, behavior: 'smooth' });
    } catch {
      refreshPreview();
    }
  }

  function scrollToPreview() {
    panel.scrollIntoView({ behavior: 'smooth', block: 'start' });
    setTimeout(scaleIframe, 350);
  }

  function updateFabVisibility() {
    if (!fabToPreview || !mobileMq.matches) {
      if (fabToPreview) fabToPreview.hidden = true;
      return;
    }
    const rect = panel.getBoundingClientRect();
    const previewVisible = rect.top < window.innerHeight * 0.55 && rect.bottom > 120;
    fabToPreview.hidden = previewVisible;
  }

  refreshBtn?.addEventListener('click', refreshPreview);
  fabToPreview?.addEventListener('click', scrollToPreview);
  fabIframeTop?.addEventListener('click', scrollIframeTop);

  window.addEventListener('resize', scaleIframe);
  window.addEventListener('scroll', updateFabVisibility, { passive: true });
  mobileMq.addEventListener('change', function () {
    scaleIframe();
    updateFabVisibility();
  });

  iframe?.addEventListener('load', scaleIframe);
  scaleIframe();
  updateFabVisibility();
})();
