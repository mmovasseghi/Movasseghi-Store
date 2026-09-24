(function () {
  'use strict';

  const toolbar = document.getElementById('reviewsToolbar');
  if (!toolbar) return;

  const searchInput = document.getElementById('reviewsSearch');
  const contentEl = document.getElementById('reviewsTableContent');
  const statsEl = document.getElementById('reviewsStats');
  const listUrl = toolbar.dataset.listUrl || '/Admin/ProductReviewsAdmin/ListPartial';

  const storageKey = (() => {
    const p = new URLSearchParams(window.location.search);
    const pid = p.get('productId') || 'all';
    const st = p.get('status') || 'all';
    return `reviewsAdminSearch:${pid}:${st}`;
  })();

  let debounceTimer = null;
  let abortCtrl = null;
  let requestId = 0;

  function urlFilters() {
    const p = new URLSearchParams(window.location.search);
    return {
      productId: p.get('productId') || '',
      status: p.get('status') || '',
      q: p.get('q') || ''
    };
  }

  function currentSearchTerm() {
    return (searchInput?.value || '').trim();
  }

  function persistSearch() {
    const term = currentSearchTerm();
    try {
      if (term) sessionStorage.setItem(storageKey, term);
      else sessionStorage.removeItem(storageKey);
    } catch { /* ignore */ }
  }

  function params() {
    const q = new URLSearchParams();
    const term = currentSearchTerm();
    const { productId, status } = urlFilters();
    if (term) q.set('q', term);
    if (productId) q.set('productId', productId);
    if (status) q.set('status', status);
    return q;
  }

  function syncUrl() {
    const qs = params().toString();
    const url = qs ? `${window.location.pathname}?${qs}` : window.location.pathname;
    window.history.replaceState(null, '', url);
  }

  function syncHiddenQFields() {
    const term = currentSearchTerm();
    contentEl?.querySelectorAll('.js-reviews-q').forEach((el) => {
      el.value = term;
    });
  }

  function formatNum(n) {
    try { return Number(n).toLocaleString('fa-IR'); } catch { return String(n); }
  }

  function showToast(message, isError) {
    let el = document.getElementById('reviewsModerateToast');
    if (!el) {
      el = document.createElement('div');
      el.id = 'reviewsModerateToast';
      el.setAttribute('role', 'status');
      el.innerHTML = '<span class="kit-insight-icon" aria-hidden="true"></span><div class="kit-insight-body"><strong></strong></div>';
      const anchor = document.getElementById('reviewsTableCard');
      anchor?.parentNode?.insertBefore(el, anchor);
    }
    el.className = `kit-insight kit-rise ${isError ? 'kit-insight--risk' : 'kit-insight--ok'}`;
    const icon = el.querySelector('.kit-insight-icon');
    if (icon) icon.textContent = isError ? '!' : '✓';
    const strong = el.querySelector('strong');
    if (strong) strong.textContent = message;
    el.hidden = false;
    clearTimeout(showToast._t);
    showToast._t = setTimeout(() => { el.hidden = true; }, 4500);
  }

  function updateStats(meta) {
    if (!statsEl || !meta) return;
    const countEl = statsEl.querySelector('[data-stat="count"]');
    const approvedEl = statsEl.querySelector('[data-stat="approved"]');
    if (countEl) countEl.textContent = formatNum(meta.dataset.count || '0');
    if (approvedEl) approvedEl.textContent = formatNum(meta.dataset.approved || '0');
  }

  function restoreSearchInput() {
    if (!searchInput) return;
    const fromUrl = urlFilters().q;
    let stored = '';
    try { stored = sessionStorage.getItem(storageKey) || ''; } catch { /* ignore */ }
    const resolved = fromUrl || stored || searchInput.value.trim();
    if (resolved) searchInput.value = resolved;
    persistSearch();
    syncHiddenQFields();
  }

  async function fetchList() {
    const id = ++requestId;
    if (abortCtrl) abortCtrl.abort();
    abortCtrl = new AbortController();

    const caret = searchInput?.selectionStart;
    const termBefore = currentSearchTerm();

    contentEl?.setAttribute('aria-busy', 'true');
    toolbar.classList.add('is-loading');

    try {
      const qs = params().toString();
      const url = qs ? `${listUrl}?${qs}` : listUrl;
      const res = await fetch(url, {
        signal: abortCtrl.signal,
        headers: { 'X-Requested-With': 'XMLHttpRequest' },
        credentials: 'same-origin'
      });
      if (!res.ok) throw new Error('fetch failed');
      const html = await res.text();
      if (id !== requestId) return;

      contentEl.innerHTML = html;
      const meta = contentEl.querySelector('#reviewsListMeta');
      updateStats(meta);
      syncUrl();
      persistSearch();

      if (searchInput) {
        const keep = termBefore || currentSearchTerm();
        if (keep) searchInput.value = keep;
        syncHiddenQFields();
        if (typeof caret === 'number') {
          try { searchInput.setSelectionRange(caret, caret); } catch { /* ignore */ }
        }
      }

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

  restoreSearchInput();
  syncUrl();

  searchInput?.addEventListener('input', () => {
    persistSearch();
    syncHiddenQFields();
    scheduleFetch(280);
  });

  searchInput?.addEventListener('keydown', (e) => {
    if (e.key === 'Enter') {
      e.preventDefault();
      clearTimeout(debounceTimer);
      persistSearch();
      syncHiddenQFields();
      fetchList();
    }
  });

  async function moderateForm(form) {
    syncHiddenQFields();
    persistSearch();

    const fd = new FormData(form);
    const term = currentSearchTerm();
    fd.set('q', term);
    fd.append('ajax', 'true');

    const { productId, status } = urlFilters();
    if (productId && !fd.get('productId')) fd.set('productId', productId);
    if (status && !fd.get('status')) fd.set('status', status);

    const btn = form.querySelector('.js-reviews-moderate-btn, button[type="submit"]');
    if (btn) btn.disabled = true;

    try {
      const res = await fetch(form.action, {
        method: 'POST',
        body: fd,
        headers: {
          'X-Requested-With': 'XMLHttpRequest',
          Accept: 'application/json'
        },
        credentials: 'same-origin'
      });

      const ct = (res.headers.get('content-type') || '').toLowerCase();
      if (!res.ok) {
        showToast('خطا در ثبت وضعیت. دوباره تلاش کنید.', true);
        return;
      }
      if (ct.includes('application/json')) {
        const data = await res.json();
        showToast(data?.message || 'انجام شد.');
      } else {
        showToast('وضعیت ثبت شد.');
      }
      await fetchList();
    } catch (err) {
      console.error(err);
      showToast('ارتباط با سرور برقرار نشد.', true);
    } finally {
      if (btn) btn.disabled = false;
      searchInput?.focus({ preventScroll: true });
    }
  }

  document.addEventListener('submit', (e) => {
    const form = e.target;
    if (!(form instanceof HTMLFormElement)) return;
    if (!form.classList.contains('js-reviews-moderate')) return;
    if (!contentEl?.contains(form)) return;
    e.preventDefault();
    e.stopPropagation();
    moderateForm(form);
  }, true);

  contentEl?.addEventListener('click', (e) => {
    const btn = e.target.closest?.('.js-reviews-moderate-btn');
    if (!btn || !contentEl.contains(btn)) return;
    const form = btn.closest('form.js-reviews-moderate');
    if (!form) return;
    e.preventDefault();
    moderateForm(form);
  });

  toolbar.querySelectorAll('.kit-filter-group a[href]').forEach((link) => {
    link.addEventListener('click', (e) => {
      const term = currentSearchTerm();
      if (!term) return;
      try {
        const url = new URL(link.href, window.location.origin);
        if (!url.searchParams.get('q')) {
          e.preventDefault();
          url.searchParams.set('q', term);
          persistSearch();
          window.location.assign(url.pathname + url.search);
        }
      } catch { /* ignore */ }
    });
  });
})();
