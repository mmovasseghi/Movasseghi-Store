// Drop only rules whose every selector starts in a separate page's namespace.
// Mixed selector groups, keyframes, shared modal styles and unknown at-rules stay.
const exclusive = /^\.(?:ms-account-|ms-addr-|ms-order-|catalog-|pd-)/;
export function homeRules(css) {
  let result = '', start = 0, opening = -1, depth = 0, parentheses = 0, quote = '';
  for (let i = 0; i < css.length; i++) {
    const c = css[i];
    if (c === '\\') { i++; continue; }
    if (quote) { if (c === quote) quote = ''; continue; }
    if (c === '"' || c === "'") { quote = c; continue; }
    if (c === '/' && css[i + 1] === '*') {
      const end = css.indexOf('*/', i + 2);
      if (end < 0) throw new Error('Unterminated CSS comment');
      i = end + 1; continue;
    }
    if (c === '(') { parentheses++; continue; }
    if (c === ')') { if (--parentheses < 0) throw new Error('Unbalanced CSS'); continue; }
    if (parentheses) continue;
    if (c === '{') { if (depth++ === 0) opening = i; }
    else if (c === '}') {
      if (--depth < 0) throw new Error('Unbalanced CSS');
      if (depth !== 0) continue;
      const originalHeader = css.slice(start, opening);
      const header = originalHeader.replace(/\/\*[\s\S]*?\*\//g, '').trim();
      const body = css.slice(opening + 1, i);
      if (/^@(media|supports|layer|container)\b/i.test(header)) {
        const kept = homeRules(body);
        if (kept.trim()) result += originalHeader + '{' + kept + '}';
      } else if (header.startsWith('@') || !header.split(',').every(s => exclusive.test(s.trim()))) {
        result += css.slice(start, i + 1);
      }
      start = i + 1;
    } else if (c === ';' && depth === 0) {
      result += css.slice(start, i + 1); start = i + 1;
    }
  }
  if (depth !== 0 || parentheses !== 0 || quote) throw new Error('Unbalanced CSS');
  return result + css.slice(start);
}
