(function () {
  'use strict';
  const reduced = matchMedia('(prefers-reduced-motion: reduce)').matches;
  const hasGsap = typeof gsap !== 'undefined';
  const isDesktop = () => matchMedia('(min-width: 768px)').matches;
  if (hasGsap) gsap.registerPlugin(ScrollTrigger);

  if ('scrollRestoration' in history) history.scrollRestoration = 'manual';
  if (!location.hash) window.scrollTo(0, 0);
  window.addEventListener('pageshow', e => {
    if (!location.hash) window.scrollTo(0, 0);
    if (hasGsap && typeof ScrollTrigger !== 'undefined') ScrollTrigger.refresh(true);
  });

  /* ── Cinematic mobile drawer ── */
  const drawer = document.getElementById('mobileNav');
  const menuBtns = document.querySelectorAll('[data-menu-toggle]');
  const drawerPanel = drawer?.querySelector('.ms-drawer-panel');
  const drawerBg = drawer?.querySelector('.ms-drawer-bg');
  let drawerOpen = false;

  function openDrawer() {
    if (!drawer || drawerOpen) return;
    drawerOpen = true;
    drawer.removeAttribute('hidden');
    drawer.setAttribute('aria-hidden', 'false');
    document.body.classList.add('ms-drawer-open');
    menuBtns.forEach(b => { b.classList.add('is-open'); b.setAttribute('aria-expanded', 'true'); });
    drawer.classList.add('is-open');

    if (hasGsap && !reduced) {
      gsap.set(drawerPanel, { x: '100%' });
      gsap.fromTo(drawerBg, { opacity: 0 }, { opacity: 1, duration: 0.4, ease: 'power2.out' });
      gsap.to(drawerPanel, { x: 0, opacity: 1, duration: 0.55, ease: 'power3.out' });
      gsap.from(drawer.querySelectorAll('.ms-drawer-link, .ms-drawer-cat, .ms-drawer-cat-all, .ms-drawer-quick-btn'), {
        opacity: 0, x: 28, stagger: 0.045, duration: 0.42, ease: 'power3.out', delay: 0.12
      });
      gsap.from(drawer.querySelector('.ms-drawer-cta'), { opacity: 0, y: 16, duration: 0.45, ease: 'back.out(1.6)', delay: 0.35 });
    } else if (drawerPanel) {
      drawerPanel.style.transform = 'translateX(0)';
    }

    drawerPanel?.querySelector('[data-menu-close]')?.focus();
  }

  function closeDrawer() {
    if (!drawer || !drawerOpen) return;
    const done = () => {
      drawer.setAttribute('hidden', '');
      drawer.setAttribute('aria-hidden', 'true');
      drawer.classList.remove('is-open');
      document.body.classList.remove('ms-drawer-open');
      menuBtns.forEach(b => { b.classList.remove('is-open'); b.setAttribute('aria-expanded', 'false'); });
      drawerOpen = false;
    };

    if (hasGsap && !reduced) {
      gsap.to(drawerPanel, { x: '100%', opacity: 0.5, duration: 0.38, ease: 'power2.in' });
      gsap.to(drawerBg, { opacity: 0, duration: 0.32, onComplete: done });
    } else {
      if (drawerPanel) drawerPanel.style.transform = 'translateX(105%)';
      done();
    }
  }

  menuBtns.forEach(b => b.addEventListener('click', () => drawerOpen ? closeDrawer() : openDrawer()));
  drawer?.querySelectorAll('[data-menu-close]').forEach(el => el.addEventListener('click', closeDrawer));
  drawer?.querySelectorAll('[data-drawer-link]').forEach(el => el.addEventListener('click', () => closeDrawer()));

  window.closeMobileDrawer = closeDrawer;

  /* ── Modal system ── */
  const modal = document.getElementById('appModal');
  const modalSheet = modal?.querySelector('[data-modal-sheet]');
  let modalOpen = false;

  function openModal(html, opts = {}) {
    if (!modal || !modalSheet) return;
    modalSheet.innerHTML = html;
    modalSheet.classList.toggle('ms-modal-sheet--auth', !!opts.auth);
    modalSheet.classList.toggle('ms-modal-sheet--cart', !!opts.cart);
    modalSheet.classList.toggle('ms-modal-sheet--add', !!opts.add);
    modal.removeAttribute('hidden');
    document.body.classList.add('ms-modal-open');
    modalOpen = true;
    bindModalClose();
    if (hasGsap && !reduced) {
      gsap.fromTo(modal.querySelector('.ms-modal-backdrop'), { opacity: 0, backdropFilter: 'blur(0px)' }, { opacity: 1, backdropFilter: 'blur(12px)', duration: 0.45 });
      if (isDesktop() || opts.auth || opts.add) {
        gsap.fromTo(modalSheet, { scale: 0.88, opacity: 0, y: 20 }, { scale: 1, opacity: 1, y: 0, duration: opts.auth ? 0.65 : 0.5, ease: 'back.out(1.35)' });
      } else {
        gsap.fromTo(modalSheet, { y: '110%', opacity: 0, scale: 0.94 }, { y: 0, opacity: 1, scale: 1, duration: opts.auth ? 0.72 : 0.55, ease: opts.auth ? 'back.out(1.35)' : 'power3.out' });
      }
      if (opts.cart) animateCartDrawer(false);
      if (opts.add) animateAddConfirm();
    }
  }

  function animateAddConfirm() {
    const box = modalSheet?.querySelector('[data-cart-add-confirm]');
    if (!box || !hasGsap) return;
    gsap.from(box.querySelector('.ms-cart-add-confirm__icon'), { scale: 0.4, opacity: 0, duration: 0.5, ease: 'back.out(2)' });
    gsap.from(box.querySelector('.ms-cart-add-confirm__title'), { y: 10, opacity: 0, duration: 0.4, ease: 'power2.out', delay: 0.08 });
    gsap.from(box.querySelectorAll('.ms-cart-add-confirm__pill'), { y: 8, opacity: 0, duration: 0.32, stagger: 0.05, ease: 'power2.out', delay: 0.14 });
    gsap.from(box.querySelectorAll('.ms-cart-add-confirm__btn'), { y: 12, opacity: 0, duration: 0.38, stagger: 0.06, ease: 'power2.out', delay: 0.2 });
  }

  const CART_DRAWER_HEAVY_ITEM_THRESHOLD = 16;
  const CART_DRAWER_MAX_STAGGER_ITEMS = 10;

  function animateCartDrawer(lightRefresh = false) {
    const drawer = modalSheet?.querySelector('.ms-cart-drawer');
    if (!drawer || !hasGsap) return;

    gsap.killTweensOf(drawer.querySelectorAll('[data-cart-item], .ms-cart-drawer-item-chips .cart-chip, .ms-cart-drawer-action-rail, .ms-cart-drawer-foot, .ms-cart-drawer-total-row, .ms-cart-drawer-checkout, [data-cart-tier-nudge], [data-cart-premium]'));

    const items = [...drawer.querySelectorAll('[data-cart-item]')];
    const itemCount = items.length;
    const heavy = itemCount > CART_DRAWER_HEAVY_ITEM_THRESHOLD;
    const footRows = drawer.querySelectorAll('.ms-cart-drawer-total-row');
    const chips = drawer.querySelectorAll('.ms-cart-drawer-item-chips .cart-chip');

    if (lightRefresh || heavy) {
      const foot = drawer.querySelector('.ms-cart-drawer-foot');
      if (foot) gsap.fromTo(foot, { opacity: 0.92 }, { opacity: 1, duration: 0.2, ease: 'power1.out' });
      return;
    }

    gsap.from(drawer.querySelector('.ms-cart-drawer-head'), { y: -14, opacity: 0, duration: 0.42, ease: 'power2.out' });
    gsap.from(drawer.querySelector('.ms-cart-drawer-close'), { scale: 0.6, opacity: 0, duration: 0.35, ease: 'back.out(2)', delay: 0.05 });

    const toAnimate = items.slice(0, CART_DRAWER_MAX_STAGGER_ITEMS);
    toAnimate.forEach(el => el.classList.add('is-entering'));
    gsap.from(toAnimate, {
      y: 18, opacity: 0, scale: 0.97, duration: 0.48, stagger: 0.05, ease: 'power2.out', delay: 0.06,
      onComplete: () => toAnimate.forEach(el => el.classList.remove('is-entering'))
    });

    const rails = drawer.querySelectorAll('.ms-cart-drawer-action-rail');
    if (rails.length && itemCount <= 12) {
      gsap.from(rails, { scaleX: 0.92, opacity: 0, duration: 0.38, stagger: 0.05, ease: 'power2.out', delay: 0.1, transformOrigin: 'right center' });
    }

    if (chips.length && chips.length <= 36) {
      gsap.from(chips, { y: 6, opacity: 0, scale: 0.92, duration: 0.32, stagger: 0.02, ease: 'power2.out', delay: 0.14 });
    }

    gsap.from(drawer.querySelector('.ms-cart-drawer-foot'), { y: 20, opacity: 0, duration: 0.45, ease: 'power2.out', delay: 0.16 });
    if (footRows.length) {
      gsap.from(footRows, { x: 12, opacity: 0, duration: 0.35, stagger: 0.06, ease: 'power2.out', delay: 0.22 });
    }

    const checkout = drawer.querySelector('.ms-cart-drawer-checkout');
    if (checkout) {
      gsap.from(checkout, { scale: 0.94, opacity: 0, duration: 0.4, ease: 'back.out(1.6)', delay: 0.28 });
    }

    const tierNudge = drawer.querySelector('[data-cart-tier-nudge]');
    if (tierNudge) {
      gsap.from(tierNudge, { y: 16, opacity: 0, scale: 0.96, duration: 0.52, ease: 'back.out(1.5)', delay: 0.12 });
      const fill = tierNudge.querySelector('[data-tier-fill]');
      if (fill) {
        const target = fill.style.width;
        fill.style.width = '0%';
        gsap.to(fill, { width: target, duration: 0.95, ease: 'power2.out', delay: 0.3 });
      }
      const callout = tierNudge.querySelector('.cart-tier-nudge__callout');
      if (callout) gsap.from(callout, { x: 10, opacity: 0, duration: 0.38, ease: 'power2.out', delay: 0.38 });
      const amount = tierNudge.querySelector('.cart-tier-nudge__amount');
      if (amount && !reduced) {
        gsap.fromTo(amount, { scale: 1 }, { scale: 1.05, duration: 0.35, yoyo: true, repeat: 1, ease: 'power1.inOut', delay: 0.55 });
      }
    }

    const premium = drawer.querySelector('[data-cart-premium]');
    if (premium) {
      gsap.from(premium, { y: 12, opacity: 0, duration: 0.45, ease: 'back.out(1.4)', delay: 0.2 });
    }
  }

  function pulseCartQty(item) {
    if (!item) return;
    const valEl = item.querySelector('[data-cart-qty-val]');
    const totalEl = item.querySelector('[data-cart-line-total]');
    const rail = item.querySelector('.ms-cart-drawer-action-rail');
    valEl?.classList.remove('is-bump');
    totalEl?.classList.remove('is-pulse');
    void valEl?.offsetWidth;
    valEl?.classList.add('is-bump');
    totalEl?.classList.add('is-pulse');
    if (hasGsap && !reduced) {
      gsap.fromTo(item, { scale: 1 }, { scale: 1.012, duration: 0.12, yoyo: true, repeat: 1, ease: 'power1.out' });
      if (rail) gsap.fromTo(rail, { boxShadow: '0 0 0 rgba(45,106,79,0)' }, { boxShadow: '0 0 0 3px rgba(45,106,79,.12)', duration: 0.2, yoyo: true, repeat: 1 });
    }
  }

  function closeModal() {
    if (!modal || !modalOpen) return;
    const done = () => {
      modal.setAttribute('hidden', '');
      modalSheet.innerHTML = '';
      modalSheet.classList.remove('ms-modal-sheet--auth', 'ms-modal-sheet--cart', 'ms-modal-sheet--add');
      document.body.classList.remove('ms-modal-open');
      modalOpen = false;
    };
    if (hasGsap && !reduced) {
      if (isDesktop() || modalSheet.classList.contains('ms-modal-sheet--auth') || modalSheet.classList.contains('ms-modal-sheet--add')) {
        gsap.to(modalSheet, { scale: 0.92, opacity: 0, duration: 0.3, ease: 'power2.in', onComplete: done });
      } else {
        gsap.to(modalSheet, { y: '100%', opacity: 0, duration: 0.35, ease: 'power2.in', onComplete: done });
      }
      gsap.to(modal.querySelector('.ms-modal-backdrop'), { opacity: 0, duration: 0.3 });
    } else done();
  }

  function bindModalClose() {
    modal?.querySelectorAll('[data-modal-close]').forEach(el =>
      el.addEventListener('click', closeModal));
  }

  document.addEventListener('keydown', e => {
    if (e.key !== 'Escape') return;
    if (drawerOpen) { closeDrawer(); return; }
    if (modalOpen) closeModal();
  });

  /* ── Auth modal ── */
  async function openAuthModal() {
    closeDrawer();
    const res = await fetch('/Account/Modal');
    if (!res.ok) return;
    openModal(await res.text(), { auth: true });
    const panel = modalSheet?.querySelector('[data-auth-panel]');
    if (panel) delete panel.dataset.authReady;
    if (window.MsAuth?.init) window.MsAuth.init(panel);
  }

  document.querySelectorAll('[data-open-auth]').forEach(el =>
    el.addEventListener('click', e => { e.preventDefault(); openAuthModal(); }));

  if (new URLSearchParams(location.search).get('auth') === '1') {
    history.replaceState(null, '', location.pathname);
    openAuthModal();
  }

  /* ── Cart modal ── */
  function updateCartBadge(count) {
    const label = count > 99 ? '99+' : String(count);
    document.querySelectorAll('[data-cart-badge]').forEach(el => {
      el.textContent = label;
      el.classList.toggle('is-visible', count > 0);
      el.hidden = count <= 0;
      el.setAttribute('aria-hidden', count <= 0 ? 'true' : 'false');
    });
    document.querySelectorAll('[data-open-cart]').forEach(el => {
      el.setAttribute('aria-label', count > 0 ? `سبد خرید (${count} قلم)` : 'سبد خرید');
    });
  }

  async function refreshCartBadge() {
    try {
      const res = await fetch('/Cart/Count');
      if (!res.ok) return;
      const data = await res.json();
      updateCartBadge(data.count ?? 0);
    } catch { /* ignore */ }
  }

  async function openCartModal() {
    closeDrawer();
    const res = await fetch('/Cart/Panel');
    if (!res.ok) return;
    openModal(await res.text(), { cart: true });
    bindCartActions();
    refreshCartBadge();
  }

  function bindCartActions() {
    modalSheet?.querySelectorAll('[data-cart-qty]').forEach(btn => {
      btn.addEventListener('click', async () => {
        const item = btn.closest('[data-variant-id]');
        const id = item?.dataset.variantId;
        const valEl = item?.querySelector('[data-cart-qty-val]');
        let qty = parseInt(valEl?.textContent || '1', 10) + parseInt(btn.dataset.cartQty, 10);
        if (qty < 1) qty = 1;
        pulseCartQty(item);
        await cartAjax('/Cart/Update', { variantId: id, cartons: qty });
      });
    });
    modalSheet?.querySelectorAll('[data-cart-remove]').forEach(btn => {
      btn.addEventListener('click', async () => {
        const item = btn.closest('[data-variant-id]');
        const id = item?.dataset.variantId;
        if (hasGsap && !reduced && item) {
          gsap.to(item, { x: -24, opacity: 0, height: 0, marginBottom: 0, paddingTop: 0, paddingBottom: 0, duration: 0.28, ease: 'power2.in' });
        }
        await cartAjax('/Cart/Remove', { variantId: id });
      });
    });
  }

  async function cartAjax(url, data) {
    const token = document.querySelector('#msAntiForgery input[name="__RequestVerificationToken"]')?.value;
    const fd = new FormData();
    Object.entries(data).forEach(([k, v]) => fd.append(k, v));
    fd.append('ajax', 'true');
    if (token) fd.append('__RequestVerificationToken', token);
    const res = await fetch(url, { method: 'POST', body: fd });
    if (res.ok) {
      modalSheet.innerHTML = await res.text();
      bindModalClose();
      bindCartActions();
      refreshCartBadge();
      animateCartDrawer(true);
    }
  }

  window.MsCart = {
    updateBadge: updateCartBadge,
    refreshBadge: refreshCartBadge,
    openDrawer: openCartModal,
    showAddConfirm(html) {
      closeDrawer();
      openModal(html, { add: true });
      bindModalClose();
      const count = modalSheet?.querySelector('[data-cart-add-confirm]')?.dataset.cartCount;
      if (count) updateCartBadge(parseInt(count, 10));
    }
  };

  document.querySelectorAll('[data-open-cart]').forEach(el =>
    el.addEventListener('click', e => { e.preventDefault(); openCartModal(); }));

  /* ── Search autocomplete ── */
  const searchInput = document.getElementById('globalSearch');
  const suggestions = document.getElementById('searchSuggestions');
  let searchTimer;

  if (searchInput && suggestions) {
    const searchForm = searchInput.closest('[data-search-form]');
    searchForm?.addEventListener('submit', async e => {
      const q = searchInput.value.trim();
      if (q.length < 2) return;
      try {
        const res = await fetch('/Catalog/Suggest?q=' + encodeURIComponent(q));
        const data = await res.json();
        const exact = Array.isArray(data) ? data : (data.exact || []);
        if (!exact.length || !exact[0].slug) return;
        const top = exact[0];
        const slugMatch = top.slug === q || decodeURIComponent(top.slug) === q;
        const nameStarts = top.name && top.name.startsWith(q);
        if (slugMatch || nameStarts) {
          e.preventDefault();
          window.location.href = (window.appUrl || (u => u))('/Shop/Product/' + encodeURIComponent(top.slug));
        }
      } catch { /* fall through to catalog */ }
    });

    searchInput.addEventListener('input', () => {
      clearTimeout(searchTimer);
      const q = searchInput.value.trim();
      if (q.length < 2) { suggestions.hidden = true; suggestions.innerHTML = ''; return; }
      searchTimer = setTimeout(async () => {
        const res = await fetch('/Catalog/Suggest?q=' + encodeURIComponent(q));
        const data = await res.json();
        const exact = Array.isArray(data) ? data : (data.exact || []);
        const similar = Array.isArray(data) ? [] : (data.similar || []);
        const items = exact.length ? exact : similar;
        if (!items.length) { suggestions.hidden = true; suggestions.innerHTML = ''; return; }

        const renderItem = (item, i, isSimilar) => {
          const img = item.imageUrl
            ? `<div class="ms-product-shot"><div class="ms-product-stage"><img class="ms-product-img-el" src="${(window.appUrl || (u => u))(item.imageUrl)}" alt="" width="44" height="44" loading="lazy"/></div></div>`
            : '<div class="ms-product-empty" aria-hidden="true"></div>';
          return `
          <li role="option" data-slug="${item.slug}" data-index="${i}"${isSimilar ? ' class="is-similar"' : ''}>
            <span class="ms-suggest-img">${img}</span>
            <span class="ms-suggest-text"><strong>${item.name}</strong>${item.productCode ? `<small>کد ${item.productCode}</small>` : ''}</span>
          </li>`;
        };

        let html = '';
        if (exact.length) {
          html = exact.map((item, i) => renderItem(item, i, false)).join('');
        } else {
          html = `<li class="ms-suggest-note" role="presentation">دقیقاً پیدا نشد — پیشنهاد نزدیک:</li>` +
            similar.map((item, i) => renderItem(item, i, true)).join('');
        }
        suggestions.innerHTML = html;
        suggestions.hidden = false;
        if (hasGsap && !reduced) gsap.from(suggestions.querySelectorAll('li:not(.ms-suggest-note)'), { opacity: 0, y: 8, stagger: 0.04, duration: 0.3, ease: 'power2.out' });
      }, 200);
    });

    suggestions.addEventListener('click', e => {
      const li = e.target.closest('[data-slug]');
      if (li) window.location.href = (window.appUrl || (u => u))('/Shop/Product/' + encodeURIComponent(li.dataset.slug));
    });

    document.addEventListener('click', e => {
      if (!e.target.closest('[data-search-form]')) suggestions.hidden = true;
    });

    searchInput.addEventListener('focus', () => {
      searchInput.closest('.ms-search')?.classList.add('is-focused');
    });
    searchInput.addEventListener('blur', () => {
      searchInput.closest('.ms-search')?.classList.remove('is-focused');
    });
  }

  /* ── Promo bar animation ── */
  const promoBar = document.querySelector('[data-promo-bar]');
  if (promoBar && hasGsap && !reduced) {
    gsap.from(promoBar, { y: -40, opacity: 0, duration: 0.6, ease: 'power3.out', delay: 0.1 });
  }

  /* ── Footer animations ── */
  const footer = document.querySelector('[data-footer]');
  if (footer && hasGsap && !reduced) {
    gsap.from('[data-footer-brand]', {
      scrollTrigger: { trigger: footer, start: 'top 92%', once: true },
      opacity: 0, y: 36, duration: 0.7, ease: 'power3.out'
    });
    gsap.from('[data-footer-col]', {
      scrollTrigger: { trigger: footer, start: 'top 88%', once: true },
      opacity: 0, y: 28, stagger: 0.1, duration: 0.55, ease: 'power3.out', delay: 0.15
    });
    gsap.from('[data-footer-copy]', {
      scrollTrigger: { trigger: footer, start: 'top 80%', once: true },
      opacity: 0, duration: 0.5, delay: 0.4
    });

    const footerBand = document.querySelector('[data-footer-band]');
    if (footerBand) {
      gsap.fromTo(footerBand,
        { scaleY: 0.08, opacity: 0.3 },
        {
          scaleY: 1, opacity: 0.55, ease: 'none',
          scrollTrigger: { trigger: footer, start: 'top 98%', end: 'bottom bottom', scrub: 1 }
        }
      );
    }
  }

  const footerTrust = document.querySelector('[data-footer-trust]');
  if (footerTrust && !reduced) {
    const rows = footerTrust.querySelectorAll('.ms-footer-trust-row');
    if (rows.length === 2) {
      footerTrust.style.animationDuration = Math.max(18, Math.min(40, rows[0].scrollWidth / 30)) + 's';
    }
  }

  const fabCall = document.querySelector('[data-fab-call]');
  const tabbar = document.querySelector('.ms-tabbar');
  function syncFabPosition() {
    if (!fabCall || !tabbar) return;
    if (getComputedStyle(tabbar).display === 'none') {
      fabCall.style.bottom = 'calc(1.25rem + var(--safe-b))';
      return;
    }
    const filterBar = document.querySelector('.mobile-filter-bar');
    const filterVisible = filterBar && getComputedStyle(filterBar).display !== 'none';
    const extra = filterVisible ? filterBar.offsetHeight + 8 : 0;
    fabCall.style.bottom = `${tabbar.offsetHeight + extra + 14}px`;
  }
  syncFabPosition();
  window.addEventListener('resize', syncFabPosition);
  window.addEventListener('orientationchange', syncFabPosition);
  if (document.fonts?.ready) document.fonts.ready.then(syncFabPosition);
  setTimeout(syncFabPosition, 150);

  if (fabCall && hasGsap && !reduced) {
    gsap.from(fabCall, { scale: 0, opacity: 0, duration: 0.6, ease: 'back.out(2)', delay: 0.8 });
  }

  /* ── Global reveal (skip sections animated in home.js) ── */
  if (hasGsap && !reduced) {
    gsap.utils.toArray('.ms-reveal:not(.ms-hero):not(.ms-stories):not(.ms-features):not(.ms-offers):not(.ms-cat-section):not(.ms-featured-section):not(.ms-segments):not(.ms-closing):not(.ms-news)').forEach(el => {
      gsap.from(el, {
        scrollTrigger: { trigger: el, start: 'top 92%', once: true },
        opacity: 0, y: 32, duration: 0.65, ease: 'power3.out',
        clearProps: 'opacity,transform'
      });
    });
  }
})();
