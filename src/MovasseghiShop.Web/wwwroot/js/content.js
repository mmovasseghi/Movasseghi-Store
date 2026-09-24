document.addEventListener('DOMContentLoaded', () => {
  const page = document.querySelector('[data-ct-page]');
  if (!page) return;

  page.querySelectorAll('[data-ct-faq]').forEach(btn => {
    btn.addEventListener('click', () => {
      const item = btn.closest('.ct-faq-item');
      if (!item) return;
      const open = item.classList.contains('is-open');
      page.querySelectorAll('.ct-faq-item.is-open').forEach(el => {
        el.classList.remove('is-open');
        el.querySelector('[data-ct-faq]')?.setAttribute('aria-expanded', 'false');
      });
      if (!open) {
        item.classList.add('is-open');
        btn.setAttribute('aria-expanded', 'true');
      }
    });
  });
});
