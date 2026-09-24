document.addEventListener('DOMContentLoaded', () => {
  const page = document.querySelector('[data-pd-page]');
  if (!page) return;

  const hasGsap = typeof gsap !== 'undefined';
  const reduced = window.matchMedia('(prefers-reduced-motion: reduce)').matches;

  /* Product gallery slider */
  (function initGallery() {
    const gallery = page.querySelector('[data-pd-gallery-multi]');
    if (!gallery) return;

    const slides = [...gallery.querySelectorAll('[data-pd-slide]')];
    const thumbs = [...gallery.querySelectorAll('[data-pd-thumb]')];
    const prevBtn = gallery.querySelector('[data-pd-gallery-prev]');
    const nextBtn = gallery.querySelector('[data-pd-gallery-next]');
    const counter = gallery.querySelector('[data-pd-gallery-counter]');
    const progressBar = gallery.querySelector('[data-pd-gallery-progress]');
    const stage = gallery.querySelector('.pd-gallery-stage');
    const rail = gallery.querySelector('[data-pd-gallery-rail]');
    const indicator = gallery.querySelector('[data-pd-gallery-indicator]');

    if (slides.length < 2) return;

    let index = 0;
    let animating = false;
    let autoplayTimer = null;
    const autoplayMs = reduced ? 0 : 5200;

    const faDigits = '۰۱۲۳۴۵۶۷۸۹';
    const toFa = n => String(n).replace(/\d/g, d => faDigits[d]);

    function setStageMode(i) {
      if (!stage) return;
      const scene = slides[i]?.classList.contains('pd-gallery-slide--scene');
      stage.classList.toggle('pd-gallery-main--scene', scene);
      stage.classList.toggle('pd-gallery-main--product', !scene);
    }

    function setIndicator(i) {
      if (!indicator || !rail || !thumbs[i]) return;
      const thumb = thumbs[i];
      const railRect = rail.getBoundingClientRect();
      const thumbRect = thumb.getBoundingClientRect();
      const indW = indicator.offsetWidth;
      let left = thumbRect.left - railRect.left + thumbRect.width / 2 - indW / 2;
      const maxLeft = Math.max(0, rail.clientWidth - indW);
      left = Math.max(0, Math.min(maxLeft, left));
      indicator.style.transform = `translateX(${left}px)`;
    }

    function setUi(i) {
      thumbs.forEach((t, ti) => {
        const on = ti === i;
        t.classList.toggle('is-active', on);
        t.setAttribute('aria-selected', on ? 'true' : 'false');
      });
      if (counter) counter.textContent = `${toFa(i + 1)} / ${toFa(slides.length)}`;
      setIndicator(i);
    }

    function resetProgress() {
      if (!progressBar || !hasGsap || reduced || !autoplayMs) return;
      gsap.killTweensOf(progressBar);
      gsap.set(progressBar, { width: '0%' });
      gsap.to(progressBar, { width: '100%', duration: autoplayMs / 1000, ease: 'none' });
    }

    function goTo(nextIndex, dir = 1) {
      if (animating || nextIndex === index) return;
      const prev = slides[index];
      const next = slides[nextIndex];
      animating = true;

      prev.classList.remove('is-active');
      prev.classList.add('is-leaving');
      next.classList.add('is-active');

      if (hasGsap && !reduced) {
        const outX = dir * -28;
        const inX = dir * 28;
        gsap.timeline({
          defaults: { ease: 'power3.out' },
          onComplete: () => {
            prev.classList.remove('is-leaving');
            gsap.set([prev, next], { clearProps: 'opacity,transform,x,scale' });
            animating = false;
          },
        })
          .fromTo(prev, { opacity: 1, x: 0, scale: 1 }, { opacity: 0, x: outX, scale: 0.94, duration: 0.42 }, 0)
          .fromTo(next, { opacity: 0, x: inX, scale: 1.04 }, { opacity: 1, x: 0, scale: 1, duration: 0.52 }, 0.06);
      } else {
        prev.classList.remove('is-leaving');
        animating = false;
      }

      index = nextIndex;
      setUi(index);
      setStageMode(index);
      resetProgress();
    }

    function next() { goTo((index + 1) % slides.length, 1); }
    function prev() { goTo((index - 1 + slides.length) % slides.length, -1); }

    prevBtn?.addEventListener('click', () => { stopAutoplay(); prev(); startAutoplay(); });
    nextBtn?.addEventListener('click', () => { stopAutoplay(); next(); startAutoplay(); });

    thumbs.forEach(btn => {
      btn.addEventListener('click', () => {
        const i = parseInt(btn.dataset.pdIndex, 10);
        if (Number.isNaN(i) || i === index) return;
        stopAutoplay();
        goTo(i, i > index ? 1 : -1);
        startAutoplay();
      });
    });

    window.addEventListener('resize', () => setIndicator(index));

    function startAutoplay() {
      if (!autoplayMs) return;
      stopAutoplay();
      autoplayTimer = setInterval(next, autoplayMs);
      resetProgress();
    }

    function stopAutoplay() {
      if (autoplayTimer) clearInterval(autoplayTimer);
      autoplayTimer = null;
      if (progressBar && hasGsap) gsap.killTweensOf(progressBar);
    }

    gallery.addEventListener('mouseenter', stopAutoplay);
    gallery.addEventListener('mouseleave', startAutoplay);
    gallery.addEventListener('focusin', stopAutoplay);
    gallery.addEventListener('focusout', e => {
      if (!gallery.contains(e.relatedTarget)) startAutoplay();
    });

    /* Touch swipe */
    let touchStartX = 0;
    stage?.addEventListener('touchstart', e => {
      touchStartX = e.changedTouches[0]?.clientX ?? 0;
      stopAutoplay();
    }, { passive: true });
    stage?.addEventListener('touchend', e => {
      const dx = (e.changedTouches[0]?.clientX ?? 0) - touchStartX;
      if (Math.abs(dx) > 40) (dx < 0 ? next : prev)();
      startAutoplay();
    }, { passive: true });

    /* Keyboard */
    gallery.addEventListener('keydown', e => {
      if (e.key === 'ArrowLeft') { e.preventDefault(); stopAutoplay(); next(); startAutoplay(); }
      if (e.key === 'ArrowRight') { e.preventDefault(); stopAutoplay(); prev(); startAutoplay(); }
    });

    /* Entrance */
    if (hasGsap && !reduced) {
      gsap.from(gallery.querySelector('.pd-gallery-stage'), { opacity: 0, y: 16, duration: 0.55, ease: 'power2.out' });
      if (rail) gsap.from(rail, { opacity: 0, y: 12, duration: 0.45, delay: 0.15, ease: 'power2.out' });
      gsap.from(thumbs, { opacity: 0, scale: 0.92, stagger: 0.08, duration: 0.4, delay: 0.22, ease: 'back.out(1.5)' });
    }

    setUi(0);
    setStageMode(0);
    startAutoplay();
  })();

  /* Breadcrumb entrance + scroll current into view on mobile */
  (function initBreadcrumb() {
    const nav = page.querySelector('[data-pd-breadcrumb]');
    if (!nav) return;

    const shell = nav.querySelector('.pd-breadcrumb__shell');
    const track = nav.querySelector('.pd-breadcrumb__track');
    const items = [...nav.querySelectorAll('.pd-breadcrumb__item')];
    const current = nav.querySelector('.pd-breadcrumb__item--current');

    if (hasGsap && !reduced) {
      gsap.set([shell, ...items], { opacity: 0, y: 10 });
      gsap.timeline({ defaults: { ease: 'power2.out' } })
        .to(shell, { opacity: 1, y: 0, duration: 0.45 })
        .to(items, { opacity: 1, y: 0, duration: 0.38, stagger: 0.07 }, '-=0.2');

      if (current && window.matchMedia('(max-width: 767px)').matches) {
        gsap.fromTo(current,
          { boxShadow: '0 4px 14px rgba(36, 56, 48, 0.22)' },
          { boxShadow: '0 4px 22px rgba(61, 90, 76, 0.42)', duration: 1.1, repeat: -1, yoyo: true, ease: 'sine.inOut', delay: 0.6 }
        );
      }
    }

    if (current && track) {
      requestAnimationFrame(() => {
        if (track.scrollWidth > track.clientWidth + 2) {
          current.scrollIntoView({ behavior: reduced ? 'auto' : 'smooth', inline: 'center', block: 'nearest' });
        } else {
          track.scrollLeft = 0;
        }
      });
    }
  })();

  /* Sticky CTA — hidden while buy box is on screen (avoids covering gallery thumbs) */
  (function initStickyBar() {
    const sticky = page.querySelector('[data-pd-sticky]');
    const buybox = page.querySelector('.pd-buybox');
    if (!sticky || !buybox) return;

    const sync = visible => sticky.classList.toggle('pd-sticky--hidden', visible);

    if ('IntersectionObserver' in window) {
      const io = new IntersectionObserver(([entry]) => sync(entry.isIntersecting), {
        threshold: 0,
      });
      io.observe(buybox);
    } else {
      sync(true);
    }
  })();

  /* Reviews carousel — scroll-snap (RTL-safe) */
  (function initReviewsCarousel() {
    const root = page.querySelector('[data-pd-reviews-carousel]');
    if (!root) return;

    const viewport = root.querySelector('[data-pd-reviews-viewport]');
    const track = root.querySelector('[data-pd-reviews-track]');
    const cards = track ? [...track.querySelectorAll('[data-pd-reviews-slide]')] : [];
    const prevBtn = root.querySelector('[data-pd-reviews-prev]');
    const nextBtn = root.querySelector('[data-pd-reviews-next]');
    const counter = root.querySelector('[data-pd-reviews-counter]');
    const dotsWrap = root.querySelector('[data-pd-reviews-dots]');
    const progressBar = root.querySelector('[data-pd-reviews-progress]');
    if (!viewport || !track || cards.length === 0) return;

    track.style.transform = 'none';
    if (hasGsap) gsap.set(track, { clearProps: 'transform' });

    const faDigits = '۰۱۲۳۴۵۶۷۸۹';
    const toFa = n => String(n).replace(/\d/g, d => faDigits[d]);

    function slidesPerView() {
      const w = window.innerWidth;
      if (w >= 960) return Math.min(3, cards.length);
      if (w >= 640) return Math.min(2, cards.length);
      return 1;
    }

    function maxIndex() {
      return Math.max(0, cards.length - slidesPerView());
    }

    let index = 0;
    let dots = [];
    let scrollLock = false;

    function layoutSlides() {
      const gap = parseFloat(getComputedStyle(track).gap) || 16;
      const spv = slidesPerView();
      const w = Math.max(120, (viewport.clientWidth - gap * Math.max(0, spv - 1)) / spv);
      cards.forEach(card => {
        card.style.flex = `0 0 ${w}px`;
        card.style.width = `${w}px`;
      });
      rebuildDots();
      if (index > maxIndex()) scrollToIndex(maxIndex(), false);
      else syncUi(false);
    }

    function rebuildDots() {
      if (!dotsWrap) return;
      dotsWrap.innerHTML = '';
      dots = [];
      const max = maxIndex();
      if (max <= 0) return;
      for (let i = 0; i <= max; i++) {
        const b = document.createElement('button');
        b.type = 'button';
        b.className = 'pd-reviews-carousel__dot';
        b.setAttribute('aria-label', `نظر ${i + 1}`);
        b.addEventListener('click', () => scrollToIndex(i, true));
        dotsWrap.appendChild(b);
        dots.push(b);
      }
    }

    function readIndexFromScroll() {
      const vp = viewport.getBoundingClientRect();
      const center = vp.left + vp.width * 0.5;
      let best = index;
      let dist = Infinity;
      cards.forEach((card, i) => {
        const r = card.getBoundingClientRect();
        const c = r.left + r.width * 0.5;
        const d = Math.abs(c - center);
        if (d < dist) {
          dist = d;
          best = i;
        }
      });
      return Math.min(best, maxIndex());
    }

    function scrollToIndex(i, animate) {
      const max = maxIndex();
      index = Math.max(0, Math.min(i, max));
      const card = cards[index];
      if (!card) return;
      scrollLock = true;
      card.scrollIntoView({
        behavior: animate && !reduced ? 'smooth' : 'instant',
        inline: 'start',
        block: 'nearest',
      });
      window.setTimeout(() => {
        scrollLock = false;
        syncUi(false);
      }, animate ? 420 : 0);
      syncUi(false);
    }

    function markActiveCards() {
      const spv = slidesPerView();
      cards.forEach((card, i) => {
        card.classList.toggle('is-carousel-active', i >= index && i < index + spv);
      });
    }

    function syncUi() {
      const spv = slidesPerView();
      const max = maxIndex();
      index = Math.min(index, max);
      if (prevBtn) prevBtn.disabled = index <= 0;
      if (nextBtn) nextBtn.disabled = index >= max;
      if (counter) {
        const end = Math.min(index + spv, cards.length);
        counter.textContent = `${toFa(index + 1)}–${toFa(end)} از ${toFa(cards.length)}`;
      }
      if (progressBar) {
        progressBar.style.width = max <= 0 ? '100%' : `${((index + 1) / (max + 1)) * 100}%`;
      }
      dots.forEach((d, i) => d.classList.toggle('is-active', i === index));
      markActiveCards();
    }

    prevBtn?.addEventListener('click', () => scrollToIndex(index - 1, true));
    nextBtn?.addEventListener('click', () => scrollToIndex(index + 1, true));

    viewport.addEventListener('scroll', () => {
      if (scrollLock) return;
      const next = readIndexFromScroll();
      if (next !== index) {
        index = next;
        syncUi();
      }
    }, { passive: true });

    let resizeT;
    window.addEventListener('resize', () => {
      clearTimeout(resizeT);
      resizeT = setTimeout(layoutSlides, 120);
    });

    if (hasGsap && !reduced) {
      const stage = root.querySelector('.pd-voices__stage') || root.querySelector('.pd-reviews-carousel__stage');
      if (stage) {
        gsap.from(stage, { y: 20, opacity: 0, duration: 0.55, ease: 'power3.out' });
      }
      const summary = page.querySelector('.pd-voices__summary');
      if (summary) {
        gsap.from(summary, { x: 16, opacity: 0, duration: 0.5, ease: 'power2.out', delay: 0.08 });
      }
    }

    layoutSlides();
  })();

  /* FAQ accordion */
  page.querySelectorAll('[data-pd-faq]').forEach(btn => {
    btn.addEventListener('click', () => {
      const item = btn.closest('.pd-faq-item');
      if (!item) return;
      const open = item.classList.contains('is-open');
      page.querySelectorAll('.pd-faq-item.is-open').forEach(el => el.classList.remove('is-open'));
      if (!open) item.classList.add('is-open');
      btn.setAttribute('aria-expanded', (!open).toString());
    });
  });

  /* Smooth scroll to inquiry */
  page.querySelectorAll('[href="#pd-inquiry"]').forEach(link => {
    link.addEventListener('click', e => {
      const target = document.getElementById('pd-inquiry');
      if (!target) return;
      e.preventDefault();
      target.scrollIntoView({ behavior: 'smooth', block: 'start' });
    });
  });

  /* Short description accordion — open on load, sync open state */
  (function initShortFold() {
    const fold = page.querySelector('[data-pd-short-fold]');
    if (!fold) return;

    fold.open = true;
    fold.classList.add('pd-short-fold--open');

    const sync = () => {
      fold.classList.toggle('pd-short-fold--open', fold.open);
      const badge = fold.querySelector('.pd-short-fold__badge');
      if (badge) {
        badge.textContent = fold.open ? 'باز' : 'بسته';
        badge.hidden = false;
        badge.dataset.foldState = fold.open ? 'open' : 'closed';
      }
    };

    fold.addEventListener('toggle', sync);
    sync();

    if (!reduced && hasGsap) {
      gsap.fromTo(
        fold,
        { opacity: 0, y: 10 },
        { opacity: 1, y: 0, duration: 0.45, ease: 'power2.out', delay: 0.08 }
      );
    }
  })();

  /* Full description accordion — open on load, richer entrance */
  (function initArticleFold() {
    const fold = page.querySelector('[data-pd-article-fold]');
    if (!fold) return;

    fold.open = true;
    fold.classList.add('pd-article-fold--open');

    const sync = () => {
      fold.classList.toggle('pd-article-fold--open', fold.open);
      const badge = fold.querySelector('.pd-article-fold__badge');
      if (badge) {
        badge.textContent = fold.open ? 'باز' : 'بسته';
        badge.hidden = false;
        badge.dataset.foldState = fold.open ? 'open' : 'closed';
      }
    };

    fold.addEventListener('toggle', sync);
    sync();

    if (!reduced && hasGsap) {
      gsap.fromTo(
        fold,
        { opacity: 0, y: 16, scale: 0.985 },
        { opacity: 1, y: 0, scale: 1, duration: 0.58, ease: 'power2.out', delay: 0.12 }
      );
    }
  })();

  /* Wholesale inquiry hero */
  (function initInquiry() {
    const block = page.querySelector('[data-pd-inquiry]');
    if (!block || reduced || !hasGsap) return;

    gsap.fromTo(
      block,
      { opacity: 0, y: 22 },
      { opacity: 1, y: 0, duration: 0.65, ease: 'power2.out', delay: 0.05 }
    );
    const form = block.querySelector('.pd-inquiry__form');
    const contact = block.querySelector('.pd-inquiry__contact');
    if (form && contact) {
      gsap.fromTo(
        [contact, form],
        { opacity: 0, y: 12 },
        { opacity: 1, y: 0, duration: 0.5, stagger: 0.1, ease: 'power2.out', delay: 0.15 }
      );
    }
  })();

  /* AJAX add to cart → confirmation popup */
  page.querySelectorAll('[data-pd-add-form]').forEach(form => {
    form.addEventListener('submit', async e => {
      e.preventDefault();
      if (form.classList.contains('is-submitting')) return;

      const qtyInput = form.querySelector('[name="cartons"]');
      const qty = Math.max(1, parseInt(qtyInput?.value || '1', 10));
      if (qtyInput) qtyInput.value = String(qty);

      const fd = new FormData(form);
      fd.set('cartons', String(qty));
      fd.set('ajax', 'true');
      fd.set('confirm', 'true');

      form.classList.add('is-submitting');
      form.classList.remove('is-success');

      try {
        const res = await fetch(form.action, { method: 'POST', body: fd });
        if (!res.ok) throw new Error('add failed');
        const html = await res.text();
        form.classList.add('is-success');
        window.MsCart?.showAddConfirm?.(html);
        window.MsCart?.refreshBadge?.();
      } catch {
        form.submit();
      } finally {
        setTimeout(() => {
          form.classList.remove('is-submitting', 'is-success');
        }, 600);
      }
    });
  });
});
