(function () {
  'use strict';

  const PHASE_MS = 6000;
  const reduced = window.matchMedia('(prefers-reduced-motion: reduce)').matches;

  function init() {
    const cards = Array.from(document.querySelectorAll('[data-ms-product-dual]'));
    if (!cards.length || reduced) return;

    const visible = new Set();
    const animations = new Map();
    let elapsed = 0;
    let startedAt = null;
    let timer;
    let flipTimer;

    function currentTime() {
      return elapsed + (startedAt === null ? 0 : performance.now() - startedAt);
    }

    function cardAnimations(card) {
      if (animations.has(card)) return animations.get(card);
      const result = [];
      for (const [selector, from, to] of [
        ['.ms-product-dual__progress-fill', 'scaleX(0)', 'scaleX(1)'],
        ['.ms-product-dual__progress-glow', 'translateX(0)', 'translateX(100%)']
      ]) {
        const target = card.querySelector(selector);
        if (!target) continue;
        const animation = target.animate([{ transform: from }, { transform: to }],
          { duration: PHASE_MS, iterations: Infinity, easing: 'linear' });
        animation.pause();
        result.push(animation);
      }
      animations.set(card, result);
      return result;
    }

    function sync() {
      clearTimeout(timer);
      const time = currentTime();
      const phase = Math.floor(time / PHASE_MS) % 2 === 0 ? 'studio' : 'scene';
      const changing = [...visible].filter(card => card.dataset.msDualPhase !== phase);
      if (changing.length) {
        changing.forEach(card => {
          card.dataset.msDualPhase = phase;
          card.classList.add('ms-product-dual--flip');
        });
        clearTimeout(flipTimer);
        flipTimer = setTimeout(() => cards.forEach(card => card.classList.remove('ms-product-dual--flip')), 1300);
      }
      // Native transform animations keep the bar and glow synchronized without
      // a root CSS-variable write, layout read, or JS callback on every frame.
      for (const card of visible) {
        for (const animation of cardAnimations(card)) {
          if (animation.playState !== 'running') {
            animation.currentTime = time;
            animation.play();
          }
        }
      }
      if (startedAt !== null) timer = setTimeout(sync, PHASE_MS - time % PHASE_MS + 1);
    }

    function update() {
      if (visible.size && !document.hidden) {
        if (startedAt === null) startedAt = performance.now();
        sync();
      } else {
        if (startedAt !== null) elapsed = currentTime();
        startedAt = null;
        clearTimeout(timer);
        for (const group of animations.values()) group.forEach(animation => animation.pause());
      }
    }

    document.addEventListener('visibilitychange', update);
    if ('IntersectionObserver' in window) {
      const io = new IntersectionObserver(entries => {
        // Observer callbacks contain only changed entries, not all observed cards.
        for (const entry of entries) {
          if (entry.isIntersecting) visible.add(entry.target);
          else {
            visible.delete(entry.target);
            animations.get(entry.target)?.forEach(animation => animation.pause());
          }
        }
        update();
      }, { rootMargin: '80px', threshold: 0.01 });
      cards.forEach(card => io.observe(card));
    } else {
      cards.forEach(card => visible.add(card));
      update();
    }
  }

  if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', init);
  else init();
})();
