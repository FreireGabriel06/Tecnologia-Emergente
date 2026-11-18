const hljs = require('highlight.js/lib/core'); hljs.registerLanguage('csharp', require('highlight.js/lib/languages/csharp'));
const fs = require('fs'); const linhas = JSON.parse(fs.readFileSync(__dirname + '/linhas_codigo.json', 'utf8'));
const r = {}; for (const l of linhas) r[l] = hljs.highlight(l, { language: 'csharp' }).value;
fs.writeFileSync(__dirname + '/codigo_realce.json', JSON.stringify(r)); console.log(Object.keys(r).length, 'linhas');
