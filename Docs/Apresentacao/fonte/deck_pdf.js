const { chromium } = require('playwright'); const fs = require('fs'), path = require('path');
(async () => {
  const imgs = fs.readdirSync(path.join(__dirname, 'deck')).filter(f => /^s\d+_.*\.jpg$/.test(f)).sort();
  fs.writeFileSync(path.join(__dirname, 'deck_jpg.html'), `<!doctype html><html><head><style>@page{size:1920px 1080px;margin:0}html,body{margin:0}img{display:block;width:1920px;height:1080px;page-break-after:always}</style></head><body>${imgs.map(a => `<img src="deck/${a}">`).join('')}</body></html>`);
  const b = await chromium.launch(); const p = await b.newPage();
  await p.goto('http://127.0.0.1:8765/deck_jpg.html'); await p.waitForLoadState('networkidle');
  await p.pdf({ path: process.argv[2], width: '1920px', height: '1080px', printBackground: true, pageRanges: `1-${imgs.length}` });
  await b.close(); console.log(imgs.length, 'páginas');
})();
