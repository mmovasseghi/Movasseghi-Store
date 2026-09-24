/* ═══════════════════════════════════════════════════════════
   Admin Kit — رفتار مشترک پنل مدیریت
   ───────────────────────────────────────────────────────────
   Toast · مودال تأیید (جای confirm بومی) · مرتب‌سازی جدول
   انتخاب گروهی · حالت بارگذاری دکمه · هشدار تغییرات ذخیره‌نشده
   شمارنده کاراکتر · میان‌بر کیبورد
   ═══════════════════════════════════════════════════════════ */
(function () {
  'use strict';

  const reduceMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;

  /* ───────────── Toast ───────────── */
  let toastHost = null;

  function ensureToastHost() {
    if (toastHost && document.body.contains(toastHost)) return toastHost;
    toastHost = document.createElement('div');
    toastHost.className = 'kit-toasts';
    toastHost.setAttribute('role', 'status');
    toastHost.setAttribute('aria-live', 'polite');
    document.body.appendChild(toastHost);
    return toastHost;
  }

  const TOAST_ICONS = { success: '✓', error: '!', warn: '⚠', info: 'i' };

  function toast(message, options) {
    if (!message) return;
    const opts = options || {};
    const kind = opts.kind || 'info';
    const duration = opts.duration != null ? opts.duration : kind === 'error' ? 9000 : 5000;

    const el = document.createElement('div');
    el.className = 'kit-toast kit-toast--' + kind;

    const icon = document.createElement('span');
    icon.className = 'kit-toast-icon';
    icon.setAttribute('aria-hidden', 'true');
    icon.textContent = TOAST_ICONS[kind] || 'i';

    const body = document.createElement('div');
    body.className = 'kit-toast-body';
    if (opts.title) {
      const t = document.createElement('strong');
      t.className = 'kit-toast-title';
      t.textContent = opts.title;
      body.appendChild(t);
    }
    body.appendChild(document.createTextNode(message));

    const close = document.createElement('button');
    close.type = 'button';
    close.className = 'kit-toast-close';
    close.setAttribute('aria-label', 'بستن');
    close.textContent = '✕';

    el.append(icon, body, close);

    if (duration > 0 && !reduceMotion) {
      const bar = document.createElement('span');
      bar.className = 'kit-toast-bar';
      bar.style.animationDuration = duration + 'ms';
      el.appendChild(bar);
    }

    ensureToastHost().appendChild(el);

    let timer = null;
    const dismiss = () => {
      if (timer) clearTimeout(timer);
      el.classList.add('is-leaving');
      const remove = () => el.remove();
      if (reduceMotion) remove();
      else el.addEventListener('animationend', remove, { once: true });
    };

    close.addEventListener('click', dismiss);
    if (duration > 0) timer = setTimeout(dismiss, duration);

    // نگه‌داشتن موس تایمر را متوقف می‌کند تا پیام خوانده شود
    el.addEventListener('mouseenter', () => { if (timer) clearTimeout(timer); });
    el.addEventListener('mouseleave', () => { if (duration > 0) timer = setTimeout(dismiss, 1800); });

    return dismiss;
  }

  /* ───────────── مودال تأیید ─────────────
     جای confirm() بومی که ظاهر سیستمی و زشتی دارد. */
  function confirmDialog(options) {
    const opts = options || {};
    return new Promise((resolve) => {
      const host = document.createElement('div');
      host.className = 'kit-modal kit-modal--' + (opts.kind || 'danger');
      host.innerHTML =
        '<div class="kit-modal-backdrop" data-kit-cancel></div>' +
        '<div class="kit-modal-panel" role="dialog" aria-modal="true">' +
        '<div class="kit-modal-icon" aria-hidden="true"></div>' +
        '<h2 class="kit-modal-title"></h2>' +
        '<p class="kit-modal-text"></p>' +
        '<div class="kit-modal-actions">' +
        '<button type="button" class="st-btn st-btn-ghost" data-kit-cancel></button>' +
        '<button type="button" class="st-btn" data-kit-ok></button>' +
        '</div></div>';

      const iconEl = host.querySelector('.kit-modal-icon');
      const okBtn = host.querySelector('[data-kit-ok]');
      const cancelBtn = host.querySelector('.kit-modal-actions [data-kit-cancel]');

      iconEl.textContent = opts.icon || (opts.kind === 'info' ? 'ℹ' : '⚠');
      host.querySelector('.kit-modal-title').textContent = opts.title || 'مطمئن هستید؟';
      host.querySelector('.kit-modal-text').innerHTML = opts.message || 'این عمل قابل بازگشت نیست.';
      okBtn.textContent = opts.confirmLabel || 'تأیید و ادامه';
      okBtn.classList.add(opts.kind === 'info' ? 'st-btn-primary' : 'st-btn-gold');
      cancelBtn.textContent = opts.cancelLabel || 'انصراف';

      const prevFocus = document.activeElement;

      function finish(result) {
        document.removeEventListener('keydown', onKey);
        host.remove();
        if (prevFocus && prevFocus.focus) prevFocus.focus();
        resolve(result);
      }

      function onKey(e) {
        if (e.key === 'Escape') { e.preventDefault(); finish(false); }
        else if (e.key === 'Tab') {
          // فوکوس داخل مودال حبس می‌شود (الزام دسترسی‌پذیری)
          const focusables = host.querySelectorAll('button');
          const first = focusables[0];
          const last = focusables[focusables.length - 1];
          if (e.shiftKey && document.activeElement === first) { e.preventDefault(); last.focus(); }
          else if (!e.shiftKey && document.activeElement === last) { e.preventDefault(); first.focus(); }
        }
      }

      okBtn.addEventListener('click', () => finish(true));
      host.querySelectorAll('[data-kit-cancel]').forEach((n) =>
        n.addEventListener('click', () => finish(false))
      );
      document.addEventListener('keydown', onKey);

      document.body.appendChild(host);
      okBtn.focus();
    });
  }

  /* فرم‌هایی با data-kit-confirm — بدون نوشتن JS در هر ویو */
  function wireConfirmForms(root) {
    (root || document).querySelectorAll('form[data-kit-confirm]:not([data-kit-wired])').forEach((form) => {
      form.setAttribute('data-kit-wired', '1');
      form.addEventListener('submit', async (e) => {
        if (form.dataset.kitApproved === '1') return;
        e.preventDefault();
        const ok = await confirmDialog({
          title: form.dataset.kitConfirmTitle || 'مطمئن هستید؟',
          message: form.dataset.kitConfirm,
          confirmLabel: form.dataset.kitConfirmOk,
          kind: form.dataset.kitConfirmKind || 'danger'
        });
        if (!ok) return;
        form.dataset.kitApproved = '1';
        markBusy(form);
        form.submit();
      });
    });
  }

  /* ───────────── حالت بارگذاری ─────────────
     بدون این، کاربر نمی‌داند کلیک ثبت شده و دوباره کلیک می‌کند. */
  function markBusy(form) {
    const btn = form.querySelector('button[type="submit"], input[type="submit"]');
    if (btn && !btn.classList.contains('kit-busy')) {
      btn.classList.add('kit-busy');
      btn.setAttribute('aria-busy', 'true');
      btn.disabled = true;
      // اگر ناوبری انجام نشد (خطای اعتبارسنجی سمت سرور) دکمه آزاد شود
      setTimeout(() => {
        btn.classList.remove('kit-busy');
        btn.removeAttribute('aria-busy');
        btn.disabled = false;
      }, 12000);
    }
  }

  function wireBusyForms(root) {
    (root || document)
      .querySelectorAll('form:not([data-kit-nobusy]):not([data-kit-confirm]):not([data-kit-busy-wired])')
      .forEach((form) => {
        form.setAttribute('data-kit-busy-wired', '1');
        form.addEventListener('submit', () => {
          if (form.checkValidity && !form.checkValidity()) return;
          markBusy(form);
        });
      });
  }

  /* ───────────── مرتب‌سازی جدول ─────────────
     سمت کلاینت، بدون رفت‌وبرگشت سرور. */
  function cellValue(row, index) {
    const cell = row.children[index];
    if (!cell) return '';
    const explicit = cell.getAttribute('data-sort-value');
    return explicit != null ? explicit : cell.textContent.trim();
  }

  function compare(a, b) {
    // اعداد فارسی/عربی را هم به لاتین تبدیل می‌کنیم تا مقایسه عددی درست باشد
    const norm = (s) =>
      s
        .replace(/[۰-۹]/g, (d) => String('۰۱۲۳۴۵۶۷۸۹'.indexOf(d)))
        .replace(/[٠-٩]/g, (d) => String('٠١٢٣٤٥٦٧٨٩'.indexOf(d)))
        .replace(/[,\s٪%]/g, '');
    const na = parseFloat(norm(a));
    const nb = parseFloat(norm(b));
    if (!isNaN(na) && !isNaN(nb)) return na - nb;
    return a.localeCompare(b, 'fa');
  }

  function wireSortableTables(root) {
    (root || document).querySelectorAll('table.kit-table:not([data-kit-sort-wired])').forEach((table) => {
      const headers = table.querySelectorAll('thead th[data-sort]');
      if (!headers.length) return;
      table.setAttribute('data-kit-sort-wired', '1');

      headers.forEach((th) => {
        th.setAttribute('tabindex', '0');
        th.setAttribute('role', 'button');

        const run = () => {
          const tbody = table.tBodies[0];
          if (!tbody) return;
          const index = Array.prototype.indexOf.call(th.parentNode.children, th);
          const asc = th.getAttribute('aria-sort') !== 'ascending';

          headers.forEach((h) => h.removeAttribute('aria-sort'));
          th.setAttribute('aria-sort', asc ? 'ascending' : 'descending');

          const rows = Array.prototype.slice.call(tbody.rows);
          rows.sort((r1, r2) => {
            const res = compare(cellValue(r1, index), cellValue(r2, index));
            return asc ? res : -res;
          });
          rows.forEach((r) => tbody.appendChild(r));
        };

        th.addEventListener('click', run);
        th.addEventListener('keydown', (e) => {
          if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); run(); }
        });
      });
    });
  }

  /* ───────────── انتخاب گروهی ───────────── */
  function wireBulkSelect(root) {
    (root || document).querySelectorAll('[data-kit-bulk]:not([data-kit-bulk-wired])').forEach((scope) => {
      scope.setAttribute('data-kit-bulk-wired', '1');
      const master = scope.querySelector('[data-kit-bulk-all]');
      const bar = scope.querySelector('[data-kit-bulk-bar]');
      const countEl = scope.querySelector('[data-kit-bulk-count]');

      const boxes = () => Array.prototype.slice.call(scope.querySelectorAll('[data-kit-bulk-item]'));

      function sync() {
        const all = boxes();
        const picked = all.filter((b) => b.checked);
        all.forEach((b) => b.closest('tr')?.classList.toggle('is-selected', b.checked));
        if (countEl) countEl.textContent = picked.length;
        if (bar) bar.hidden = picked.length === 0;
        if (master) {
          master.checked = picked.length > 0 && picked.length === all.length;
          master.indeterminate = picked.length > 0 && picked.length < all.length;
        }
        // شناسه‌های انتخاب‌شده در فرم‌های عملیات گروهی نوشته می‌شوند
        scope.querySelectorAll('[data-kit-bulk-ids]').forEach((input) => {
          input.value = picked.map((b) => b.value).join(',');
        });
      }

      if (master) master.addEventListener('change', () => {
        boxes().forEach((b) => { b.checked = master.checked; });
        sync();
      });

      scope.addEventListener('change', (e) => {
        if (e.target.matches('[data-kit-bulk-item]')) sync();
      });

      scope.querySelectorAll('[data-kit-bulk-clear]').forEach((btn) =>
        btn.addEventListener('click', () => {
          boxes().forEach((b) => { b.checked = false; });
          sync();
        })
      );

      sync();
    });
  }

  /* ───────────── هشدار تغییرات ذخیره‌نشده ─────────────
     قبلاً بستن تب وسط ویرایش محصول، کار را بی‌هشدار از بین می‌برد. */
  function wireDirtyGuard(root) {
    (root || document).querySelectorAll('form[data-kit-dirty-guard]:not([data-kit-dirty-wired])').forEach((form) => {
      form.setAttribute('data-kit-dirty-wired', '1');
      let dirty = false;

      form.addEventListener('input', () => { dirty = true; });
      form.addEventListener('change', () => { dirty = true; });
      form.addEventListener('submit', () => { dirty = false; });

      window.addEventListener('beforeunload', (e) => {
        if (!dirty) return;
        e.preventDefault();
        e.returnValue = '';
      });

      // کلیک روی لینک خروج هم هشدار می‌دهد
      document.addEventListener('click', async (e) => {
        const link = e.target.closest('a[href]');
        if (!dirty || !link) return;
        const href = link.getAttribute('href');
        if (!href || href.startsWith('#') || link.target === '_blank') return;
        e.preventDefault();
        const ok = await confirmDialog({
          kind: 'warn',
          icon: '✎',
          title: 'تغییرات ذخیره نشده',
          message: 'تغییراتی دارید که ذخیره نشده است. اگر خارج شوید از بین می‌رود.',
          confirmLabel: 'خارج شو',
          cancelLabel: 'برگرد و ذخیره کن'
        });
        if (ok) { dirty = false; window.location.href = href; }
      });
    });
  }

  /* ───────────── شمارنده کاراکتر (Meta Title/Description) ───────────── */
  function wireCounters(root) {
    (root || document).querySelectorAll('[data-kit-counter]:not([data-kit-counter-wired])').forEach((input) => {
      input.setAttribute('data-kit-counter-wired', '1');
      const target = document.querySelector(input.getAttribute('data-kit-counter'));
      if (!target) return;
      const min = parseInt(input.getAttribute('data-kit-min') || '0', 10);
      const max = parseInt(input.getAttribute('data-kit-max') || '0', 10);

      const update = () => {
        const n = (input.value || '').length;
        target.textContent = max ? n + ' / ' + max : String(n);
        target.classList.toggle('is-ok', n >= min && (!max || n <= max));
        target.classList.toggle('is-over', max > 0 && n > max);
      };
      input.addEventListener('input', update);
      update();
    });
  }

  /* ───────────── نمایش پیام‌های سرور به‌صورت Toast ───────────── */
  function flashServerMessages() {
    document.querySelectorAll('[data-kit-flash]').forEach((node) => {
      const text = (node.textContent || '').trim();
      if (!text) return;
      toast(text, { kind: node.getAttribute('data-kit-flash') || 'info' });
      node.remove();
    });
  }

  /* ───────────── میان‌بر کیبورد ───────────── */
  function wireShortcuts() {
    document.addEventListener('keydown', (e) => {
      const inField = /^(input|textarea|select)$/i.test(e.target.tagName)
        || e.target.isContentEditable
        || e.target.closest?.('.ProseMirror, .st-rte-editor');

      // "/" فوکوس روی جستجو
      if (e.key === '/' && !inField) {
        const search = document.querySelector('.kit-search input, input[name="q"]');
        if (search) { e.preventDefault(); search.focus(); search.select(); }
        return;
      }

      // Ctrl+S ذخیره فرم اصلی
      if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 's') {
        const form = document.querySelector('form[data-kit-primary]');
        if (form) {
          e.preventDefault();
          form.requestSubmit ? form.requestSubmit() : form.submit();
        }
        return;
      }

      // Escape خروج از فیلد جستجو
      if (e.key === 'Escape' && e.target.matches('.kit-search input')) e.target.blur();
    });
  }

  /* ───────────── جدول‌های موبایل (کارت‌وار + data-label خودکار) ───────────── */
  const MOBILE_TABLE_MQ = window.matchMedia('(max-width: 960px)');

  function headerLabel(th) {
    const sr = th.querySelector('.kit-sr-only');
    if (sr) return sr.textContent.replace(/\s+/g, ' ').trim();
    const t = (th.textContent || '').replace(/\s+/g, ' ').trim();
    return t;
  }

  function labelStackTable(table) {
    const headers = [...table.querySelectorAll('thead th')].map(headerLabel);
    if (!headers.length) return;
    table.querySelectorAll('tbody tr').forEach((tr) => {
      [...tr.children].forEach((cell, i) => {
        if (cell.getAttribute('data-label')) return;
        let label = headers[i] || '';
        if (!label && cell.classList.contains('kit-cell-actions')) label = 'عملیات';
        if (!label && cell.classList.contains('kit-col-star')) label = 'ویژه · برتر';
        if (!label && (cell.querySelector('a.st-btn, button.st-btn') || cell.querySelector('form'))) label = 'عملیات';
        if (label) cell.setAttribute('data-label', label);
      });
    });
  }

  function setMobileTableMode(enabled, root) {
    const scope = root && root.querySelector ? root : document;
    scope.querySelectorAll('.kit-table-scroll').forEach((scroll) => {
      if (scroll.classList.contains('kit-table-scroll--wide')) return;
      const table = scroll.querySelector('.kit-table');
      if (!table) return;
      scroll.classList.toggle('kit-table-scroll--stack', enabled);
      table.classList.toggle('kit-table--stack', enabled);
      if (enabled) labelStackTable(table);
    });
    scope.querySelectorAll('.st-table-wrap').forEach((wrap) => {
      if (wrap.classList.contains('st-table-wrap--wide')) return;
      const table = wrap.querySelector('.st-table');
      if (!table) return;
      wrap.classList.toggle('st-table-wrap--stack', enabled);
      table.classList.toggle('st-table--stack', enabled);
      if (enabled) labelStackTable(table);
    });
  }

  function wireMobileTables(root) {
    const apply = () => setMobileTableMode(MOBILE_TABLE_MQ.matches, root || document);
    apply();
    if (!wireMobileTables._bound) {
      wireMobileTables._bound = true;
      MOBILE_TABLE_MQ.addEventListener('change', () => setMobileTableMode(MOBILE_TABLE_MQ.matches, document));
    }
  }

  /* ───────────── راه‌اندازی ───────────── */
  function init(root) {
    wireConfirmForms(root);
    wireBusyForms(root);
    wireSortableTables(root);
    wireBulkSelect(root);
    wireDirtyGuard(root);
    wireCounters(root);
    wireMobileTables(root);
  }

  window.AdminKit = {
    toast: toast,
    confirm: confirmDialog,
    init: init,
    busy: markBusy,
    refreshMobileTables: () => setMobileTableMode(MOBILE_TABLE_MQ.matches, document),
  };

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => { init(document); flashServerMessages(); wireShortcuts(); });
  } else {
    init(document);
    flashServerMessages();
    wireShortcuts();
  }
})();
