(function () {
  const url = (path) => (typeof window.appUrl === 'function' ? window.appUrl(path) : path);

  const banner = document.getElementById('studio-maintenance-banner');
  if (!banner) return;

  const taskEl = banner.querySelector('[data-studio-task]');
  const countEl = banner.querySelector('[data-studio-count]');
  const barEl = banner.querySelector('[data-studio-progress]');
  const doneEl = document.getElementById('studio-maintenance-done');
  const summaryEl = doneEl?.querySelector('[data-studio-summary]');
  const timeEl = doneEl?.querySelector('[data-studio-time]');

  let wasRunning = banner.dataset.running === 'true';
  let pollTimer = null;

  function formatTime(iso) {
    if (!iso) return '';
    try {
      const d = new Date(iso);
      return d.toLocaleString('fa-IR', { dateStyle: 'short', timeStyle: 'short' });
    } catch {
      return '';
    }
  }

  function setRunningUi(snap) {
    banner.hidden = false;
    banner.classList.remove('st-alert-success');
    banner.classList.add('st-alert-warn');
    if (doneEl) doneEl.hidden = true;
    if (taskEl) taskEl.textContent = snap.currentTask || '…';
    if (countEl) {
      countEl.textContent =
        snap.total > 0 ? ` (${snap.done}/${snap.total})` : '';
    }
    if (barEl && snap.total > 0) {
      const pct = Math.min(100, Math.round((snap.done / snap.total) * 100));
      barEl.style.width = pct + '%';
      barEl.setAttribute('aria-valuenow', String(pct));
    }
    banner.dataset.running = 'true';
  }

  function setIdleUi(snap) {
    banner.dataset.running = 'false';
    if (snap.lastSummary) {
      banner.hidden = true;
      if (doneEl) {
        doneEl.hidden = false;
        if (summaryEl) summaryEl.textContent = snap.lastSummary;
        if (timeEl) timeEl.textContent = formatTime(snap.lastCompletedAt);
      }
    } else {
      banner.hidden = true;
      if (doneEl) doneEl.hidden = true;
    }
  }

  function updateStats(stats) {
    if (!stats) return;
    const map = {
      'data-stat-readiness': stats.averageReadiness,
      'data-stat-score': stats.averageScore,
      'data-stat-competitive': stats.averageCompetitive,
      'data-stat-ready': stats.publishReadyCount,
      'data-stat-blocked': stats.blockedCount,
      'data-stat-actions': stats.openActions
    };
    Object.entries(map).forEach(([attr, val]) => {
      document.querySelectorAll(`[${attr}]`).forEach((el) => {
        if (typeof val !== 'number') return;
        const n = Math.round(val);
        if (attr === 'data-stat-readiness' || attr === 'data-stat-score') {
          el.innerHTML = n + '<small>/100</small>';
        } else {
          el.textContent = String(n);
        }
      });
    });
    const foot = document.querySelector('[data-stat-audited-foot]');
    if (foot && stats.auditedCount != null) {
      foot.textContent = `قدرت رقابتی ${Math.round(stats.averageCompetitive)} · ${stats.auditedCount} محصول تحلیل‌شده`;
    }
  }

  function renderActions(actions) {
    const host = document.getElementById('studio-priority-actions');
    if (!host || !Array.isArray(actions)) return;
    const token =
      document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
    if (actions.length === 0) {
      host.innerHTML = '<p class="st-ok">همه چیز مرتب است — عالی!</p>';
      return;
    }
    host.innerHTML = actions
      .map((action) => {
        let foot = '';
        if (action.entityType === 'product' && action.entityId) {
          foot = `<div class="st-action-foot">
                <a class="st-btn st-btn-ghost st-btn-sm" href="${url('/Admin/Products/Edit/' + action.entityId)}">ویرایش</a>
                <form method="post" action="${url('/Admin/Studio/AutoFixProduct')}" style="display:inline">
                  <input type="hidden" name="__RequestVerificationToken" value="${token}" />
                  <input type="hidden" name="id" value="${action.entityId}" />
                  <button type="submit" class="st-btn st-btn-primary st-btn-sm">✨ Auto-Fix</button>
                </form>
              </div>`;
        } else if (action.entityType === 'blog' && action.entityId) {
          foot = `<div class="st-action-foot">
                <a class="st-btn st-btn-ghost st-btn-sm" href="${url('/Admin/BlogAdmin/Edit/' + action.entityId)}">ویرایش</a>
                <form method="post" action="${url('/Admin/Studio/AutoFixBlog')}" style="display:inline">
                  <input type="hidden" name="__RequestVerificationToken" value="${token}" />
                  <input type="hidden" name="id" value="${action.entityId}" />
                  <button type="submit" class="st-btn st-btn-primary st-btn-sm">✨ Auto-Fix</button>
                </form>
              </div>`;
        }
        const desc = action.description
          ? `<small class="st-action-desc">${escapeHtml(action.description)}</small>`
          : '';
        return `<div class="st-action">
          <span class="st-action-priority ${escapeHtml(action.priority || 'medium')}"></span>
          <div class="st-action-body">
            <strong class="st-action-title">${escapeHtml(action.title || '')}</strong>
            ${desc}
            ${foot}
          </div>
        </div>`;
      })
      .join('');
  }

  function escapeHtml(s) {
    return String(s)
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;');
  }

  async function poll() {
    try {
      const res = await fetch(url('/Admin/Studio/DashboardLive'), { credentials: 'same-origin' });
      if (!res.ok) return;
      const data = await res.json();
      const snap = data.maintenance;
      if (snap.isRunning) {
        setRunningUi(snap);
        wasRunning = true;
        updateStats(data.stats);
      } else {
        if (wasRunning) {
          await fetch(url('/Admin/Studio/DashboardLive?refreshQueue=true'), { credentials: 'same-origin' })
            .then((r) => (r.ok ? r.json() : data))
            .then((fresh) => {
              updateStats(fresh.stats);
              renderActions(fresh.topActions);
            })
            .catch(() => {
              updateStats(data.stats);
              renderActions(data.topActions);
            });
        } else {
          updateStats(data.stats);
        }
        setIdleUi(snap);
        wasRunning = false;
        stopPoll();
        return;
      }
    } catch {
      /* ignore transient network errors */
    }
  }

  function startPoll() {
    if (pollTimer) return;
    pollTimer = window.setInterval(poll, 2000);
    poll();
  }

  function stopPoll() {
    if (pollTimer) {
      window.clearInterval(pollTimer);
      pollTimer = null;
    }
  }

  if (wasRunning || banner.dataset.running === 'true') startPoll();

  document.addEventListener('visibilitychange', () => {
    if (document.hidden) stopPoll();
    else if (banner.dataset.running === 'true') startPoll();
  });
})();
