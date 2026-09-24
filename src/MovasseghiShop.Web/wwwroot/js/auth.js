(function () {
  'use strict';

  const hasGsap = typeof gsap !== 'undefined';
  const reduced = matchMedia('(prefers-reduced-motion: reduce)').matches;
  const touchUi = () => matchMedia('(max-width: 767px)').matches;
  const liteMotion = () => reduced || touchUi();

  const STEP_INDEX = { phone: 0, otp: 1, register: 2, 'password-login': 0, success: 3 };
  const BAR_WIDTH = [33, 66, 100, 100];

  function faDigits(s) {
    return String(s).replace(/[0-9]/g, d => '۰۱۲۳۴۵۶۷۸۹'[d]);
  }

  function toEnDigits(s) {
    return String(s)
      .replace(/[۰-۹]/g, d => '۰۱۲۳۴۵۶۷۸۹'.indexOf(d))
      .replace(/[٠-٩]/g, d => '٠١٢٣٤٥٦٧٨٩'.indexOf(d))
      .replace(/\D/g, '');
  }

  function bounce(el) {
    if (!el || reduced || !hasGsap) return;
    gsap.fromTo(el, { scale: 1.12 }, { scale: 1, duration: 0.38, ease: 'back.out(3)' });
  }

  function shake(el) {
    if (!el || reduced || !hasGsap) return;
    gsap.fromTo(el, { x: -10 }, { x: 0, duration: 0.5, ease: 'elastic.out(1, 0.45)' });
  }

  function pulseGlider(el) {
    if (!el || reduced || !hasGsap) return;
    gsap.fromTo(el, { scale: 0.92 }, { scale: 1, duration: 0.45, ease: 'back.out(2.5)' });
  }

  function initOtpBoxes(container, hiddenInput) {
    if (!container) return null;
    const boxes = [...container.querySelectorAll('.ms-otp-box')];

    function syncHidden() {
      if (hiddenInput) hiddenInput.value = boxes.map(b => b.value).join('');
    }

    boxes.forEach((box, i) => {
      box.addEventListener('input', () => {
        box.value = box.value.replace(/\D/g, '').slice(-1);
        syncHidden();
        if (box.value && i < boxes.length - 1) boxes[i + 1].focus();
        bounce(box);
      });

      box.addEventListener('keydown', e => {
        if (e.key === 'Backspace' && !box.value && i > 0) {
          boxes[i - 1].focus();
          boxes[i - 1].value = '';
          syncHidden();
        }
        if (e.key === 'ArrowLeft' && i < boxes.length - 1) boxes[i + 1].focus();
        if (e.key === 'ArrowRight' && i > 0) boxes[i - 1].focus();
      });

      box.addEventListener('paste', e => {
        e.preventDefault();
        const digits = (e.clipboardData.getData('text') || '').replace(/\D/g, '').slice(0, boxes.length);
        digits.split('').forEach((d, j) => { if (boxes[j]) boxes[j].value = d; });
        syncHidden();
        boxes[Math.min(digits.length, boxes.length - 1)]?.focus();
        boxes.forEach(bounce);
      });
    });

    return {
      clear() { boxes.forEach(b => { b.value = ''; }); syncHidden(); boxes[0]?.focus(); },
      focus() { boxes[0]?.focus(); },
      value: () => boxes.map(b => b.value).join('')
    };
  }

  function bindPhoneInput(input) {
    input?.addEventListener('input', e => {
      e.target.value = faDigits(toEnDigits(e.target.value)).slice(0, 11);
    });
  }

  function bindPasswordToggle(panel) {
    panel.querySelectorAll('[data-toggle-password]').forEach(btn => {
      btn.addEventListener('click', () => {
        const input = panel.querySelector('#' + btn.dataset.togglePassword);
        if (!input) return;
        const show = input.type === 'password';
        input.type = show ? 'text' : 'password';
        btn.textContent = show ? '🙈' : '👁';
        bounce(btn);
      });
    });
  }

  function initAuthPanel(panel) {
    if (!panel) return;
    if (panel.dataset.authReady === '1') return;
    panel.dataset.authReady = '1';

    const phoneForm = panel.querySelector('[data-auth-phone-form]');
    const otpForm = panel.querySelector('[data-auth-otp-form]');
    const regForm = panel.querySelector('[data-auth-register-form]');
    const pwdForm = panel.querySelector('[data-auth-password-form]');
    const errEl = panel.querySelector('[data-auth-error]');
    const devEl = panel.querySelector('[data-auth-dev]');
    const phoneHidden = panel.querySelector('[data-auth-phone-hidden]');
    const phoneDisplay = panel.querySelector('[data-auth-phone-display]');
    const phoneReadonly = panel.querySelector('[data-auth-phone-readonly]');
    const otpHidden = panel.querySelector('[data-otp-value]');
    const resendBtn = panel.querySelector('[data-auth-resend]');
    const resendTimer = panel.querySelector('[data-auth-resend-timer]');
    const stepbar = panel.querySelector('[data-auth-stepbar]');
    const loginModes = panel.querySelector('[data-auth-login-modes]');
    const loginOptions = panel.querySelector('[data-auth-login-options]');
    const registerNote = panel.querySelector('[data-auth-register-note]');
    const tabsGlider = panel.querySelector('[data-auth-tabs-glider]');
    const modeGlider = panel.querySelector('[data-auth-mode-glider]');
    const phoneLead = panel.querySelector('[data-auth-phone-lead]');
    const phoneBtn = panel.querySelector('[data-auth-phone-btn]');
    const otpApi = initOtpBoxes(panel.querySelector('[data-otp-inputs]'), otpHidden);

    let resendInterval = null;
    let currentPhone = '';
    let stepDir = 1;
    let mainTab = 'login';
    let loginMode = 'sms';

    function showError(msg) {
      if (!errEl) return;
      errEl.textContent = msg;
      errEl.hidden = !msg;
      if (msg) shake(errEl);
    }

    function setLoading(form, on) {
      form?.querySelector('[data-auth-submit]')?.classList.toggle('is-loading', on);
    }

    function setPhone(phone) {
      currentPhone = phone;
      const display = faDigits(phone);
      if (phoneHidden) phoneHidden.value = phone;
      if (phoneDisplay) phoneDisplay.textContent = display;
      if (phoneReadonly) phoneReadonly.value = display;
    }

    function updateChrome(stepName, idx) {
      const title = panel.querySelector('[data-auth-title]');
      const sub = panel.querySelector('[data-auth-sub]');
      const bar = panel.querySelector('[data-auth-bar]');

      const isPwdLogin = mainTab === 'login' && loginMode === 'password';
      const showBar = !isPwdLogin && stepName !== 'success';

      if (stepbar) stepbar.hidden = !showBar;

      panel.querySelectorAll('[data-auth-pill]').forEach(p => {
        const pillIdx = Number(p.dataset.authPill);
        p.classList.toggle('is-active', showBar && pillIdx <= Math.min(idx, 2));
        p.classList.toggle('is-current', showBar && pillIdx === Math.min(idx, 2));
      });

      if (title) {
        title.textContent = mainTab === 'register'
          ? (stepName === 'register' ? 'تکمیل ثبت‌نام' : 'ثبت‌نام')
          : (stepName === 'success' ? 'ورود موفق' : 'ورود');
      }

      const subs = {
        phone: mainTab === 'register' ? 'شماره موبایل برای ثبت‌نام' : 'با شماره موبایل وارد شوید',
        otp: 'کد تأیید را وارد کنید',
        register: 'اطلاعات خود را تکمیل کنید',
        'password-login': 'ورود با رمز عبور',
        success: 'ورود موفق'
      };
      if (sub) sub.textContent = subs[stepName] || subs.phone;

      if (bar && showBar) {
        const w = BAR_WIDTH[idx] || 33;
        if (hasGsap && !reduced) gsap.to(bar, { width: w + '%', duration: 0.55, ease: 'power3.inOut' });
        else bar.style.width = w + '%';
      }
    }

    function goStep(stepName, dir = 1) {
      const stage = panel.querySelector('[data-auth-stage]');
      if (!stage) return;

      const idx = STEP_INDEX[stepName] ?? 0;
      const current = stage.querySelector('.ms-auth-step.is-active');
      const next = stage.querySelector(`[data-auth-step="${stepName}"]`);
      if (!next || current === next) {
        updateChrome(stepName, idx);
        return;
      }

      updateChrome(stepName, idx);

      const swap = () => {
        current?.classList.remove('is-active');
        current?.setAttribute('hidden', '');
        next.classList.add('is-active');
        next.removeAttribute('hidden');
      };

      if (hasGsap && !liteMotion() && current) {
        gsap.timeline()
          .to(current, { opacity: 0, y: dir * -12, duration: 0.2, ease: 'power2.in' })
          .call(swap)
          .fromTo(next, { opacity: 0, y: dir * 10 }, { opacity: 1, y: 0, duration: 0.28, ease: 'power2.out', clearProps: 'opacity,transform' });
      } else {
        swap();
        if (next) {
          next.style.opacity = '1';
          next.style.transform = 'none';
          next.style.filter = 'none';
        }
      }
    }

    function dismissKeyboard() {
      const active = document.activeElement;
      if (active && panel.contains(active) && typeof active.blur === 'function') active.blur();
    }

    function moveGlider(container, glider, activeBtn) {
      if (!container || !glider || !activeBtn) return;
      glider.style.width = activeBtn.offsetWidth + 'px';
      glider.style.transform = `translate3d(${activeBtn.offsetLeft}px, 0, 0)`;
      pulseGlider(glider);
    }

    function syncTabsUI() {
      panel.querySelectorAll('[data-auth-tab]').forEach(btn => {
        const on = btn.dataset.authTab === mainTab;
        btn.classList.toggle('is-active', on);
        btn.setAttribute('aria-selected', on ? 'true' : 'false');
      });
      const activeTab = panel.querySelector(`[data-auth-tab="${mainTab}"]`);
      moveGlider(panel.querySelector('[data-auth-tabs]'), tabsGlider, activeTab);

      const isLogin = mainTab === 'login';
      if (loginOptions) loginOptions.hidden = !isLogin;
      if (registerNote) registerNote.hidden = isLogin;

      panel.querySelectorAll('[data-auth-login-mode]').forEach(btn => {
        btn.setAttribute('aria-pressed', btn.dataset.authLoginMode === loginMode ? 'true' : 'false');
      });

      if (phoneLead) {
        phoneLead.textContent = mainTab === 'register'
          ? 'شماره موبایل خود را وارد کنید. پس از تأیید کد، فرم ثبت‌نام باز می‌شود.'
          : 'شماره موبایل خود را وارد کنید تا کد تأیید ارسال شود.';
      }
      if (phoneBtn) phoneBtn.textContent = mainTab === 'register' ? 'دریافت کد ثبت‌نام' : 'دریافت کد تأیید';
    }

    function syncLoginModeUI() {
      panel.querySelectorAll('[data-auth-login-mode]').forEach(btn => {
        const on = btn.dataset.authLoginMode === loginMode;
        btn.classList.toggle('is-active', on);
        btn.setAttribute('aria-pressed', on ? 'true' : 'false');
      });
      const activeMode = panel.querySelector(`[data-auth-login-mode="${loginMode}"]`);
      moveGlider(loginModes, modeGlider, activeMode);
    }

    function resetToEntry() {
      showError('');
      if (devEl) { devEl.hidden = true; devEl.textContent = ''; }
      clearInterval(resendInterval);
      dismissKeyboard();
      if (mainTab === 'login' && loginMode === 'password') goStep('password-login', -1);
      else goStep('phone', -1);
    }

    function setMainTab(tab) {
      const changed = mainTab !== tab;
      mainTab = tab;
      dismissKeyboard();
      syncTabsUI();
      if (changed) resetToEntry();
      requestAnimationFrame(() => syncTabsUI());
    }

    function setLoginMode(mode) {
      loginMode = mode;
      dismissKeyboard();
      syncLoginModeUI();
      showError('');
      if (mode === 'password') goStep('password-login', 1);
      else goStep('phone', -1);
      requestAnimationFrame(() => syncLoginModeUI());
    }

    function startResendCooldown(sec = 60) {
      if (!resendBtn || !resendTimer) return;
      let left = sec;
      resendBtn.disabled = true;
      resendTimer.textContent = String(left);
      clearInterval(resendInterval);
      resendInterval = setInterval(() => {
        left -= 1;
        resendTimer.textContent = String(Math.max(left, 0));
        if (left <= 0) {
          clearInterval(resendInterval);
          resendBtn.disabled = false;
          resendBtn.innerHTML = 'ارسال مجدد کد';
        }
      }, 1000);
    }

    async function sendOtp(phone) {
      const fd = new FormData();
      fd.append('phone', phone);
      fd.append('ajax', 'true');
      const token = phoneForm?.querySelector('input[name="__RequestVerificationToken"]')?.value;
      if (token) fd.append('__RequestVerificationToken', token);
      const res = await fetch('/Account/SendLoginOtp', { method: 'POST', body: fd });
      const data = await res.json().catch(() => ({}));
      if (!res.ok) throw new Error(data.error || 'خطا در ارسال کد');
      return data;
    }

    function redirectSoon(url) {
      stepDir = 1;
      goStep('success', 1);
      if (hasGsap && !reduced) {
        gsap.from(panel.querySelector('.ms-auth-success-icon'), { scale: 0, rotation: -180, duration: 0.7, ease: 'back.out(2)' });
      }
      setTimeout(() => { window.location.href = url; }, 1100);
    }

    bindPhoneInput(panel.querySelector('#authPhone'));
    bindPhoneInput(panel.querySelector('#authLoginPhone'));
    bindPasswordToggle(panel);

    panel.addEventListener('mousedown', e => {
      if (e.target.closest('[data-auth-tab], [data-auth-login-mode]')) e.preventDefault();
    });

    panel.addEventListener('click', e => {
      const tabBtn = e.target.closest('[data-auth-tab]');
      if (tabBtn && panel.contains(tabBtn)) {
        e.preventDefault();
        setMainTab(tabBtn.dataset.authTab || 'login');
        return;
      }
      const modeBtn = e.target.closest('[data-auth-login-mode]');
      if (modeBtn && panel.contains(modeBtn)) {
        e.preventDefault();
        setLoginMode(modeBtn.dataset.authLoginMode || 'sms');
      }
    });

    panel.querySelector('[data-auth-switch-sms]')?.addEventListener('click', () => setLoginMode('sms'));

    phoneForm?.addEventListener('submit', async e => {
      e.preventDefault();
      showError('');
      setLoading(phoneForm, true);
      const raw = toEnDigits(phoneForm.querySelector('[name=phone]')?.value || '');
      const fd = new FormData(phoneForm);
      fd.set('phone', raw);
      fd.append('ajax', 'true');
      try {
        const res = await fetch('/Account/SendLoginOtp', { method: 'POST', body: fd });
        const data = await res.json().catch(() => ({}));
        if (!res.ok) { showError(data.error || 'خطا در ارسال کد'); return; }
        setPhone(data.phone || raw);
        if (devEl && data.devOtp) {
          devEl.textContent = 'کد تست (موقت): ' + faDigits(data.devOtp);
          devEl.hidden = false;
        }
        stepDir = 1;
        goStep('otp', 1);
        otpApi?.clear();
        startResendCooldown();
        setTimeout(() => otpApi?.focus(), 500);
      } finally {
        setLoading(phoneForm, false);
      }
    });

    panel.querySelector('[data-auth-back]')?.addEventListener('click', () => {
      showError('');
      stepDir = -1;
      goStep('phone', -1);
    });

    resendBtn?.addEventListener('click', async () => {
      if (!currentPhone || resendBtn.disabled) return;
      showError('');
      try {
        const data = await sendOtp(currentPhone);
        if (devEl && data.devOtp) {
          devEl.textContent = 'کد تست (موقت): ' + faDigits(data.devOtp);
          devEl.hidden = false;
        }
        otpApi?.clear();
        startResendCooldown();
        bounce(resendBtn);
      } catch (err) {
        showError(err.message || 'خطا در ارسال مجدد');
      }
    });

    otpForm?.addEventListener('submit', async e => {
      e.preventDefault();
      showError('');
      if (otpHidden) otpHidden.value = otpApi?.value() || '';
      if ((otpHidden?.value || '').length < 6) {
        showError('کد ۶ رقمی را کامل وارد کنید.');
        panel.querySelectorAll('.ms-otp-box').forEach(b => bounce(b));
        return;
      }
      setLoading(otpForm, true);
      const fd = new FormData(otpForm);
      fd.append('ajax', 'true');
      const verifyUrl = mainTab === 'register' ? '/Account/VerifyRegisterOtp' : '/Account/VerifyOtp';
      try {
        const res = await fetch(verifyUrl, { method: 'POST', body: fd });
        const data = await res.json().catch(() => ({}));
        if (!res.ok) { showError(data.error || 'کد نامعتبر'); return; }

        if (mainTab === 'register' || data.needsRegistration) {
          if (mainTab === 'login' && data.needsRegistration) {
            mainTab = 'register';
            syncTabsUI();
          }
          setPhone(data.phone || currentPhone);
          stepDir = 1;
          goStep('register', 1);
          panel.querySelector('#authFirstName')?.focus();
          return;
        }
        if (data.redirect) redirectSoon((window.appUrl || (u => u))(data.redirect));
      } finally {
        setLoading(otpForm, false);
      }
    });

    pwdForm?.addEventListener('submit', async e => {
      e.preventDefault();
      showError('');
      setLoading(pwdForm, true);
      const fd = new FormData(pwdForm);
      fd.set('phone', toEnDigits(fd.get('phone') || ''));
      fd.append('ajax', 'true');
      try {
        const res = await fetch('/Account/LoginWithPassword', { method: 'POST', body: fd });
        const data = await res.json().catch(() => ({}));
        if (!res.ok) { showError(data.error || 'خطا در ورود'); return; }
        if (data.redirect) redirectSoon((window.appUrl || (u => u))(data.redirect));
      } finally {
        setLoading(pwdForm, false);
      }
    });

    regForm?.addEventListener('submit', async e => {
      e.preventDefault();
      showError('');
      const pwd = regForm.querySelector('[name=password]')?.value || '';
      const confirm = regForm.querySelector('[name=confirmPassword]')?.value || '';
      if (pwd.length < 8) {
        showError('رمز عبور باید حداقل ۸ کاراکتر باشد.');
        return;
      }
      if (pwd !== confirm) {
        showError('رمز عبور و تکرار آن یکسان نیست.');
        return;
      }
      setLoading(regForm, true);
      const fd = new FormData(regForm);
      fd.append('ajax', 'true');
      try {
        const res = await fetch('/Account/CompleteRegistration', { method: 'POST', body: fd });
        const data = await res.json().catch(() => ({}));
        if (!res.ok) { showError(data.error || 'خطا در ثبت‌نام'); return; }
        if (data.redirect) redirectSoon((window.appUrl || (u => u))(data.redirect));
      } finally {
        setLoading(regForm, false);
      }
    });

    panel.querySelectorAll('.ms-ripple').forEach(btn => {
      btn.addEventListener('click', function (e) {
        const r = document.createElement('span');
        r.className = 'ms-ripple-fx';
        const rect = this.getBoundingClientRect();
        const size = Math.max(rect.width, rect.height);
        r.style.width = r.style.height = size + 'px';
        r.style.left = (e.clientX - rect.left - size / 2) + 'px';
        r.style.top = (e.clientY - rect.top - size / 2) + 'px';
        this.appendChild(r);
        r.addEventListener('animationend', () => r.remove());
      });
    });

    requestAnimationFrame(() => {
      syncTabsUI();
      syncLoginModeUI();
      updateChrome('phone', 0);
    });

    if (hasGsap && !liteMotion()) {
      gsap.to(panel.querySelector('.ms-auth-bg-ring'), { rotation: 360, duration: 40, repeat: -1, ease: 'none' });
      gsap.to(panel.querySelector('.ms-auth-bg-spark--1'), { y: -18, x: 12, duration: 4, repeat: -1, yoyo: true, ease: 'sine.inOut' });
      gsap.to(panel.querySelector('.ms-auth-bg-spark--2'), { y: 14, x: -10, duration: 5.5, repeat: -1, yoyo: true, ease: 'sine.inOut' });
    }
  }

  window.MsAuth = { init: initAuthPanel };
})();
