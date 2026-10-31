import {test} from 'node:test';
import assert from 'node:assert/strict';
import {transformSync} from 'esbuild';
import {homeRules} from './home-css-rules.mjs';
const minify = css => transformSync(css,{loader:'css',minify:true}).code.trim();
test('removes multiline page rules but preserves public mixed groups',()=>{
 const css='.ms-account-page {\n color:red;\n}\n.ms-account-name,.ms-auth-panel { color:blue }';
 assert.equal(minify(homeRules(css)),minify('.ms-account-name,.ms-auth-panel{color:blue}'));
});
test('nested responsive public states survive and empty private blocks disappear',()=>{
 const css='@media(min-width:768px){@supports(display:grid){.pd-buy{display:flex}.ms-auth-panel{display:grid}}}@media(max-width:767px){.catalog-head{color:red}}';
 assert.equal(minify(homeRules(css)),minify('@media(min-width:768px){@supports(display:grid){.ms-auth-panel{display:grid}}}'));
});
test('strings, escaped braces, comments and keyframes remain valid',()=>{
 const shared='@layer shared;/* } */.ms-auth-panel::before{content:"};{,"}.literal\\{brace{color:red}@keyframes ms-account-pulse{0%{opacity:0}to{opacity:1}}';
 assert.equal(minify(homeRules(shared)),minify(shared));
});
test('complex public selectors are kept conservatively',()=>{
 const css=':is(.ms-account-panel,.ms-auth-panel){padding:2px}.ms-account-panel:is(.a,.b){color:red}@font-face{font-family:test;src:url(test.woff2)}';
 assert.equal(minify(homeRules(css)),minify(css));
});
test('invalid syntax fails the build rather than publishing a truncated stylesheet',()=>{
 for(const css of ['.x{color:red','/* unterminated','.x{content:"abc}'])assert.throws(()=>homeRules(css));
});

test('unquoted data URLs can contain braces and semicolons',()=>{
 const css='.ms-auth-panel{--template:url(data:text/plain,{);color:red}.pd-page{color:blue}';
 assert.equal(minify(homeRules(css)),minify('.ms-auth-panel{--template:url(data:text/plain,{);color:red}'));
});

test('header mapping preserves nested cascade and filters only selected keyframes', async () => {
  const { mapStyleRules } = await import('./home-css-rules.mjs');
  const input = '@font-face{font-family:x;src:url(x)}@media(min-width:1px){.header,.shared{color:red}.main{color:blue}}@keyframes used{to{opacity:1}}@keyframes unused{to{opacity:0}}';
  const result = mapStyleRules(input, (h,b,original) => h.includes('.header') ? original+'{'+b+'}' : '',
    (h,b,original) => h.includes('unused') ? '' : original+'{'+b+'}');
  assert.equal(result, '@font-face{font-family:x;src:url(x)}@media(min-width:1px){.header,.shared{color:red}}@keyframes used{to{opacity:1}}');
});
