/**
 * TipTap rich text — bundled for offline / no CDN (build → wwwroot/js/rich-text-editor.bundle.js)
 */
import { Editor } from '@tiptap/core';
import StarterKit from '@tiptap/starter-kit';
import Link from '@tiptap/extension-link';
import Underline from '@tiptap/extension-underline';
import Placeholder from '@tiptap/extension-placeholder';
import Image from '@tiptap/extension-image';
import Table from '@tiptap/extension-table';
import TableRow from '@tiptap/extension-table-row';
import TableCell from '@tiptap/extension-table-cell';
import TableHeader from '@tiptap/extension-table-header';

function showFallback(root, err) {
  const sourceId = root.dataset.stRte;
  const existing = window.__rteEditors?.[sourceId];
  if (existing && !existing.isDestroyed) {
    console.warn('[st-rte] init skipped — editor already active', sourceId, err);
    return;
  }
  console.error('[st-rte] fallback', sourceId, err);
  const source = document.getElementById(sourceId);
  const mount = document.getElementById(sourceId + '-mount');
  const toolbar = root.querySelector('.st-rte-toolbar');
  if (!source || !mount) return;
  if (toolbar) toolbar.hidden = true;
  mount.replaceWith(source);
  source.hidden = false;
  source.removeAttribute('aria-hidden');
  source.classList.add('st-rte-fallback');
  const warn = document.createElement('p');
  warn.className = 'st-rte-fallback-warn';
  warn.textContent =
    'ویرایشگر پیشرفته بارگذاری نشد — می‌توانید HTML را مستقیم در کادر زیر ویرایش کنید.';
  root.querySelector('.st-rte-shell')?.prepend(warn);
}

async function initRichTextEditor(root) {
  if (root.dataset.stRteReady === '1') return window.__rteEditors?.[root.dataset.stRte];
  const sourceId = root.dataset.stRte;
  const existing = window.__rteEditors?.[sourceId];
  if (existing && !existing.isDestroyed) {
    root.dataset.stRteReady = '1';
    return existing;
  }
  const source = document.getElementById(sourceId);
  const fieldName = root.dataset.stRteField || 'Description';
  const mount = document.getElementById(sourceId + '-mount');
  const preview = document.getElementById(sourceId + '-preview');
  if (!source || !mount) return;

  const syncSource = (ed) => {
    const html = ed.getHTML();
    source.value = html;
    source.dispatchEvent(new Event('input', { bubbles: true }));
    if (preview && !preview.hidden) preview.innerHTML = html;
    updateToolbarState(ed);
  };

  const extensions = [
    StarterKit.configure({ heading: { levels: [2, 3] } }),
    Underline,
    Link.configure({
      openOnClick: false,
      autolink: true,
      linkOnPaste: true,
      HTMLAttributes: { rel: 'noopener noreferrer' },
    }),
    Image.configure({
      inline: false,
      allowBase64: false,
      HTMLAttributes: { class: 'rte-inline-image', loading: 'lazy', decoding: 'async' },
    }),
    Table.configure({ resizable: false }),
    TableRow,
    TableHeader,
    TableCell,
    Placeholder.configure({ placeholder: 'محتوا را اینجا بنویسید…' }),
  ];

  const baseEditorOptions = {
    element: mount,
    extensions,
    editorProps: {
      attributes: {
        class: 'st-rte-editor ProseMirror',
        dir: 'rtl',
        spellcheck: 'true',
        'data-gramm': 'false',
      },
      handleKeyDown(view, event) {
        const key = event.key;
        if (key !== 'Backspace' && key !== 'Delete') return false;
        const { state, dispatch } = view;
        if (!state.selection.empty) {
          dispatch(state.tr.deleteSelection());
          event.preventDefault();
          return true;
        }
        return false;
      },
    },
    onUpdate: ({ editor: ed }) => syncSource(ed),
    onSelectionUpdate: ({ editor: ed }) => updateToolbarState(ed),
  };

  let initialContent = source.value?.trim() ? source.value : '<p></p>';
  let editor;
  try {
    editor = new Editor({ ...baseEditorOptions, content: initialContent });
  } catch (parseErr) {
    console.warn('[st-rte] content parse failed, using empty document', parseErr);
    editor = new Editor({ ...baseEditorOptions, content: '<p></p>' });
  }

  function setMode(mode) {
    root.querySelectorAll('[data-rte-mode]').forEach((b) =>
      b.classList.toggle('is-active', b.dataset.rteMode === mode));
    mount.hidden = mode !== 'edit';
    if (preview) {
      preview.hidden = mode !== 'preview';
      if (mode === 'preview') preview.innerHTML = editor.getHTML();
    }
  }

  root.querySelectorAll('[data-rte-mode]').forEach((btn) => {
    btn.addEventListener('click', () => setMode(btn.dataset.rteMode));
  });

  function normalizeUrl(raw) {
    const url = (raw || '').trim();
    if (!url) return '';
    if (/^(https?:\/\/|mailto:|tel:|\/|#)/i.test(url)) return url;
    return 'https://' + url;
  }

  function openLinkModal() {
    let modal = document.getElementById('st-rte-link-modal');
    if (!modal) {
      modal = document.createElement('div');
      modal.id = 'st-rte-link-modal';
      modal.className = 'st-rte-modal';
      modal.innerHTML = `
        <div class="st-rte-modal-box" role="dialog" aria-label="ویرایش لینک">
          <h4>افزودن / ویرایش لینک</h4>
          <div class="st-field">
            <label for="st-rte-link-url">آدرس URL</label>
            <input id="st-rte-link-url" class="st-input" type="url" placeholder="https://example.com یا /Catalog" dir="ltr" />
          </div>
          <div class="st-field">
            <label for="st-rte-link-text">متن نمایشی (اختیاری)</label>
            <input id="st-rte-link-text" class="st-input" type="text" placeholder="متن لینک" />
          </div>
          <label class="st-checkbox">
            <input type="checkbox" id="st-rte-link-blank" /> باز شدن در تب جدید
          </label>
          <div class="st-rte-modal-actions">
            <button type="button" class="st-btn st-btn-ghost" data-rte-link-cancel>انصراف</button>
            <button type="button" class="st-btn st-btn-ghost st-btn--danger" data-rte-link-remove>حذف لینک</button>
            <button type="button" class="st-btn st-btn-primary" data-rte-link-save>ذخیره</button>
          </div>
        </div>`;
      document.body.appendChild(modal);
      modal.addEventListener('click', (e) => {
        if (e.target === modal) closeLinkModal();
      });
      modal.querySelector('[data-rte-link-cancel]').addEventListener('click', closeLinkModal);
      modal.querySelector('[data-rte-link-remove]').addEventListener('click', () => {
        editor.chain().focus().extendMarkRange('link').unsetLink().run();
        closeLinkModal();
      });
      modal.querySelector('[data-rte-link-save]').addEventListener('click', applyLink);
    }

    const prev = editor.getAttributes('link');
    const urlInput = modal.querySelector('#st-rte-link-url');
    const textInput = modal.querySelector('#st-rte-link-text');
    const blankInput = modal.querySelector('#st-rte-link-blank');
    urlInput.value = prev.href || '';
    blankInput.checked = prev.target === '_blank';
    const { from, to } = editor.state.selection;
    textInput.value = editor.state.doc.textBetween(from, to, '') || '';
    modal.classList.add('is-open');
    urlInput.focus();
  }

  function closeLinkModal() {
    document.getElementById('st-rte-link-modal')?.classList.remove('is-open');
  }

  function applyLink() {
    const modal = document.getElementById('st-rte-link-modal');
    const url = normalizeUrl(modal.querySelector('#st-rte-link-url').value);
    const text = modal.querySelector('#st-rte-link-text').value.trim();
    const blank = modal.querySelector('#st-rte-link-blank').checked;
    if (!url) {
      editor.chain().focus().extendMarkRange('link').unsetLink().run();
      closeLinkModal();
      return;
    }
    const attrs = { href: url };
    if (blank) attrs.target = '_blank';
    const { from, to, empty } = editor.state.selection;
    if (empty) {
      const label = text || url;
      editor.chain().focus().insertContent({
        type: 'text',
        text: label,
        marks: [{ type: 'link', attrs }],
      }).run();
    } else if (text && text !== editor.state.doc.textBetween(from, to, '')) {
      editor.chain().focus().deleteSelection()
        .insertContent({ type: 'text', text, marks: [{ type: 'link', attrs }] }).run();
    } else {
      editor.chain().focus().extendMarkRange('link').setLink(attrs).run();
    }
    closeLinkModal();
  }

  function updateToolbarState(ed) {
    root.querySelectorAll('[data-cmd]').forEach((btn) => {
      const cmd = btn.dataset.cmd;
      let active = false;
      if (cmd === 'bold') active = ed.isActive('bold');
      else if (cmd === 'italic') active = ed.isActive('italic');
      else if (cmd === 'underline') active = ed.isActive('underline');
      else if (cmd === 'h2') active = ed.isActive('heading', { level: 2 });
      else if (cmd === 'h3') active = ed.isActive('heading', { level: 3 });
      else if (cmd === 'bulletList') active = ed.isActive('bulletList');
      else if (cmd === 'orderedList') active = ed.isActive('orderedList');
      else if (cmd === 'blockquote') active = ed.isActive('blockquote');
      else if (cmd === 'link') active = ed.isActive('link');
      btn.classList.toggle('is-active', active);
    });
  }

  const cmds = {
    bold: () => editor.chain().focus().toggleBold().run(),
    italic: () => editor.chain().focus().toggleItalic().run(),
    underline: () => editor.chain().focus().toggleUnderline().run(),
    h2: () => editor.chain().focus().toggleHeading({ level: 2 }).run(),
    h3: () => editor.chain().focus().toggleHeading({ level: 3 }).run(),
    bulletList: () => editor.chain().focus().toggleBulletList().run(),
    orderedList: () => editor.chain().focus().toggleOrderedList().run(),
    blockquote: () => editor.chain().focus().toggleBlockquote().run(),
    hr: () => editor.chain().focus().setHorizontalRule().run(),
    link: openLinkModal,
    unlink: () => editor.chain().focus().unsetLink().run(),
    undo: () => editor.chain().focus().undo().run(),
    redo: () => editor.chain().focus().redo().run(),
    clear: () => editor.chain().focus().clearNodes().unsetAllMarks().run(),
  };

  root.querySelectorAll('[data-cmd]').forEach((btn) => {
    btn.addEventListener('click', (e) => {
      e.preventDefault();
      cmds[btn.dataset.cmd]?.();
    });
  });

  const form = source.closest('form');
  const flushToSource = () => {
    source.value = editor.getHTML();
  };
  const pushToFormData = (fd) => {
    if (!fd) return;
    fd.set(fieldName, editor.getHTML());
  };
  form?.addEventListener('formdata', (e) => pushToFormData(e.formData));
  form?.addEventListener('submit', () => flushToSource(), { capture: true });
  mount.addEventListener('compositionend', () => syncSource(editor));

  window.__rteEditors = window.__rteEditors || {};
  window.__rteEditors[sourceId] = editor;
  root.dataset.stRteReady = '1';
  syncSource(editor);
  updateToolbarState(editor);
  return editor;
}

function bootRichTextEditors() {
  document.querySelectorAll('[data-st-rte]:not([data-st-rte-ready])').forEach((root) => {
    initRichTextEditor(root).catch((err) => showFallback(root, err));
  });
}

window.stBootRichTextEditors = bootRichTextEditors;
bootRichTextEditors();
