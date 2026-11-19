// Vídeo v2.  node render2.js previa <cena> <t1> [t2...]      -> PNGs em v2/previa/
//            node render2.js trecho <cena> <q0> <q1> <saida.mp4> -> quadros [q0,q1) codificados em H.264
const { chromium } = require('playwright');
const { spawn } = require('child_process');
const fs = require('fs'), path = require('path');
const V2 = path.resolve(__dirname, '../../v2');
const linha = JSON.parse(fs.readFileSync(path.join(V2, 'linha_do_tempo.json'), 'utf8'));
const FPS = linha.fps;
const PAGINA = { '3d': 'jogo.html', unity: 'slides2.html', slide: 'slides2.html' };

function montar(arquivo) { // troca {{icone:nome}} pelo SVG do lucide e {{logo:nome}} pelo do simple-icons
  let html = fs.readFileSync(path.join(__dirname, arquivo), 'utf8');
  html = html.replace(/\{\{icone:([a-z0-9-]+)\}\}/g, (_, n) => fs.readFileSync(path.join(__dirname, 'node_modules/lucide-static/icons', n + '.svg'), 'utf8')
    .replace(/<!--[\s\S]*?-->/g, '').replace(/class="[^"]*"/, ''));
  html = html.replace(/\{\{logo:([a-z0-9-]+)\}\}/g, (_, n) => fs.readFileSync(path.join(__dirname, 'node_modules/simple-icons/icons', n + '.svg'), 'utf8')
    .replace('<svg ', '<svg fill="currentColor" '));
  const saida = '_' + arquivo; fs.writeFileSync(path.join(__dirname, saida), html);
  return 'http://127.0.0.1:8765/' + saida;
}

(async () => {
  const [modo, id, ...resto] = process.argv.slice(2);
  const cena = linha.cenas.find(c => c.id === id);
  if (!cena) throw new Error('cena desconhecida: ' + id);
  const navegador = await chromium.launch({ args: ['--use-angle=swiftshader', '--enable-unsafe-swiftshader'] });
  const pagina = await navegador.newPage({ viewport: { width: 1920, height: 1080 } });
  pagina.on('pageerror', e => console.error('erro na página:', e.message));
  pagina.on('console', m => { if (m.type() === 'error' || m.type() === 'warning') console.error('console:', m.text()); });
  await pagina.goto(montar(PAGINA[cena.tipo]));
  await pagina.waitForFunction(() => window.pronto === true, null, { timeout: 180000 });
  await pagina.evaluate(() => document.fonts.ready);
  const desenhar = t => pagina.evaluate(([c, t]) => window.desenhar(c, t), [cena, t]);

  if (modo === 'previa') {
    const dir = path.join(V2, 'previa'); fs.mkdirSync(dir, { recursive: true });
    for (const t of resto.map(Number)) {
      await desenhar(t);
      await pagina.screenshot({ path: path.join(dir, `${id}_${t.toFixed(1)}.png`) });
    }
  } else {
    const [q0, q1, saida] = [+resto[0], +resto[1], resto[2]];
    const ff = spawn('ffmpeg', ['-y', '-loglevel', 'error', '-f', 'image2pipe', '-c:v', 'mjpeg', '-framerate', String(FPS), '-i', '-',
      '-c:v', 'libx264', '-preset', 'medium', '-crf', '15', '-pix_fmt', 'yuv420p', '-r', String(FPS), saida], { stdio: ['pipe', 'inherit', 'inherit'] });
    const t0 = Date.now();
    for (let q = q0; q < q1; q++) {
      await desenhar(q / FPS);
      const img = await pagina.screenshot({ type: 'jpeg', quality: 94 });
      if (!ff.stdin.write(img)) await new Promise(r => ff.stdin.once('drain', r));
    }
    ff.stdin.end(); await new Promise(r => ff.on('close', r));
    console.log(`${id} [${q0},${q1}) em ${((Date.now() - t0) / 1000).toFixed(0)} s`);
  }
  await navegador.close();
})().catch(e => { console.error(e); process.exit(1); });
