/**
 * Legacy entry — TipTap runs from rich-text-editor.bundle.js (no CDN).
 * If an old cached page still loads this file as a module, do not fetch jsdelivr
 * (that failure used to replace a working editor with the HTML fallback).
 */
if (!window.__stRteBootstrapped && !window.__stRteLegacyWarned) {
  window.__stRteLegacyWarned = true;
  console.warn(
    '[st-rte] rich-text-editor.js is deprecated. Hard-refresh (Ctrl+F5) so the page loads ~/js/rich-text-editor.bundle.js.',
  );
}
