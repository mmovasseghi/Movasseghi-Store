(function () {
  'use strict';

  const hasGsap = typeof gsap !== 'undefined';
  const reduced = matchMedia('(prefers-reduced-motion: reduce)').matches;
  const tabsRoot = document.querySelector('[data-account-tabs]');
  const logoutSheet = document.getElementById('logoutSheet');

  if (tabsRoot) {
    const tabs = [...tabsRoot.querySelectorAll('[data-tab]')];
    const panels = [...tabsRoot.querySelectorAll('[data-panel]')];
    const indicator = tabsRoot.querySelector('[data-tab-indicator]');

    function moveIndicator(tab) {
      if (!indicator || !tab) return;
      indicator.style.width = tab.offsetWidth + 'px';
      indicator.style.transform = `translateX(${tab.offsetLeft}px)`;
    }

    function activate(tabName) {
      tabs.forEach(t => {
        const on = t.dataset.tab === tabName;
        t.classList.toggle('is-active', on);
        t.setAttribute('aria-selected', on ? 'true' : 'false');
      });
      panels.forEach(p => {
        const on = p.dataset.panel === tabName;
        p.hidden = !on;
        p.classList.toggle('is-active', on);
        if (on && hasGsap && !reduced) {
          gsap.fromTo(p, { opacity: 0, y: 12 }, { opacity: 1, y: 0, duration: 0.4, ease: 'power3.out' });
        }
      });
      const activeTab = tabs.find(t => t.dataset.tab === tabName);
      moveIndicator(activeTab);
    }

    tabs.forEach(tab => {
      tab.addEventListener('click', () => activate(tab.dataset.tab));
    });

    activate('profile');
    window.addEventListener('resize', () => {
      const active = tabs.find(t => t.classList.contains('is-active'));
      moveIndicator(active);
    });

    document.querySelectorAll('[data-order-timeline]').forEach(tl => {
      if (hasGsap && !reduced) {
        gsap.from(tl.querySelectorAll('.ms-order-dot'), {
          scale: 0, opacity: 0, stagger: 0.08, duration: 0.45, ease: 'back.out(2.5)', delay: 0.15
        });
      }
    });
  }

  document.querySelectorAll('[data-toggle-password]').forEach(btn => {
    btn.addEventListener('click', () => {
      const input = document.getElementById(btn.dataset.togglePassword);
      if (!input) return;
      const show = input.type === 'password';
      input.type = show ? 'text' : 'password';
      btn.textContent = show ? '🙈' : '👁';
    });
  });

  const passwordForm = document.querySelector('[data-password-form]');
  const passwordMsg = document.querySelector('[data-password-msg]');
  passwordForm?.addEventListener('submit', async e => {
    e.preventDefault();
    const btn = passwordForm.querySelector('[data-password-save]');
    btn?.classList.add('is-loading');
    if (passwordMsg) passwordMsg.hidden = true;
    const fd = new FormData(passwordForm);
    fd.append('ajax', 'true');
    try {
      const res = await fetch('/Account/ChangePassword', { method: 'POST', body: fd });
      const data = await res.json().catch(() => ({}));
      if (!passwordMsg) return;
      passwordMsg.hidden = false;
      if (res.ok) {
        passwordMsg.textContent = data.message || '✓ رمز عبور ذخیره شد';
        passwordMsg.className = 'ms-account-save-msg is-success';
        passwordForm.reset();
        const hadCurrent = !!passwordForm.querySelector('[name="currentPassword"]');
        if (!hadCurrent) {
          setTimeout(() => window.location.reload(), 600);
        }
      } else {
        passwordMsg.textContent = data.error || 'خطا در تغییر رمز';
        passwordMsg.className = 'ms-account-save-msg is-error';
      }
    } finally {
      btn?.classList.remove('is-loading');
    }
  });

  const profileForm = document.querySelector('[data-profile-form]');
  const profileMsg = document.querySelector('[data-profile-msg]');
  profileForm?.addEventListener('submit', async e => {
    e.preventDefault();
    const btn = profileForm.querySelector('[data-profile-save]');
    btn?.classList.add('is-loading');
    profileMsg && (profileMsg.hidden = true);
    const fd = new FormData(profileForm);
    fd.append('ajax', 'true');
    try {
      const res = await fetch('/Account/UpdateProfile', { method: 'POST', body: fd });
      const data = await res.json().catch(() => ({}));
      if (!profileMsg) return;
      profileMsg.hidden = false;
      if (res.ok) {
        profileMsg.textContent = '✓ تغییرات با موفقیت ذخیره شد';
        profileMsg.className = 'ms-account-save-msg is-success';
        const nameEl = document.querySelector('[data-account-name]');
        if (nameEl && data.displayName) nameEl.textContent = data.displayName;
        const avatar = document.querySelector('.ms-account-avatar');
        if (avatar && data.firstName) {
          const ini = data.firstName.slice(0, 1) + (data.lastName?.slice(0, 1) || '');
          avatar.textContent = ini.toUpperCase();
        }
        const chips = document.querySelector('[data-account-chips]');
        let companyChip = document.querySelector('[data-company-chip]');
        if (data.companyName) {
          if (!companyChip && chips) {
            companyChip = document.createElement('span');
            companyChip.className = 'ms-account-chip';
            companyChip.dataset.companyChip = '';
            chips.appendChild(companyChip);
          }
          if (companyChip) companyChip.textContent = data.companyName;
        } else if (companyChip) {
          companyChip.remove();
        }
        if (hasGsap && !reduced) gsap.fromTo(profileMsg, { scale: 0.95 }, { scale: 1, duration: 0.35, ease: 'back.out(2)' });
      } else {
        profileMsg.textContent = data.error || 'خطا در ذخیره';
        profileMsg.className = 'ms-account-save-msg is-error';
      }
    } finally {
      btn?.classList.remove('is-loading');
    }
  });

  function openLogoutSheet() {
    if (!logoutSheet) return;
    logoutSheet.hidden = false;
    document.body.classList.add('ms-sheet-open');
    if (hasGsap && !reduced) {
      gsap.fromTo(logoutSheet.querySelector('.ms-sheet-backdrop'), { opacity: 0 }, { opacity: 1, duration: 0.3 });
      gsap.fromTo(logoutSheet.querySelector('.ms-sheet-panel'), { y: '100%' }, { y: 0, duration: 0.45, ease: 'back.out(1.6)' });
    }
  }

  function closeLogoutSheet() {
    if (!logoutSheet) return;
    const done = () => {
      logoutSheet.hidden = true;
      document.body.classList.remove('ms-sheet-open');
    };
    if (hasGsap && !reduced) {
      gsap.to(logoutSheet.querySelector('.ms-sheet-panel'), { y: '100%', duration: 0.3, ease: 'power2.in', onComplete: done });
      gsap.to(logoutSheet.querySelector('.ms-sheet-backdrop'), { opacity: 0, duration: 0.25 });
    } else done();
  }

  document.querySelector('[data-logout-open]')?.addEventListener('click', openLogoutSheet);
  logoutSheet?.querySelectorAll('[data-logout-close]').forEach(el => el.addEventListener('click', closeLogoutSheet));
  document.addEventListener('keydown', e => {
    if (e.key === 'Escape' && logoutSheet && !logoutSheet.hidden) closeLogoutSheet();
  });

  document.querySelectorAll('.ms-account-card').forEach(card => {
    card.addEventListener('mouseenter', () => {
      if (reduced || !hasGsap) return;
      gsap.to(card, { y: -4, duration: 0.35, ease: 'power2.out' });
    });
    card.addEventListener('mouseleave', () => {
      if (reduced || !hasGsap) return;
      gsap.to(card, { y: 0, duration: 0.35, ease: 'power2.out' });
    });
  });

  /* ── Addresses + map ── */
  const addrForm = document.querySelector('[data-address-form]');
  const addrMsg = document.querySelector('[data-addr-msg]');
  const addrReset = document.querySelector('[data-addr-reset]');
  const coordsDisplay = document.querySelector('[data-addr-coords-display]');
  const mapOverlay = document.querySelector('[data-map-overlay]');
  const mapOverlayText = document.querySelector('[data-map-overlay-text]');
  const geocodePreview = document.querySelector('[data-geocode-preview]');
  const geocodePreviewText = document.querySelector('[data-geocode-preview-text]');
  const locationStatus = document.querySelector('[data-location-status]');
  const mapEl = document.getElementById('accountMap');
  let map, marker;
  let mapReady = false;
  let geocodeAbort = null;
  let geocodeTimer = null;
  let geocodeSeq = 0;
  let skipGeocode = false;

  const geoErrorMessages = {
    1: 'دسترسی به موقعیت رد شد. از تنظیمات مرورگر یا گوشی اجازه دهید.',
    2: 'موقعیت در دسترس نیست. GPS یا اینترنت را بررسی کنید.',
    3: 'درخواست موقعیت زمان‌بر شد. دوباره تلاش کنید.'
  };

  function showAddrMsg(text, ok) {
    if (!addrMsg) return;
    addrMsg.hidden = !text;
    addrMsg.textContent = text;
    addrMsg.className = 'ms-account-save-msg ' + (ok ? 'is-success' : 'is-error');
  }

  function setMapLoading(on, text) {
    if (!mapOverlay) return;
    mapOverlay.hidden = !on;
    if (mapOverlayText && text) mapOverlayText.textContent = text;
  }

  function showGeocodePreview(text) {
    if (!geocodePreview || !geocodePreviewText) return;
    geocodePreviewText.textContent = text;
    geocodePreview.hidden = false;
    if (hasGsap && !reduced) {
      gsap.fromTo(geocodePreview, { opacity: 0, y: 8 }, { opacity: 1, y: 0, duration: 0.35, ease: 'power2.out' });
    }
  }

  function hideGeocodePreview() {
    geocodePreview && (geocodePreview.hidden = true);
  }

  function updateLocationStatus(lat, lng) {
    if (!locationStatus) return;
    const has = lat != null && lng != null;
    locationStatus.hidden = !has;
    if (has && hasGsap && !reduced) {
      gsap.fromTo(locationStatus.querySelector('.ms-addr-location-dot'), { scale: 0.5 }, { scale: 1, duration: 0.4, ease: 'back.out(2)' });
    }
  }

  function updateCoordsDisplay(lat, lng) {
    if (!coordsDisplay) return;
    if (lat != null && lng != null) {
      coordsDisplay.hidden = false;
      coordsDisplay.textContent = `${Number(lat).toFixed(5)}, ${Number(lng).toFixed(5)}`;
    } else {
      coordsDisplay.hidden = true;
      coordsDisplay.textContent = '';
    }
    updateLocationStatus(lat, lng);
  }

  function createPulseMarker(lat, lng) {
    const icon = L.divIcon({
      className: 'ms-addr-marker-wrap',
      html: '<span class="ms-addr-marker-pin"></span><span class="ms-addr-marker-pulse"></span>',
      iconSize: [32, 32],
      iconAnchor: [16, 32]
    });
    return L.marker([lat, lng], { icon });
  }

  function resetMapLoading() {
    setMapLoading(false);
    if (geocodeAbort) {
      geocodeAbort.abort();
      geocodeAbort = null;
    }
    clearTimeout(geocodeTimer);
    geocodeTimer = null;
  }

  async function reverseGeocode(lat, lng) {
    const seq = ++geocodeSeq;
    if (geocodeAbort) geocodeAbort.abort();
    geocodeAbort = new AbortController();
    const timeoutId = setTimeout(() => geocodeAbort?.abort(), 12000);
    setMapLoading(true, 'در حال استخراج آدرس…');
    try {
      const res = await fetch(
        `/Account/ReverseGeocode?lat=${encodeURIComponent(lat)}&lng=${encodeURIComponent(lng)}`,
        { signal: geocodeAbort.signal, credentials: 'same-origin', headers: { Accept: 'application/json' } }
      );
      if (seq !== geocodeSeq) return;

      const ct = res.headers.get('content-type') || '';
      if (!ct.includes('application/json')) {
        showAddrMsg('خطا در دریافت آدرس. صفحه را رفرش کنید.', false);
        return;
      }

      const data = await res.json().catch(() => ({}));
      if (!res.ok) {
        showAddrMsg(data.error || 'آدرسی برای این نقطه یافت نشد. آدرس را دستی وارد کنید.', false);
        return;
      }
      if (skipGeocode) return;

      const prov = addrForm?.querySelector('[data-addr-province]');
      const city = addrForm?.querySelector('[data-addr-city]');
      const full = addrForm?.querySelector('[data-addr-full]');
      if (prov && data.province) prov.value = data.province;
      if (city && data.city) city.value = data.city;
      if (full && data.fullAddress) full.value = data.fullAddress;
      [prov, city, full].forEach(el => {
        if (el?.value) el.classList.add('is-geocoded');
        setTimeout(() => el?.classList.remove('is-geocoded'), 1200);
      });
      const preview = [data.province, data.city, data.fullAddress].filter(Boolean).join('، ');
      if (preview || data.displayName) showGeocodePreview(preview || data.displayName);
      showAddrMsg('', true);
    } catch (err) {
      if (seq !== geocodeSeq) return;
      if (err.name === 'AbortError') {
        showAddrMsg('استخراج آدرس طول کشید. موقعیت ذخیره شد — آدرس را دستی تکمیل کنید.', false);
      } else {
        showAddrMsg('خطا در دریافت آدرس. دوباره تلاش کنید.', false);
      }
    } finally {
      clearTimeout(timeoutId);
      if (seq === geocodeSeq) setMapLoading(false);
    }
  }

  function scheduleGeocode(lat, lng) {
    clearTimeout(geocodeTimer);
    geocodeTimer = setTimeout(() => reverseGeocode(lat, lng), 400);
  }

  function setLatLng(lat, lng, { geocode = true } = {}) {
    addrForm?.querySelector('[data-addr-lat]')?.setAttribute('value', lat ?? '');
    addrForm?.querySelector('[data-addr-lng]')?.setAttribute('value', lng ?? '');
    if (addrForm?.querySelector('[data-addr-lat]')) addrForm.querySelector('[data-addr-lat]').value = lat ?? '';
    if (addrForm?.querySelector('[data-addr-lng]')) addrForm.querySelector('[data-addr-lng]').value = lng ?? '';
    updateCoordsDisplay(lat, lng);
    if (!map) return;
    if (lat != null && lng != null) {
      if (!marker) marker = createPulseMarker(lat, lng).addTo(map);
      else marker.setLatLng([lat, lng]);
      map.setView([lat, lng], Math.max(map.getZoom(), 14));
      if (geocode && !skipGeocode) scheduleGeocode(lat, lng);
    } else {
      if (geocodeSeq) geocodeAbort?.abort();
      clearTimeout(geocodeTimer);
      resetMapLoading();
      hideGeocodePreview();
      if (marker) {
        map.removeLayer(marker);
        marker = null;
      }
    }
  }

  function syncMapFromForm() {
    const latRaw = addrForm?.querySelector('[data-addr-lat]')?.value;
    const lngRaw = addrForm?.querySelector('[data-addr-lng]')?.value;
    const lat = latRaw ? parseFloat(latRaw) : null;
    const lng = lngRaw ? parseFloat(lngRaw) : null;
    if (lat != null && lng != null && !Number.isNaN(lat) && !Number.isNaN(lng)) {
      skipGeocode = true;
      setLatLng(lat, lng, { geocode: false });
      skipGeocode = false;
    }
  }

  function initMap() {
    if (!mapEl || typeof L === 'undefined' || mapReady) return;
    mapReady = true;
    map = L.map(mapEl, { zoomControl: true }).setView([35.6892, 51.389], 11);
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      maxZoom: 19,
      attribution: '© OpenStreetMap'
    }).addTo(map);
    map.on('click', e => setLatLng(e.latlng.lat, e.latlng.lng));
    setTimeout(() => {
      map.invalidateSize();
      syncMapFromForm();
    }, 200);
  }

  function resetAddrForm() {
    if (!addrForm) return;
    skipGeocode = true;
    addrForm.reset();
    addrForm.querySelector('[data-addr-id]').value = '';
    setLatLng(null, null, { geocode: false });
    skipGeocode = false;
    hideGeocodePreview();
    addrReset?.setAttribute('hidden', '');
    showAddrMsg('', true);
  }

  function fillAddrForm(card) {
    if (!addrForm || !card) return;
    skipGeocode = true;
    addrForm.querySelector('[data-addr-id]').value = card.dataset.addressId || '';
    addrForm.querySelector('[data-addr-label]').value = card.dataset.label || '';
    addrForm.querySelector('[data-addr-province]').value = card.dataset.province || '';
    addrForm.querySelector('[data-addr-city]').value = card.dataset.city || '';
    addrForm.querySelector('[data-addr-full]').value = card.dataset.full || '';
    addrForm.querySelector('[data-addr-plaque]').value = card.dataset.plaque || '';
    addrForm.querySelector('[data-addr-unit]').value = card.dataset.unit || '';
    addrForm.querySelector('[data-addr-postal]').value = card.dataset.postal || '';
    addrForm.querySelector('[data-addr-default]').checked = card.dataset.default === 'true';
    const lat = card.dataset.lat ? parseFloat(card.dataset.lat) : null;
    const lng = card.dataset.lng ? parseFloat(card.dataset.lng) : null;
    setLatLng(lat, lng, { geocode: false });
    skipGeocode = false;
    hideGeocodePreview();
    addrReset?.removeAttribute('hidden');
    addrForm.scrollIntoView({ behavior: 'smooth', block: 'start' });
  }

  function formatAddressLine(a) {
    let line = `${a.province}، ${a.city} — ${a.fullAddress}`;
    if (a.plaque) line += `، پلاک ${a.plaque}`;
    if (a.unit) line += `، واحد ${a.unit}`;
    return line;
  }

  function buildAddressCard(a) {
    const el = document.createElement('article');
    el.className = 'ms-address-card ms-address-card--saved';
    el.dataset.addressId = a.id;
    el.dataset.label = a.label || '';
    el.dataset.province = a.province;
    el.dataset.city = a.city;
    el.dataset.full = a.fullAddress;
    el.dataset.postal = a.postalCode || '';
    el.dataset.plaque = a.plaque || '';
    el.dataset.unit = a.unit || '';
    el.dataset.lat = a.latitude ?? '';
    el.dataset.lng = a.longitude ?? '';
    el.dataset.default = a.isDefault ? 'true' : 'false';
    const title = a.label || `${a.province}، ${a.city}`;
    el.innerHTML = `
      <div class="ms-address-card-head">
        <strong>${title}</strong>
        ${a.isDefault ? '<span class="ms-address-badge">پیش‌فرض</span>' : ''}
      </div>
      <p>${formatAddressLine(a)}</p>
      ${a.postalCode ? `<small dir="ltr">کد پستی: ${a.postalCode}</small>` : ''}
      ${a.latitude != null && a.longitude != null ? `<small class="ms-address-coords" dir="ltr">📍 ${Number(a.latitude).toFixed(5)}, ${Number(a.longitude).toFixed(5)}</small>` : ''}
      <div class="ms-address-card-actions">
        <button type="button" class="ms-auth-link" data-edit-address>ویرایش</button>
        <button type="button" class="ms-auth-link ms-auth-link--danger" data-delete-address>حذف</button>
      </div>`;
    bindAddressCard(el);
    return el;
  }

  function bindAddressCard(card) {
    card.querySelector('[data-edit-address]')?.addEventListener('click', () => fillAddrForm(card));
    card.querySelector('[data-delete-address]')?.addEventListener('click', () => deleteAddress(card));
  }

  async function deleteAddress(card) {
    const id = card.dataset.addressId;
    if (!id || !confirm('این آدرس حذف شود؟')) return;
    const fd = new FormData();
    fd.append('id', id);
    fd.append('ajax', 'true');
    const token = addrForm?.querySelector('input[name="__RequestVerificationToken"]')?.value;
    if (token) fd.append('__RequestVerificationToken', token);
    const res = await fetch('/Account/DeleteAddress', { method: 'POST', body: fd });
    const data = await res.json().catch(() => ({}));
    if (!res.ok) { showAddrMsg(data.error || 'خطا در حذف', false); return; }
    card.remove();
    document.querySelector('[data-addr-empty]')?.removeAttribute('hidden');
    showAddrMsg('✓ آدرس حذف شد', true);
    if (addrForm?.querySelector('[data-addr-id]')?.value === id) resetAddrForm();
  }

  document.querySelectorAll('[data-address-id]').forEach(bindAddressCard);

  addrForm?.addEventListener('submit', async e => {
    e.preventDefault();
    showAddrMsg('', true);
    const btn = addrForm.querySelector('[data-addr-save]');
    btn?.classList.add('is-loading');
    const fd = new FormData(addrForm);
    fd.append('ajax', 'true');
    if (!fd.get('isDefault')) fd.append('isDefault', 'false');
    try {
      const res = await fetch('/Account/SaveAddress', { method: 'POST', body: fd });
      const data = await res.json().catch(() => ({}));
      if (!res.ok) { showAddrMsg(data.error || 'خطا در ذخیره آدرس', false); return; }
      showAddrMsg('✓ آدرس ذخیره شد', true);
      document.querySelector('[data-addr-empty]')?.setAttribute('hidden', '');
      const list = document.querySelector('[data-address-list]');
      const existing = list?.querySelector(`[data-address-id="${data.address.id}"]`);
      const card = buildAddressCard(data.address);
      if (existing) existing.replaceWith(card);
      else list?.appendChild(card);
      if (data.address.isDefault) {
        list?.querySelectorAll('[data-address-id]').forEach(c => {
          if (c.dataset.addressId !== String(data.address.id)) {
            c.dataset.default = 'false';
            c.querySelector('.ms-address-badge')?.remove();
          }
        });
      }
      resetAddrForm();
      if (hasGsap && !reduced) gsap.from(card, { opacity: 0, y: 12, duration: 0.4, ease: 'power2.out' });
    } finally {
      btn?.classList.remove('is-loading');
    }
  });

  addrReset?.addEventListener('click', resetAddrForm);

  document.querySelector('[data-locate-me]')?.addEventListener('click', () => {
    initMap();
    if (!navigator.geolocation) { showAddrMsg('مرورگر از موقعیت پشتیبانی نمی‌کند.', false); return; }
    const btn = document.querySelector('[data-locate-me]');
    btn?.classList.add('is-loading');
    setMapLoading(true, 'در حال دریافت موقعیت شما…');
    navigator.geolocation.getCurrentPosition(
      pos => {
        btn?.classList.remove('is-loading');
        setLatLng(pos.coords.latitude, pos.coords.longitude);
      },
      err => {
        btn?.classList.remove('is-loading');
        setMapLoading(false);
        showAddrMsg(geoErrorMessages[err.code] || 'خطا در دریافت موقعیت.', false);
      },
      { enableHighAccuracy: true, timeout: 15000, maximumAge: 0 }
    );
  });

  document.querySelector('[data-clear-location]')?.addEventListener('click', () => setLatLng(null, null, { geocode: false }));
  document.querySelector('[data-geocode-dismiss]')?.addEventListener('click', hideGeocodePreview);

  if (tabsRoot) {
    const origActivate = tabsRoot.querySelector('[data-tab="addresses"]');
    origActivate?.addEventListener('click', () => {
      resetMapLoading();
      setTimeout(() => {
        initMap();
        map?.invalidateSize();
        syncMapFromForm();
      }, 350);
    });
  }
})();
