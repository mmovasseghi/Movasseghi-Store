(function initCartHumanGate() {
  const HUMAN_POST_URL = '/Cart/HumanCheck';
  const HUMAN_REFRESH_URL = '/Cart/RefreshHumanChallenge';

  function showMsg(gate, text) {
    const msg = gate.querySelector('[data-human-msg]');
    if (!msg) return;
    msg.hidden = false;
    msg.textContent = text;
  }

  function setVerified(gate, checkout) {
    gate.dataset.humanVerified = '1';
    gate.classList.add('is-verified');
    gate.innerHTML =
      '<p class="ms-human-gate__ok"><span class="ms-human-gate__ok-icon" aria-hidden="true">✓</span> تأیید شد — می‌توانید سفارش را تکمیل کنید</p>';
    if (checkout) {
      checkout.classList.remove('is-disabled');
      checkout.setAttribute('href', '/Checkout');
      checkout.removeAttribute('tabindex');
      checkout.setAttribute('aria-disabled', 'false');
    }
  }

  async function postHumanJson(fields) {
    const fd = new FormData();
    Object.entries(fields).forEach(([k, v]) => fd.append(k, v));

    const res = await fetch(HUMAN_POST_URL, {
      method: 'POST',
      body: fd,
      credentials: 'same-origin',
      headers: { Accept: 'application/json' },
    });

    if (!res.ok) {
      throw new Error(`HTTP ${res.status}`);
    }
    return res.json();
  }

  function boot() {
    const gate = document.getElementById('humanGate');
    if (!gate || gate.dataset.humanVerified === '1') return;

    const checkout = document.getElementById('checkoutContinue');
    const form = gate.querySelector('[data-human-form]');
    const canAjax = typeof window.fetch === 'function';

    checkout?.addEventListener('click', (e) => {
      if (gate.dataset.humanVerified !== '1') {
        e.preventDefault();
        showMsg(gate, 'لطفاً گزینهٔ مربوط به فروشگاه موثقی را بزنید (نه ربات).');
        gate.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
      }
    });

    gate.addEventListener('click', (e) => {
      const btn = e.target.closest('[data-human-option]');
      if (!btn || btn.disabled) return;
      gate.querySelectorAll('.ms-human-opt').forEach((b) => b.classList.remove('is-selected', 'is-busy'));
      btn.classList.add('is-selected');
    });

    form?.addEventListener('submit', async (e) => {
      const submitter = e.submitter;
      if (!submitter?.matches?.('[data-human-option]')) return;

      if (gate.dataset.fallbackSubmit === '1') {
        gate.dataset.fallbackSubmit = '0';
        return;
      }

      if (!canAjax) return;

      e.preventDefault();
      if (submitter.disabled) return;

      const optionId = submitter.value || submitter.dataset.humanOption;
      const token = form.querySelector('[data-human-token-input]')?.value || '';
      if (!optionId || !token) {
        showMsg(gate, 'خطا در بارگذاری سؤال — صفحه را یک‌بار رفرش کنید.');
        return;
      }

      submitter.classList.add('is-busy');
      gate.querySelectorAll('[data-human-option]').forEach((b) => {
        b.disabled = true;
      });

      try {
        const data = await postHumanJson({ token, optionId });
        if (data.ok) {
          setVerified(gate, checkout);
          return;
        }
        showMsg(gate, 'این گزینه مربوط به فروشگاه نبود — گزینهٔ دیگر را بزنید یا «سؤال دیگر».');
        gate.querySelectorAll('[data-human-option]').forEach((b) => {
          b.disabled = false;
          b.classList.remove('is-busy');
        });
        submitter.classList.remove('is-selected');
      } catch {
        showMsg(gate, 'در حال ثبت پاسخ…');
        gate.querySelectorAll('[data-human-option]').forEach((b) => {
          b.disabled = false;
          b.classList.remove('is-busy');
        });
        gate.dataset.fallbackSubmit = '1';
        if (typeof form.requestSubmit === 'function') {
          form.requestSubmit(submitter);
        } else {
          submitter.click();
        }
      }
    });

    const refresh = gate.querySelector('[data-human-refresh]');
    refresh?.addEventListener('click', async (e) => {
      e.preventDefault();
      if (!canAjax) {
        window.location.reload();
        return;
      }
      refresh.disabled = true;
      try {
        const fd = new FormData();
        const res = await fetch(HUMAN_REFRESH_URL, {
          method: 'POST',
          body: fd,
          credentials: 'same-origin',
          headers: { Accept: 'application/json' },
        });
        if (!res.ok) throw new Error(`HTTP ${res.status}`);
        const data = await res.json();
        const options = gate.querySelector('.ms-human-gate__options');
        const tokenInput = form?.querySelector('[data-human-token-input]');
        if (!options || !data.options || !tokenInput) return;
        gate.querySelector('.ms-human-gate__prompt')?.textContent = data.prompt;
        tokenInput.value = data.token;
        options.innerHTML = data.options
          .map(
            (o) =>
              `<button type="submit" name="optionId" value="${o.id}" class="ms-human-opt" data-human-option="${o.id}">
            <span class="ms-human-opt__emoji" aria-hidden="true">${o.emoji}</span>
            <span class="ms-human-opt__label">${o.label}</span>
            <span class="ms-human-opt__chev" aria-hidden="true">‹</span></button>`
          )
          .join('');
        const msg = gate.querySelector('[data-human-msg]');
        if (msg) msg.hidden = true;
      } catch {
        showMsg(gate, 'بارگذاری سؤال جدید ممکن نشد — لطفاً صفحه را رفرش کنید.');
      } finally {
        refresh.disabled = false;
      }
    });
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', boot);
  } else {
    boot();
  }
})();
