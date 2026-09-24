(() => {
  const faDigits = '۰۱۲۳۴۵۶۷۸۹';
  const enDigits = '0123456789';

  const toEn = (s) => {
    let out = '';
    for (const ch of s) {
      const i = faDigits.indexOf(ch);
      out += i >= 0 ? enDigits[i] : ch;
    }
    return out;
  };

  const digitsOnly = (raw) => toEn(raw).replace(/[^\d]/g, '');

  const format = (digits) => {
    if (!digits) return '';
    return digits.replace(/\B(?=(\d{3})+(?!\d))/g, '٬');
  };

  const bind = (input) => {
    if (input.dataset.priceBound === '1') return;
    input.dataset.priceBound = '1';
    input.setAttribute('inputmode', 'numeric');
    input.setAttribute('autocomplete', 'off');
    if (input.type === 'number') input.type = 'text';

    const initial = digitsOnly(input.value || input.getAttribute('value') || '');
    if (initial) input.value = format(initial);

    input.addEventListener('input', () => {
      const d = digitsOnly(input.value);
      input.value = format(d);
      input.dataset.rawValue = d;
    });
  };

  document.querySelectorAll('[data-admin-price-input]').forEach(bind);

  document.querySelectorAll('form').forEach((form) => {
    form.addEventListener('submit', () => {
      form.querySelectorAll('[data-admin-price-input]').forEach((input) => {
        const d = input.dataset.rawValue || digitsOnly(input.value);
        input.value = d;
      });
    });
  });
})();
