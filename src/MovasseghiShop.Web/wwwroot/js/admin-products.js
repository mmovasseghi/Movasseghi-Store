(function () {
  'use strict';

  const toolbar = document.getElementById('productsToolbar');
  if (!toolbar) return;

  const searchInput = document.getElementById('productsSearch');
  const categorySelect = document.getElementById('productsCategory');
  const statusSelect = document.getElementById('productsStatus');
  const contentEl = document.getElementById('productsTableContent');
  const statsEl = document.getElementById('productsStats');
  const listUrl = toolbar.dataset.listUrl || '/Admin/Products/ListPartial';
  const highlightUrl = toolbar.dataset.highlightUrl || '/Admin/Products/SetHomeHighlight';
  const modal = document.getElementById('homeHighlightModal');

  let modalProductId = null;
  let modalSpecial = false;
  let modalFeatured = false;
  let modalProductName = '';

  function antiforgeryToken() {
    return toolbar.querySelector('input[name="__RequestVerificationToken"]')?.value
      || document.querySelector('input[name="__RequestVerificationToken"]')?.value
      || '';
  }

  let debounceTimer = null;
  let abortCtrl = null;
  let requestId = 0;

  function params() {
    const q = new URLSearchParams();
    const term = (searchInput?.value || '').trim();
    const cat = categorySelect?.value || '';
    const status = statusSelect?.value || '';
    if (term) q.set('q', term);
    if (cat) q.set('categoryId', cat);
    if (status) q.set('status', status);
    return q;
  }

  function syncUrl() {
    const q = params();
    const qs = q.toString();
    const url = qs ? `${window.location.pathname}?${qs}` : window.location.pathname;
    window.history.replaceState(null, '', url);
  }

  function formatNum(n) {
    try { return Number(n).toLocaleString('fa-IR'); } catch { return String(n); }
  }

  function readMeta() {
    return contentEl?.querySelector('#productsListMeta');
  }

  function updateStats(meta) {
    if (!statsEl || !meta) return;
    const countEl = statsEl.querySelector('[data-stat="count"]');
    const activeEl = statsEl.querySelector('[data-stat="active"]');
    const weakEl = statsEl.querySelector('[data-stat="weak"]');
    const specialEl = statsEl.querySelector('[data-stat="special"]');
    const featuredEl = statsEl.querySelector('[data-stat="featured"]');
    if (countEl) countEl.textContent = formatNum(meta.dataset.count || '0');
    if (activeEl) activeEl.textContent = formatNum(meta.dataset.active || '0');
    if (specialEl) specialEl.textContent = formatNum(meta.dataset.specialTotal || meta.dataset.special || '0');
    if (featuredEl) featuredEl.textContent = formatNum(meta.dataset.featuredTotal || meta.dataset.featured || '0');
    if (weakEl) {
      const weak = parseInt(meta.dataset.weak || '0', 10);
      weakEl.textContent = formatNum(weak);
      weakEl.classList.toggle('kit-stat-value--gold', weak > 0);
      weakEl.classList.toggle('kit-stat-value--ok', weak === 0);
    }
  }

  function updateMeters(specialTotal, featuredTotal, max) {
    if (!modal) return;
    const sp = modal.querySelector('[data-hi-count-special]');
    const ft = modal.querySelector('[data-hi-count-featured]');
    const spBar = modal.querySelector('[data-hi-meter-special]');
    const ftBar = modal.querySelector('[data-hi-meter-featured]');
    if (sp) sp.textContent = `${formatNum(specialTotal)}/${formatNum(max)}`;
    if (ft) ft.textContent = `${formatNum(featuredTotal)}/${formatNum(max)}`;
    if (spBar) spBar.style.width = `${Math.min(100, (specialTotal / max) * 100)}%`;
    if (ftBar) ftBar.style.width = `${Math.min(100, (featuredTotal / max) * 100)}%`;
  }

  function syncModalCards() {
    if (!modal) return;
    const goldCard = modal.querySelector('[data-hi-role="special"]');
    const diaCard = modal.querySelector('[data-hi-role="featured"]');
    const spState = modal.querySelector('[data-hi-state-special]');
    const ftState = modal.querySelector('[data-hi-state-featured]');
    goldCard?.classList.toggle('is-active', modalSpecial);
    diaCard?.classList.toggle('is-active', modalFeatured);
    if (spState) {
      spState.textContent = modalSpecial ? 'فعال' : 'خاموش';
      spState.classList.toggle('is-on', modalSpecial);
      spState.classList.toggle('is-off', !modalSpecial);
    }
    if (ftState) {
      ftState.textContent = modalFeatured ? 'فعال' : 'خاموش';
      ftState.classList.toggle('is-on', modalFeatured);
      ftState.classList.toggle('is-off', !modalFeatured);
    }
  }

  function showModalError(msg) {
    const el = modal?.querySelector('[data-hi-error]');
    if (!el) return;
    if (!msg) {
      el.hidden = true;
      el.textContent = '';
      return;
    }
    el.hidden = false;
    el.textContent = msg;
  }

  function openModal(btn) {
    if (!modal) return;
    modalProductId = btn.getAttribute('data-product-id');
    modalProductName = btn.getAttribute('data-product-name') || '';
    modalSpecial = btn.getAttribute('data-special') === '1';
    modalFeatured = btn.getAttribute('data-featured') === '1';
    modal.querySelector('[data-hi-product-name]').textContent = modalProductName;
    const meta = readMeta();
    const max = parseInt(meta?.dataset.highlightMax || '8', 10);
    const sp = parseInt(meta?.dataset.specialTotal || '0', 10);
    const ft = parseInt(meta?.dataset.featuredTotal || '0', 10);
    updateMeters(sp, ft, max);
    syncModalCards();
    showModalError('');
    modal.hidden = false;
    modal.setAttribute('aria-hidden', 'false');
    document.body.classList.add('ms-drawer-open');
    modal.querySelector('.kit-hi-card')?.focus();
  }

  function closeModal() {
    if (!modal) return;
    modal.hidden = true;
    modal.setAttribute('aria-hidden', 'true');
    document.body.classList.remove('ms-drawer-open');
    modalProductId = null;
  }

  function updatePickerRow(productId, special, featured) {
    const btn = contentEl?.querySelector(`[data-highlight-picker][data-product-id="${productId}"]`);
    if (!btn) return;
    btn.setAttribute('data-special', special ? '1' : '0');
    btn.setAttribute('data-featured', featured ? '1' : '0');
    btn.querySelector('.kit-hi-seg--gold')?.classList.toggle('is-on', special);
    btn.querySelector('.kit-hi-seg--diamond')?.classList.toggle('is-on', featured);
    const row = btn.closest('tr');
    row?.classList.toggle('is-home-special', special);
    row?.classList.toggle('is-home-featured', featured);
  }

  async function postHighlight(role, enabled) {
    if (!modalProductId) return null;
    const token = antiforgeryToken();
    const fd = new FormData();
    if (token) fd.append('__RequestVerificationToken', token);
    fd.append('role', role);
    fd.append('enabled', enabled ? 'true' : 'false');

    const res = await fetch(`${highlightUrl}/${modalProductId}`, {
      method: 'POST',
      body: fd,
      headers: { 'X-Requested-With': 'XMLHttpRequest' }
    });
    const data = await res.json().catch(() => ({}));
    if (!res.ok || !data.ok) {
      throw new Error(data.message || 'ذخیره نشد');
    }
    return data;
  }

  async function applyRole(role) {
    const enabling = role === 'special' ? !modalSpecial : !modalFeatured;
    showModalError('');
    const cards = modal?.querySelectorAll('.kit-hi-card');
    cards?.forEach(c => { c.disabled = true; });
    try {
      const data = await postHighlight(role, enabling);
      modalSpecial = !!data.isHomeSpecialOffer;
      modalFeatured = !!data.isHomeFeatured;
      syncModalCards();
      updatePickerRow(data.id, modalSpecial, modalFeatured);
      const meta = readMeta();
      if (meta) {
        meta.dataset.specialTotal = String(data.specialTotal);
        meta.dataset.featuredTotal = String(data.featuredTotal);
        updateStats(meta);
      }
      updateMeters(data.specialTotal, data.featuredTotal, data.max || 8);
      window.AdminKit?.toast?.(enabling ? 'به صفحه اصلی اضافه شد' : 'از صفحه اصلی برداشته شد', 'success');
      const status = statusSelect?.value || '';
      if ((status === 'special' && !modalSpecial) || (status === 'featured' && !modalFeatured)) {
        await fetchList();
        closeModal();
      }
    } catch (err) {
      showModalError(err.message || 'خطا در ذخیره');
    } finally {
      cards?.forEach(c => { c.disabled = false; });
    }
  }

  async function clearBoth() {
    showModalError('');
    try {
      if (modalSpecial) await postHighlight('special', false);
      if (modalFeatured) await postHighlight('featured', false);
      modalSpecial = false;
      modalFeatured = false;
      syncModalCards();
      if (modalProductId) updatePickerRow(modalProductId, false, false);
      await fetchList();
      closeModal();
      window.AdminKit?.toast?.('از هر دو بلوک حذف شد', 'info');
    } catch (err) {
      showModalError(err.message || 'خطا در حذف');
    }
  }

  async function fetchList() {
    const id = ++requestId;
    if (abortCtrl) abortCtrl.abort();
    abortCtrl = new AbortController();

    contentEl?.setAttribute('aria-busy', 'true');
    toolbar.classList.add('is-loading');

    try {
      const qs = params().toString();
      const url = qs ? `${listUrl}?${qs}` : listUrl;
      const res = await fetch(url, {
        signal: abortCtrl.signal,
        headers: { 'X-Requested-With': 'XMLHttpRequest' }
      });
      if (!res.ok) throw new Error('fetch failed');
      const html = await res.text();
      if (id !== requestId) return;

      contentEl.innerHTML = html;
      const meta = readMeta();
      updateStats(meta);
      syncUrl();

      if (window.AdminKit?.init) window.AdminKit.init(contentEl);
    } catch (err) {
      if (err.name !== 'AbortError') console.error(err);
    } finally {
      if (id === requestId) {
        contentEl?.setAttribute('aria-busy', 'false');
        toolbar.classList.remove('is-loading');
      }
    }
  }

  function scheduleFetch(delay) {
    clearTimeout(debounceTimer);
    debounceTimer = setTimeout(fetchList, delay);
  }

  searchInput?.addEventListener('input', () => scheduleFetch(280));
  categorySelect?.addEventListener('change', () => fetchList());
  statusSelect?.addEventListener('change', () => fetchList());

  searchInput?.addEventListener('keydown', (e) => {
    if (e.key === 'Enter') {
      e.preventDefault();
      clearTimeout(debounceTimer);
      fetchList();
    }
  });

  contentEl?.addEventListener('click', (e) => {
    const btn = e.target.closest('[data-highlight-picker]');
    if (!btn || btn.disabled) return;
    e.preventDefault();
    openModal(btn);
  });

  modal?.querySelectorAll('[data-hi-close]').forEach(el => {
    el.addEventListener('click', () => closeModal());
  });

  modal?.querySelector('[data-hi-role="special"]')?.addEventListener('click', () => applyRole('special'));
  modal?.querySelector('[data-hi-role="featured"]')?.addEventListener('click', () => applyRole('featured'));
  modal?.querySelector('[data-hi-clear]')?.addEventListener('click', () => clearBoth());

  document.addEventListener('keydown', (e) => {
    if (e.key === 'Escape' && modal && !modal.hidden) closeModal();
  });
})();
