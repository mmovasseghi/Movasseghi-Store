(function () {
  'use strict';

  function flushProductDescription() {
    window.stFlushAllRichTextEditors?.();
  }

  window.stFlushProductDesc = flushProductDescription;

  const editForm = document.getElementById('product-edit-form');
  editForm?.querySelectorAll('button[type="submit"], input[type="submit"]').forEach((btn) => {
    btn.addEventListener('click', () => flushProductDescription(), true);
  });
  editForm?.addEventListener('submit', (e) => {
    flushProductDescription();
    const name = editForm.querySelector('[name="Name"]');
    if (!name?.value?.trim()) {
      e.preventDefault();
      const tabBtn = document.querySelector('[data-pe-tab="content"]');
      tabBtn?.click();
      name?.focus();
      alert('نام محصول الزامی است.');
    }
  }, true);

  const tabs = document.querySelectorAll('[data-pe-tab]');
  const panels = document.querySelectorAll('[data-pe-panel]');

  function activate(tabId) {
    tabs.forEach(t => t.classList.toggle('is-active', t.dataset.peTab === tabId));
    panels.forEach(p => { p.hidden = p.dataset.pePanel !== tabId; });
    try { localStorage.setItem('st-pe-tab', tabId); } catch (_) { /* ignore */ }
  }

  if (tabs.length) {
    tabs.forEach(btn => btn.addEventListener('click', () => activate(btn.dataset.peTab)));
    const urlTab = new URLSearchParams(window.location.search).get('tab');
    const saved = (() => { try { return localStorage.getItem('st-pe-tab'); } catch { return null; } })();
    const initial = urlTab || saved;
    if (initial && document.querySelector(`[data-pe-panel="${initial}"]`)) activate(initial);
  }

  const antiforgery = () =>
    document.getElementById('product-edit-form')?.querySelector('input[name="__RequestVerificationToken"]')?.value
    || document.querySelector('input[name="__RequestVerificationToken"]')?.value
    || '';

  async function readJsonResponse(resp) {
    const text = await resp.text();
    if (!text) return { data: null, text };
    try {
      return { data: JSON.parse(text), text };
    } catch {
      return { data: null, text };
    }
  }

  function normalizeImageUrl(url) {
    if (!url) return '';
    if (url.startsWith('http') || url.startsWith('blob:')) return url;
    return url.startsWith('/') ? url : `/${url}`;
  }

  function setSlotPreview(slot, slotKey, src) {
    const img = slot?.querySelector(`[data-gallery-slot-preview="${slotKey}"]`);
    if (!img || !src) return;
    img.src = normalizeImageUrl(src);
    img.hidden = false;
    slot?.classList.add('has-image');
    const empty = slot?.querySelector(`[data-gallery-empty="${slotKey}"]`);
    if (empty) empty.hidden = true;
    const pickLabel = slot?.querySelector(`[data-gallery-pick-label="${slotKey}"]`);
    if (pickLabel) {
      pickLabel.textContent = slotKey === 'scene' ? 'تعویض تصویر Scene' : 'تعویض تصویر Studio';
    }
  }

  function ensureAltCard(slotKey, productId, data) {
    const stack = document.querySelector('[data-pe-alt-stack]');
    if (!stack || !data?.imageId) return null;

    stack.hidden = false;
    let block = document.querySelector(`[data-alt-block][data-alt-slot="${slotKey}"]`);
    if (block) return block;

    const label = slotKey === 'scene' ? '🌅 Scene' : '📦 Studio';
    const url = normalizeImageUrl(data.url);
    const alt = (data.altText || '').trim();
    block = document.createElement('article');
    block.className = 'pe-alt-card';
    block.dataset.altBlock = '';
    block.dataset.altSlot = slotKey;
    block.innerHTML = `
      <div class="pe-alt-card__media">
        <img src="${url}" alt="${alt || label}" data-gallery-preview="${slotKey}" width="160" height="160" />
        <span class="pe-alt-badge pe-alt-badge--empty" data-alt-status data-alt-status-value="empty">خالی — باید پر شود</span>
      </div>
      <div class="pe-alt-card__body">
        <header class="pe-alt-card__head">
          <strong>${label}</strong>
          <span class="pe-alt-card__hint">متن جایگزین تصویر — در سایت و گوگل نمایش داده می‌شود</span>
        </header>
        <div class="pe-alt-preview" aria-live="polite">
          <span class="pe-alt-preview__label">پیش‌نمایش — همین متن در سایت و نتایج گوگل دیده می‌شود</span>
          <p class="pe-alt-preview__text" data-alt-preview-text>— هنوز Alt تنظیم نشده —</p>
        </div>
        <label class="pe-alt-label">ویرایش Alt</label>
        <textarea class="st-input pe-alt-input" rows="3" maxlength="125" data-alt-input
          data-image-id="${data.imageId}" data-product-id="${productId}"
          placeholder="مثلاً: ظرف گیاهی آملون روی پس‌زمینه سفید"></textarea>
        <div class="pe-alt-toolbar">
          <span class="pe-alt-counter" data-alt-counter>0 / 125 (حداقل 25)</span>
          <button type="button" class="st-btn st-btn-ghost st-btn-sm" data-alt-save>ذخیره Alt</button>
        </div>
        <p class="pe-alt-save-hint" data-alt-save-hint hidden></p>
      </div>`;
    stack.appendChild(block);
    wireAltInput(block.querySelector('[data-alt-input]'));
    return block;
  }

  document.querySelectorAll('[data-gallery-file]').forEach(input => {
    input.addEventListener('change', async () => {
      const file = input.files?.[0];
      if (!file) return;

      const role = input.dataset.galleryRole;
      const productId = input.dataset.productId;
      const slotKey = role === 'Scene' ? 'scene' : 'studio';
      const slot = input.closest('[data-gallery-slot]');
      const status = slot?.querySelector(`[data-gallery-status="${slotKey}"]`);
      const blobUrl = URL.createObjectURL(file);
      setSlotPreview(slot, slotKey, blobUrl);

      if (status) {
        status.hidden = false;
        status.textContent = 'در حال آپلود…';
        status.classList.remove('is-error', 'is-ok');
        status.classList.add('is-busy');
      }
      slot?.classList.add('is-uploading');
      input.disabled = true;

      const body = new FormData();
      body.append('file', file);
      body.append('role', role);
      body.append('__RequestVerificationToken', antiforgery());

      try {
        const resp = await fetch(`/Admin/Products/UploadGallery/${productId}`, {
          method: 'POST',
          body,
          credentials: 'same-origin',
          headers: { Accept: 'application/json' }
        });
        const { data } = await readJsonResponse(resp);
        if (!resp.ok || !data?.ok) {
          throw new Error(data?.message || 'آپلود ناموفق بود.');
        }

        setSlotPreview(slot, slotKey, data.url);
        URL.revokeObjectURL(blobUrl);

        const altBlock = ensureAltCard(slotKey, productId, data)
          || document.querySelector(`[data-alt-block][data-alt-slot="${slotKey}"]`);
        const altInput = altBlock?.querySelector('[data-alt-input]');
        if (altInput) {
          if (data.imageId) altInput.dataset.imageId = String(data.imageId);
          if (data.altText) {
            altInput.value = data.altText;
            syncAltUi(altBlock, data.altText);
          }
        }
        if (data.url && altBlock) {
          const cardImg = altBlock.querySelector('[data-gallery-preview]');
          if (cardImg) cardImg.src = normalizeImageUrl(data.url);
        }
        if (status) {
          status.textContent = data.message || '✓ تصویر ذخیره شد — نیازی به «ذخیره تغییرات» نیست.';
          status.classList.remove('is-busy', 'is-error');
          status.classList.add('is-ok');
        }
        window.AdminKit?.toast?.(data.message || 'تصویر ذخیره شد.', { kind: 'success' });
        slot?.classList.remove('is-uploading');
        input.disabled = false;
        input.value = '';
      } catch (err) {
        if (status) {
          status.textContent = err.message || 'خطا در آپلود. دوباره تلاش کنید.';
          status.classList.remove('is-busy');
          status.classList.add('is-error');
        }
        slot?.classList.remove('is-uploading');
        input.disabled = false;
        window.AdminKit?.toast?.(err.message || 'خطا در آپلود', { kind: 'error' });
      }
    });
  });

  const minAlt = 25;
  const maxAlt = 125;

  function altStatus(len) {
    if (len === 0) return { key: 'empty', label: 'خالی — باید پر شود' };
    if (len < minAlt) return { key: 'short', label: 'کوتاه — حداقل ۲۵ کاراکتر' };
    if (len > maxAlt) return { key: 'long', label: 'بلند — حداکثر ۱۲۵' };
    return { key: 'ok', label: 'مناسب SEO' };
  }

  function syncAltUi(block, text) {
    if (!block) return;
    const t = (text || '').trim();
    const len = t.length;
    const st = altStatus(len);
    const preview = block.querySelector('[data-alt-preview-text]');
    const badge = block.querySelector('[data-alt-status]');
    const img = block.querySelector('[data-gallery-preview]');
    const counter = block.querySelector('[data-alt-counter]');
    const slot = block.dataset.altSlot;

    if (preview) {
      preview.textContent = t || '— هنوز Alt تنظیم نشده —';
      preview.classList.toggle('is-empty', !t);
    }
    if (badge) {
      badge.textContent = st.label;
      badge.className = `pe-alt-badge pe-alt-badge--${st.key}`;
      badge.dataset.altStatusValue = st.key;
    }
    if (img) img.alt = t || block.dataset.altSlot || 'تصویر';
    if (counter) {
      counter.textContent = `${len} / ${maxAlt} (حداقل ${minAlt})`;
      counter.classList.toggle('is-warn', len > 0 && (len < minAlt || len > maxAlt));
    }
    if (slot) {
      const side = document.querySelector(`[data-alt-sidebar-text="${slot}"]`);
      if (side) side.textContent = t || '— خالی —';
    }
  }

  async function saveAltInput(input) {
    const productId = input.dataset.productId;
    const imageId = input.dataset.imageId;
    const block = input.closest('[data-alt-block]');
    const hint = block?.querySelector('[data-alt-save-hint]');
    if (!productId || !imageId) return;

    const body = new FormData();
    body.append('imageId', imageId);
    body.append('altText', input.value.trim());
    body.append('__RequestVerificationToken', antiforgery());

    input.classList.add('is-saving');
    if (hint) { hint.hidden = true; hint.classList.remove('is-ok', 'is-error'); }
    try {
      const resp = await fetch(`/Admin/Products/UpdateImageAlt/${productId}`, {
        method: 'POST',
        body,
        credentials: 'same-origin',
        headers: { Accept: 'application/json' }
      });
      const { data } = await readJsonResponse(resp);
      if (!resp.ok || !data?.ok) {
        const msg = data?.message
          || (resp.status === 404 ? 'تصویر پیدا نشد. صفحه را یک‌بار رفرش کنید.' : null)
          || (resp.status === 400 ? 'خطای اعتبارسنجی — صفحه را رفرش کنید.' : null)
          || 'ذخیره Alt ناموفق بود.';
        throw new Error(msg);
      }
      input.classList.remove('is-invalid');
      input.classList.add('is-saved');
      syncAltUi(block, data.altText || input.value);
      if (hint) {
        hint.hidden = false;
        hint.textContent = '✓ Alt ذخیره شد';
        hint.classList.add('is-ok');
      }
      setTimeout(() => input.classList.remove('is-saved'), 1200);
    } catch (err) {
      input.classList.add('is-invalid');
      if (hint) {
        hint.hidden = false;
        hint.textContent = err.message || 'خطا در ذخیره';
        hint.classList.add('is-error');
      }
      if (window.AdminKit?.toast) window.AdminKit.toast(err.message, { kind: 'error' });
    } finally {
      input.classList.remove('is-saving');
    }
  }

  function wireAltInput(input) {
    if (!input || input.dataset.altWired === '1') return;
    input.dataset.altWired = '1';
    const block = input.closest('[data-alt-block]');
    const onInput = () => syncAltUi(block, input.value);
    input.addEventListener('input', onInput);
    input.addEventListener('blur', () => saveAltInput(input));
    block?.querySelector('[data-alt-save]')?.addEventListener('click', () => saveAltInput(input));
    onInput();
  }

  document.querySelectorAll('[data-alt-input]').forEach(wireAltInput);

  document.querySelectorAll('[data-pe-tab-jump]').forEach(btn => {
    btn.addEventListener('click', () => {
      const tab = btn.dataset.peTabJump;
      if (tab) activate(tab);
    });
  });

  document.querySelectorAll('[data-generate-alts]').forEach(btn => {
    btn.addEventListener('click', async () => {
      const productId = btn.dataset.productId;
      if (!productId) return;
      btn.disabled = true;
      const body = new FormData();
      body.append('__RequestVerificationToken', antiforgery());
      try {
        const resp = await fetch(`/Admin/Products/GenerateImageAlts/${productId}`, {
          method: 'POST',
          body,
          credentials: 'same-origin'
        });
        const data = await resp.json().catch(() => null);
        if (!resp.ok || !data?.ok) throw new Error(data?.message || 'تولید Alt ناموفق بود.');
        (data.alts || []).forEach(row => {
          const input = document.querySelector(`[data-alt-input][data-image-id="${row.id}"]`);
          if (input) {
            input.value = row.altText || '';
            syncAltUi(input.closest('[data-alt-block]'), input.value);
          }
        });
        if (window.AdminKit?.toast) window.AdminKit.toast('متن Alt تصاویر تولید شد.', { kind: 'success' });
      } catch (err) {
        if (window.AdminKit?.toast) window.AdminKit.toast(err.message, { kind: 'error' });
      } finally {
        btn.disabled = false;
      }
    });
  });

  document.querySelectorAll('[data-pe-count]').forEach(field => {
    const max = parseInt(field.dataset.peCountMax || '0', 10);
    const counter = field.parentElement?.querySelector('[data-pe-counter]');
    const update = () => {
      const len = (field.value || '').length;
      if (!counter) return;
      counter.textContent = max > 0 ? `${len} / ${max}` : `${len} کاراکتر`;
      counter.classList.toggle('is-warn', max > 0 && len > max);
    };
    field.addEventListener('input', update);
    update();
  });

  const specsRoot = document.querySelector('[data-pe-specs-preview]');
  if (specsRoot) {
    const fields = specsRoot.closest('form')?.querySelectorAll('[data-spec-field]');
    const render = () => {
      const get = name => document.querySelector(`[name="${name}"]`)?.value?.trim() || '';
      const mw = document.querySelector('[name="MicrowaveSafe"]')?.checked;
      let html = '<section class="product-specs"><h2>مشخصات فنی</h2><ul>';
      const code = get('ProductCode');
      if (code) html += `<li><strong>کد محصول آملون:</strong> ${code}</li>`;
      const dim = get('Dimensions');
      if (dim) html += `<li><strong>ابعاد:</strong> ${dim}</li>`;
      const mat = get('Material');
      if (mat) html += `<li><strong>جنس:</strong> ${mat}</li>`;
      const cap = get('CapacityCc');
      if (cap) html += `<li><strong>ظرفیت:</strong> ${cap} سی‌سی</li>`;
      const comp = get('CompartmentCount');
      if (comp) html += `<li><strong>تعداد خانه:</strong> ${comp}</li>`;
      if (mw) html += '<li><strong>مایکروویو:</strong> قابل استفاده</li>';
      const app = get('Applications');
      if (app) html += `<li><strong>کاربرد:</strong> ${app}</li>`;
      html += '</ul></section>';
      specsRoot.innerHTML = html;
    };
    fields?.forEach(f => f.addEventListener('input', render));
    document.querySelector('[name="MicrowaveSafe"]')?.addEventListener('change', render);
    render();
  }

  const packEditor = document.querySelector('[data-pack-editor]');
  if (packEditor) {
    packEditor.querySelectorAll('[data-pack-enable]').forEach(toggle => {
      toggle.addEventListener('change', () => {
        const key = toggle.dataset.packEnable;
        const body = packEditor.querySelector(`[data-pack-body="${key}"]`);
        if (body) body.hidden = !toggle.checked;
      });
    });

    const nylonPrice = packEditor.querySelector('[data-bulk-nylon-price]');
    const nylonUnits = packEditor.querySelector('[data-bulk-nylon-units]');
    const cartonNylons = packEditor.querySelector('[data-bulk-carton-nylons]');
    const cartonUnits = packEditor.querySelector('[data-bulk-carton-units]');
    const cartonPrice = packEditor.querySelector('[data-bulk-carton-price]');

    const parsePriceVal = (el) => parseFloat(String(el?.value || '').replace(/[^\d]/g, '') || '0');

    const syncBulkCarton = () => {
      if (!nylonUnits || !cartonNylons || !cartonUnits) return;
      const u = parseInt(nylonUnits.value || '0', 10);
      const n = parseInt(cartonNylons.value || '0', 10);
      if (u > 0 && n > 0 && (!cartonUnits.value || cartonUnits.dataset.auto === '1'))
        cartonUnits.value = String(u * n);
      if (nylonPrice && cartonPrice && cartonPrice.dataset.auto !== '0') {
        const p = parsePriceVal(nylonPrice);
        if (p > 0 && n > 0) cartonPrice.value = String(Math.round(p * n));
      }
    };

    [nylonPrice, nylonUnits, cartonNylons].forEach(el => el?.addEventListener('input', syncBulkCarton));
    cartonUnits?.addEventListener('input', () => { if (cartonUnits) cartonUnits.dataset.auto = '0'; });
    cartonPrice?.addEventListener('input', () => { if (cartonPrice) cartonPrice.dataset.auto = '0'; });
    if (cartonUnits && !cartonUnits.value) cartonUnits.dataset.auto = '1';
    if (cartonPrice) cartonPrice.dataset.auto = cartonPrice.value ? '0' : '1';

    const packPrice = packEditor.querySelector('[data-shrink-pack-price]');
    const packUnits = packEditor.querySelector('[data-shrink-pack-units]');
    const scPacks = packEditor.querySelector('[data-shrink-carton-packs]');
    const scUnits = packEditor.querySelector('[data-shrink-carton-units]');
    const scPrice = packEditor.querySelector('[data-shrink-carton-price]');

    const syncShrinkCarton = () => {
      if (!packUnits || !scPacks || !scUnits) return;
      const u = parseInt(packUnits.value || '0', 10);
      const n = parseInt(scPacks.value || '0', 10);
      if (u > 0 && n > 0 && (!scUnits.value || scUnits.dataset.auto === '1'))
        scUnits.value = String(u * n);
      if (packPrice && scPrice && scPrice.dataset.auto !== '0') {
        const p = parsePriceVal(packPrice);
        if (p > 0 && n > 0) scPrice.value = String(Math.round(p * n));
      }
    };

    [packPrice, packUnits, scPacks].forEach(el => el?.addEventListener('input', syncShrinkCarton));
    scUnits?.addEventListener('input', () => { if (scUnits) scUnits.dataset.auto = '0'; });
    scPrice?.addEventListener('input', () => { if (scPrice) scPrice.dataset.auto = '0'; });
  }
})();
