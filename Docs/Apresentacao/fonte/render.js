// Uso: node render.js previa            -> um PNG por cena (estado final) em previa/
//      node render.js quadros [ids...]  -> quadros + lista de concatenação do ffmpeg
const { chromium } = require('playwright');
const fs = require('fs'), path = require('path');
const V = path.resolve(__dirname, '..');
const linha = JSON.parse(fs.readFileSync(path.join(V, 'linha_do_tempo.json'), 'utf8'));
const FPS = linha.fps, FADE = 0.35, DUR = 0.55;

function montar(arquivo) {
  // troca {{icone:nome}} pelo SVG do lucide
  let html = fs.readFileSync(path.join(__dirname, arquivo), 'utf8');
  html = html.replace(/\{\{icone:([a-z0-9-]+)\}\}/g, (_, nome) =>
    fs.readFileSync(path.join(__dirname, 'node_modules/lucide-static/icons', nome + '.svg'), 'utf8')
      .replace(/<!--[\s\S]*?-->/g, '').replace(/class="[^"]*"/, ''));
  const saida = path.join(__dirname, '_' + arquivo);
  fs.writeFileSync(saida, html);
  return 'file://' + saida;
}

function quadrosDaCena(cena) {
  const ini = Math.round(cena.inicioGlobal * FPS), fim = Math.round((cena.inicioGlobal + cena.duracao) * FPS);
  const n = fim - ini, set = new Set([0]);
  if (cena.tresD) { for (let i = 0; i < n; i++) set.add(i); }
  else {
    const add = t => { const i = Math.round(t * FPS); if (i >= 0 && i < n) set.add(i); };
    for (let k = 0; k <= Math.ceil(FADE * FPS) + 1; k++) { add(k / FPS); add(cena.duracao - k / FPS); }
    const inicios = [0.12, ...cena.segmentos.map(s => s.inicio)];
    for (const s of inicios) for (let k = 0; k <= Math.ceil(DUR * FPS) + 1; k++) add(s + k / FPS);
  }
  return { ini, n, indices: [...set].sort((a, b) => a - b) };
}

(async () => {
  const modo = process.argv[2] || 'previa';
  const filtro = process.argv.slice(3);
  const navegador = await chromium.launch({ args: ['--use-angle=swiftshader', '--enable-unsafe-swiftshader', '--allow-file-access-from-files'] });
  const pagina = await navegador.newPage({ viewport: { width: 1920, height: 1080 } });
  pagina.on('pageerror', e => console.error('erro na página:', e.message));
  const urlSlides = montar('slides.html'), url3D = fs.existsSync(path.join(__dirname, 'cena3d.html')) ? montar('cena3d.html') : null;
  let carregada = null;
  const abrir = async url => { if (carregada !== url) { await pagina.goto(url); await pagina.waitForFunction(() => window.prontoSlides === true || window.pronto3D === true, null, { timeout: 60000 }); await pagina.evaluate(() => document.fonts.ready); carregada = url; } };

  const dirPrevia = path.join(V, 'previa'), dirQuadros = path.join(V, 'quadros');
  fs.mkdirSync(dirPrevia, { recursive: true }); fs.mkdirSync(dirQuadros, { recursive: true });

  for (const cena of linha.cenas) {
    if (filtro.length && !filtro.includes(cena.id)) continue;
    await abrir(cena.tresD ? url3D : urlSlides);
    if (modo === 'previa') {
      const ts = cena.tresD ? [cena.segmentos[0].inicio + 2, cena.segmentos[1].inicio + 1, cena.duracao - 0.6] : [cena.duracao - FADE - 0.2];
      for (const [j, t] of ts.entries()) {
        await pagina.evaluate(([c, t]) => window.desenhar(c, t), [cena, t]);
        await pagina.screenshot({ path: path.join(dirPrevia, `${String(linha.cenas.indexOf(cena) + 1).padStart(2, '0')}_${cena.id}_${j}.png`) });
      }
      console.log('prévia', cena.id);
      continue;
    }
    const { ini, n, indices } = quadrosDaCena(cena);
    const dir = path.join(dirQuadros, cena.id); fs.rmSync(dir, { recursive: true, force: true }); fs.mkdirSync(dir);
    const lista = [];
    const t0 = Date.now();
    for (let j = 0; j < indices.length; j++) {
      const i = indices[j], prox = j + 1 < indices.length ? indices[j + 1] : n;
      await pagina.evaluate(([c, t]) => window.desenhar(c, t), [cena, i / FPS]);
      const arq = path.join(dir, `q${String(i).padStart(5, '0')}.png`);
      await pagina.screenshot({ path: arq });
      lista.push(`file '${arq}'\nduration ${((prox - i) / FPS).toFixed(6)}`);
    }
    fs.writeFileSync(path.join(dir, 'lista.txt'), lista.join('\n') + `\nfile '${path.join(dir, `q${String(indices[indices.length - 1]).padStart(5, '0')}.png`)}'\n`);
    fs.writeFileSync(path.join(dir, 'info.json'), JSON.stringify({ ini, n }));
    console.log(`quadros ${cena.id}: ${indices.length} imagens para ${n} frames (${((Date.now() - t0) / 1000).toFixed(0)} s)`);
  }
  await navegador.close();
})().catch(e => { console.error(e); process.exit(1); });
