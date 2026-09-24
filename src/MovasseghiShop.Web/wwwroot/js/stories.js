(function () {
  'use strict';

  const STORAGE_KEY = 'ms-stories-viewed';
  const SLIDE_MS = 5000;
  const FADE_MS = 400;
  const HOLD_MS = 220;
  const NAV_COOLDOWN_MS = 400;

  const dataEl = document.getElementById('msStoriesData');
  const modal = document.getElementById('msStoryModal');
  if (!dataEl || !modal) return;

  let stories = [];
  try {
    const raw = JSON.parse(dataEl.textContent || '[]');
    stories = raw.map(normalizeStory);
  } catch {
    return;
  }

  const progressEl = modal.querySelector('[data-story-progress]');
  const slidesEl = modal.querySelector('[data-story-slides]');
  const labelEl = modal.querySelector('[data-story-viewer-label]');
  const iconEl = modal.querySelector('[data-story-viewer-icon]');
  const ctaEl = modal.querySelector('[data-story-cta]');
  const viewer = modal.querySelector('.ms-story-viewer');
  const tapPrev = modal.querySelector('[data-story-prev]');
  const tapNext = modal.querySelector('[data-story-next]');

  let viewed = new Set(JSON.parse(localStorage.getItem(STORAGE_KEY) || '[]'));
  let storyIdx = 0;
  let slideIdx = 0;
  let paused = false;
  let progressStart = 0;
  let progressElapsed = 0;
  let rafId = null;
  let closeTimer = null;
  let pointerDownAt = 0;
  let navLocked = false;

  function normalizeStory(s) {
    return {
      id: s.id ?? s.Id,
      label: s.label ?? s.Label,
      slides: (s.slides ?? s.Slides ?? []).map(sl => ({
        image: sl.image ?? sl.Image,
        title: sl.title ?? sl.Title,
        link: sl.link ?? sl.Link
      }))
    };
  }

  document.querySelectorAll('.ms-story[data-story-id]').forEach(btn => {
    if (viewed.has(btn.dataset.storyId)) btn.classList.add('is-viewed');
    btn.addEventListener('click', e => {
      e.preventDefault();
      open(btn.dataset.storyId);
    });
  });

  function markViewed(id) {
    viewed.add(id);
    localStorage.setItem(STORAGE_KEY, JSON.stringify([...viewed]));
    document.querySelector(`.ms-story[data-story-id="${CSS.escape(id)}"]`)?.classList.add('is-viewed');
  }

  function open(storyId) {
    storyIdx = stories.findIndex(s => s.id === storyId);
    if (storyIdx < 0) return;
    slideIdx = 0;
    clearTimeout(closeTimer);
    modal.hidden = false;
    document.body.classList.add('ms-story-open');
    requestAnimationFrame(() => modal.classList.add('is-open'));
    render();
    startTimer();
  }

  function close() {
    modal.classList.remove('is-open');
    stopTimer();
    closeTimer = setTimeout(() => {
      modal.hidden = true;
      document.body.classList.remove('ms-story-open');
      if (slidesEl) slidesEl.innerHTML = '';
    }, FADE_MS);
  }

  function render() {
    const story = stories[storyIdx];
    if (!story) return;

    if (labelEl) labelEl.textContent = story.label;
    const thumb = document.querySelector(`.ms-story[data-story-id="${CSS.escape(story.id)}"] .ms-story-icon-wrap`);
    if (thumb && iconEl) iconEl.innerHTML = thumb.innerHTML;

    if (progressEl) {
      progressEl.innerHTML = story.slides.map((_, i) =>
        `<div class="ms-story-progress-seg${i < slideIdx ? ' is-done' : ''}${i === slideIdx ? ' is-active' : ''}"><span></span></div>`
      ).join('');
    }

    const slide = story.slides[slideIdx];
    if (slide && slidesEl) {
      slidesEl.innerHTML = `<div class="ms-story-slide"><img src="${escapeAttr(slide.image)}" alt="${escapeAttr(slide.title)}" /></div>`;
    }
    if (ctaEl && slide) {
      ctaEl.href = slide.link || '#';
      ctaEl.hidden = !slide.link;
    }
    updateProgressBar(0);
  }

  function escapeHtml(s) {
    return String(s).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
  }

  function escapeAttr(s) { return escapeHtml(s).replace(/"/g, '&quot;'); }

  function updateProgressBar(p) {
    const bar = progressEl?.querySelector('.ms-story-progress-seg.is-active span');
    if (bar) bar.style.width = `${Math.min(100, p * 100)}%`;
  }

  function tick(now) {
    if (paused) return;
    const p = Math.min(1, (progressElapsed + now - progressStart) / SLIDE_MS);
    updateProgressBar(p);
    if (p >= 1) { nextSlide(); return; }
    rafId = requestAnimationFrame(tick);
  }

  function startTimer() {
    stopTimer();
    progressStart = performance.now();
    progressElapsed = 0;
    paused = false;
    rafId = requestAnimationFrame(tick);
  }

  function stopTimer() {
    cancelAnimationFrame(rafId);
    rafId = null;
  }

  function pauseTimer() {
    if (paused) return;
    paused = true;
    progressElapsed += performance.now() - progressStart;
    stopTimer();
  }

  function resumeTimer() {
    if (!paused) return;
    paused = false;
    progressStart = performance.now();
    rafId = requestAnimationFrame(tick);
  }

  function withNavLock(fn) {
    if (navLocked) return;
    navLocked = true;
    fn();
    setTimeout(() => { navLocked = false; }, NAV_COOLDOWN_MS);
  }

  function nextSlide() {
    stopTimer();
    const story = stories[storyIdx];
    if (!story) return;
    if (slideIdx < story.slides.length - 1) {
      slideIdx++;
      render();
      startTimer();
      return;
    }
    markViewed(story.id);
    if (storyIdx < stories.length - 1) {
      storyIdx++;
      slideIdx = 0;
      render();
      startTimer();
      return;
    }
    close();
  }

  function prevSlide() {
    stopTimer();
    if (slideIdx > 0) {
      slideIdx--;
      render();
      startTimer();
      return;
    }
    if (storyIdx > 0) {
      storyIdx--;
      slideIdx = stories[storyIdx].slides.length - 1;
      render();
      startTimer();
    } else {
      startTimer();
    }
  }

  function goNext(e) {
    if (e) {
      e.preventDefault();
      e.stopPropagation();
    }
    withNavLock(nextSlide);
  }

  function goPrev(e) {
    if (e) {
      e.preventDefault();
      e.stopPropagation();
    }
    withNavLock(prevSlide);
  }

  function isInteractiveTarget(el) {
    return el?.closest('[data-story-close], .ms-story-modal-close, .ms-story-cta, .ms-story-foot');
  }

  function onPointerDown(e) {
    if (isInteractiveTarget(e.target)) return;
    pointerDownAt = performance.now();
    pauseTimer();
  }

  function onTapZonePointerUp(e, go) {
    if (isInteractiveTarget(e.target)) return;
    const held = performance.now() - pointerDownAt >= HOLD_MS;
    resumeTimer();
    if (!held) go(e);
  }

  modal.querySelectorAll('[data-story-close]').forEach(el => el.addEventListener('click', close));

  tapPrev?.addEventListener('pointerup', e => onTapZonePointerUp(e, goPrev));
  tapNext?.addEventListener('pointerup', e => onTapZonePointerUp(e, goNext));

  viewer?.addEventListener('pointerdown', onPointerDown);
  viewer?.addEventListener('pointerup', e => {
    if (isInteractiveTarget(e.target)) return;
    if (e.target.closest('[data-story-prev], [data-story-next]')) return;
    resumeTimer();
  });
  viewer?.addEventListener('pointerleave', () => { if (paused) resumeTimer(); });
  viewer?.addEventListener('pointercancel', () => resumeTimer());

  document.addEventListener('keydown', e => {
    if (modal.hidden) return;
    if (e.key === 'Escape') close();
    if (e.key === 'ArrowRight') goNext();
    if (e.key === 'ArrowLeft') goPrev();
  });
})();
