(function () {
  const hasGsap = typeof gsap !== 'undefined';
  const reduced = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
  const lineCount = parseInt(document.querySelector('.cart-analytics')?.dataset.cartLineCount || '0', 10)
    || document.querySelectorAll('.ms-cart-item--page').length;
  const heavyCart = lineCount > 12;

  if (!heavyCart && !reduced) {
    document.querySelectorAll('.cart-bar-fill').forEach((el, i) => {
      el.style.animationDelay = `${Math.min(i, 14) * 0.08}s`;
    });
  }

  function formatToman(n) {
    return Math.round(n).toLocaleString('fa-IR');
  }

  function animateCartInvoice() {
    const invoice = document.querySelector('[data-cart-invoice]');
    const payable = invoice?.querySelector('[data-cart-payable]');
    if (!invoice || !hasGsap || reduced) return;

    const amountEl = payable?.querySelector('[data-cart-payable-amount]');
    const target = parseFloat(payable?.dataset.payableValue || '0');

    const tl = gsap.timeline({ defaults: { ease: 'power3.out' } });

    tl.from(invoice.querySelector('.cart-invoice__head'), { y: -12, opacity: 0, duration: 0.45 }, 0.05);

    const invoiceRows = [...invoice.querySelectorAll('[data-invoice-row]')].filter(
      row => !row.hasAttribute('data-cart-payable') && !row.hasAttribute('data-cart-discount')
    );
    const rowsToAnimate = heavyCart ? invoiceRows.slice(0, 6) : invoiceRows;
    rowsToAnimate.forEach((row, i) => {
      tl.from(row, { x: 16, opacity: 0, duration: 0.38 }, 0.12 + i * 0.07);
    });

    invoice.querySelectorAll('[data-cart-discount]').forEach((row, di) => {
      const amountEl = row.querySelector('[data-cart-discount-amount]');
      const target = parseFloat(row.dataset.discountValue || '0');
      const icon = row.querySelector('.cart-invoice__discount-icon');
      if (!amountEl || !target) return;

      const counter = { val: 0 };
      amountEl.textContent = formatToman(0);

      tl.from(icon, { y: -10, opacity: 0, scale: 0.7, duration: 0.4, ease: 'back.out(2)' }, 0.28 + di * 0.08);
      tl.to(counter, {
        val: target,
        duration: 0.75,
        ease: 'power2.out',
        onUpdate: () => { amountEl.textContent = formatToman(counter.val); },
      }, 0.32 + di * 0.08);
      tl.from(row.querySelector('.cart-invoice__save-num'), { opacity: 0, x: -8, duration: 0.35 }, 0.3 + di * 0.08);
    });

    if (payable && amountEl && target) {
      const startVal = target * 0.97;
      const counter = { val: startVal };
      amountEl.textContent = formatToman(startVal);

      tl.fromTo(payable,
        { autoAlpha: 0, y: 10 },
        { autoAlpha: 1, y: 0, duration: 0.5, ease: 'power2.out', clearProps: 'transform,opacity,visibility' },
        0.4
      );

      tl.to(counter, {
        val: target,
        duration: 0.85,
        ease: 'power2.out',
        onUpdate: () => { amountEl.textContent = formatToman(counter.val); },
      }, 0.48);
    }
  }

  if (!hasGsap || reduced || heavyCart) return;

  const tierNudge = document.querySelector('[data-cart-tier-nudge]');
  if (tierNudge) {
    gsap.from(tierNudge, { y: 12, opacity: 0, scale: 0.98, duration: 0.5, ease: 'power2.out', delay: 0.1 });
    const fill = tierNudge.querySelector('[data-tier-fill]');
    if (fill) {
      const target = fill.style.width;
      fill.style.width = '0%';
      gsap.to(fill, { width: target, duration: 0.95, ease: 'power2.out', delay: 0.25 });
    }
    const callout = tierNudge.querySelector('.cart-tier-nudge__callout');
    if (callout) {
      gsap.from(callout, { x: 10, opacity: 0, duration: 0.4, ease: 'power2.out', delay: 0.35 });
    }
  }

  const premiumCards = document.querySelectorAll('[data-cart-premium]');
  premiumCards.forEach(premium => {
    gsap.from(premium, { y: 12, opacity: 0, duration: 0.5, ease: 'back.out(1.4)', delay: 0.15 });
    const badge = premium.querySelector('.cart-premium-card__badge');
    if (badge) gsap.from(badge, { scale: 0, rotation: -180, duration: 0.55, ease: 'back.out(2.5)', delay: 0.28 });
    const cta = premium.querySelector('.cart-premium-card__cta');
    if (cta) gsap.from(cta, { scale: 0.94, opacity: 0, duration: 0.35, ease: 'back.out(1.6)', delay: 0.38 });
  });

  animateCartInvoice();
})();
