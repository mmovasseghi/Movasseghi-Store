(function () {
  'use strict';

  function flushAllRichTextEditors() {
    const editors = window.__rteEditors || {};
    Object.keys(editors).forEach((id) => {
      const ed = editors[id];
      if (!ed || ed.isDestroyed) return;
      const html = ed.getHTML();
      const ta = document.getElementById(id);
      if (ta) ta.value = html;
    });
  }

  window.stFlushAllRichTextEditors = flushAllRichTextEditors;

  document.addEventListener(
    'formdata',
    (e) => {
      const form = e.target;
      if (!form || form.tagName !== 'FORM') return;
      flushAllRichTextEditors();
      const editors = window.__rteEditors || {};
      Object.keys(editors).forEach((id) => {
        const ed = editors[id];
        if (!ed || ed.isDestroyed) return;
        const root = document.querySelector(`[data-st-rte="${id}"]`);
        const fieldName = root?.dataset?.stRteField;
        if (!fieldName) return;
        e.formData.set(fieldName, ed.getHTML());
      });
    },
    true,
  );

  document.addEventListener(
    'submit',
    (e) => {
      const form = e.target;
      if (!form || form.tagName !== 'FORM') return;
      flushAllRichTextEditors();
    },
    true,
  );
})();
