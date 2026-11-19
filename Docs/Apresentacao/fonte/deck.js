// Slides avulsos (sem legenda): captura o estado final de cada cena e junta num PDF 1920x1080.
const { chromium } = require('playwright'); const fs = require('fs'), path = require('path');
const linha = JSON.parse(fs.readFileSync(path.resolve(__dirname, '../../v2/linha_do_tempo.json'), 'utf8'));
const cena = id => linha.cenas.find(c => c.id === id);
function montar(arquivo) { // mesma troca de ícones do render2.js, num arquivo próprio
  let html = fs.readFileSync(path.join(__dirname, arquivo), 'utf8');
  html = html.replace(/\{\{icone:([a-z0-9-]+)\}\}/g, (_, n) => fs.readFileSync(path.join(__dirname, 'node_modules/lucide-static/icons', n + '.svg'), 'utf8').replace(/<!--[\s\S]*?-->/g, '').replace(/class="[^"]*"/, ''));
  html = html.replace(/\{\{logo:([a-z0-9-]+)\}\}/g, (_, n) => fs.readFileSync(path.join(__dirname, 'node_modules/simple-icons/icons', n + '.svg'), 'utf8').replace('<svg ', '<svg fill="currentColor" '));
  fs.writeFileSync(path.join(__dirname, '_d_' + arquivo), html); return 'http://127.0.0.1:8765/_d_' + arquivo;
}
const LISTA = [
  ['jogo', 'abertura', 19.5], ['slides', 'proposta', 17.3], ['slides', 'pilares', 15.6], ['slides', 'sao_luis', 19.0], ['slides', 'sao_luis', 27.0],
  ['slides', 'tecnologias', 23.8], ['jogo', 'cenario', 9.0], ['jogo', 'cenario', 16.0], ['jogo', 'jogo_vr', 19.5], ['jogo', 'jogo_vr', 27.0],
  ['jogo', 'jogo_pc', 13.5], ['slides', 'unity_projeto', 17.0], ['slides', 'unity_inspector', 6.7], ['slides', 'unity_inspector', 14.8],
  ['slides', 'codigo', 12.2], ['slides', 'automacao', 11.5], ['slides', 'automacao', 19.8], ['slides', 'arquitetura', 10.4],
  ['slides', 'processo', 15.2], ['slides', 'roadmap', 17.2], ['jogo', 'encerramento', 10.3],
];
(async () => {
  const b = await chromium.launch({ args: ['--use-angle=swiftshader', '--enable-unsafe-swiftshader'] });
  const pags = {};
  for (const tipo of ['jogo', 'slides']) {
    const p = await b.newPage({ viewport: { width: 1920, height: 1080 } }); p.on('pageerror', e => console.error('erro:', e.message));
    await p.goto(montar(tipo === 'jogo' ? 'jogo.html' : 'slides2.html')); await p.waitForFunction(() => window.pronto === true, null, { timeout: 180000 });
    await p.evaluate(() => document.fonts.ready); pags[tipo] = p;
  }
  const arqs = [];
  for (const [i, [tipo, id, t]] of LISTA.entries()) {
    const p = pags[tipo]; await p.evaluate(([c, t]) => window.desenhar(c, t), [{ ...cena(id), deck: true }, t]);
    const arq = `deck/s${String(i + 1).padStart(2, '0')}_${id}.png`; await p.screenshot({ path: path.join(__dirname, arq) }); arqs.push(arq); console.log(arq);
  }
  const html = `<!doctype html><html><head><style>@page{size:1920px 1080px;margin:0}html,body{margin:0}img{display:block;width:1920px;height:1080px;page-break-after:always}</style></head><body>${arqs.map(a => `<img src="${a}">`).join('')}</body></html>`;
  fs.writeFileSync(path.join(__dirname, 'deck.html'), html);
  const p = await b.newPage(); await p.goto('http://127.0.0.1:8765/deck.html'); await p.waitForLoadState('networkidle');
  await p.pdf({ path: process.argv[2], width: '1920px', height: '1080px', printBackground: true, pageRanges: `1-${arqs.length}` });
  await b.close(); console.log('PDF ok');
})().catch(e => { console.error(e); process.exit(1); });
