// Parse first-party browser scripts without executing them or touching the DOM.
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const directory = path.join(__dirname, '../src/MovasseghiShop.Web/wwwroot/js');
const files = fs.readdirSync(directory).filter(name => name.endsWith('.js'));
for (const name of files) new vm.Script(fs.readFileSync(path.join(directory, name), 'utf8'), { filename: name });
console.log(`JavaScript syntax passed: ${files.length} browser scripts`);
