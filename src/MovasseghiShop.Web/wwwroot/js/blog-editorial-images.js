(function () {
  'use strict';

  const modalId = 'blog-catalog-image-modal';
  let pickerTarget = null;
  let pickerOldUrl = null;

  function ensureModal() {
    let m = document.getElementById(modalId);
    if (m) return m;
    m = document.createElement('div');
    m.id = modalId;
    m.className = 'st-rte-modal';
    m.innerHTML = `
      <div class="st-rte-modal-box" role="dialog" aria-label="انتخاب تصویر از کاتالوگ">
        <h3>تصاویر محصولات سایت</h3>
        <input type="search" class="st-input" id="blog-img-search" placeholder="جستجو نام محصول…" />
        <div id="blog-img-grid" class="blog-img-grid"></div>
        <div class="st-rte-modal-actions">
          <button type="button" class="st-btn st-btn-ghost" data-blog-img-cancel>بستن</button>
        </div>
      </div>`;
    document.body.appendChild(m);
    m.addEventListener('click', (e) => {
      if (e.target === m) closeModal();
    });
    m.querySelector('[data-blog-img-cancel]').addEventListener('click', closeModal);
    m.querySelector('#blog-img-search').addEventListener('input', (e) => {
      loadImages(e.target.value);
    });
    return m;
  }

  function openModal(target, oldUrl) {
    pickerTarget = target;
    pickerOldUrl = oldUrl || null;
    const m = ensureModal();
    m.classList.add('is-open');
    loadImages('');
  }

  function closeModal() {
    document.getElementById(modalId)?.classList.remove('is-open');
    pickerTarget = null;
    pickerOldUrl = null;
  }

  async function loadImages(q) {
    const grid = document.getElementById('blog-img-grid');
    if (!grid) return;
    grid.innerHTML = '<p>در حال بارگذاری…</p>';
    const url = '/Admin/BlogAdmin/CatalogImages?take=60' + (q ? '&q=' + encodeURIComponent(q) : '');
    const res = await fetch(url);
    const items = await res.json();
    if (!items.length) {
      grid.innerHTML = '<p>تصویری پیدا نشد.</p>';
      return;
    }
    grid.innerHTML = '';
    items.forEach((item) => {
      const btn = document.createElement('button');
      btn.type = 'button';
      btn.className = 'blog-img-tile';
      btn.innerHTML = `<img src="${item.url}" alt="" loading="lazy" /><span>${item.product}</span>`;
      btn.addEventListener('click', () => selectImage(item.url, item.alt || item.product));
      grid.appendChild(btn);
    });
  }

  function selectImage(url, alt) {
    if (pickerTarget === 'featured') {
      const inp = document.getElementById('FeaturedImageUrl');
      if (inp) inp.value = url;
      const prev = document.getElementById('featured-image-preview');
      if (prev) {
        prev.src = url;
        prev.hidden = false;
      }
    } else if (pickerTarget === 'inline' && pickerOldUrl) {
      replaceInlineImage(pickerOldUrl, url, alt);
    }
    closeModal();
  }

  function getEditor() {
    const editors = window.__rteEditors || {};
    return editors['blog-content'] || null;
  }

  function replaceInlineImage(oldUrl, newUrl, alt) {
    const ed = getEditor();
    if (!ed || ed.isDestroyed) return;
    let html = ed.getHTML();
    const esc = oldUrl.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
    const re = new RegExp(
      `<img([^>]*)\\ssrc=["']${esc}["']([^>]*)>`,
      'i',
    );
    html = html.replace(re, (tag) => {
      let next = tag.replace(/\ssrc=["'][^"']+["']/i, ` src="${newUrl}"`);
      if (alt) {
        if (/\salt=["']/i.test(next)) next = next.replace(/\salt=["'][^"']*["']/i, ` alt="${alt.replace(/"/g, '&quot;')}"`);
        else next = next.replace(/<img/i, `<img alt="${alt.replace(/"/g, '&quot;')}"`);
      }
      return next;
    });
    ed.commands.setContent(html, false);
    const ta = document.getElementById('blog-content');
    if (ta) ta.value = html;
  }

  document.addEventListener('click', (e) => {
    const feat = e.target.closest?.('[data-pick-featured-image]');
    if (feat) {
      e.preventDefault();
      openModal('featured', null);
      return;
    }
    const inline = e.target.closest?.('[data-pick-inline-image]');
    if (inline) {
      e.preventDefault();
      const oldUrl = inline.getAttribute('data-image-url');
      openModal('inline', oldUrl);
    }
  });
})();
