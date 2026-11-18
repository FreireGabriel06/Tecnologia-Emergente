// Gera as imagens do modelo 3D usadas nos slides (pasta img/).
const { chromium } = require('playwright'); const fs = require('fs'), path = require('path');
const FOTOS = {
  estante:  { camera: [-14.35, 1.5, 0.95], alvo: [-16.55, 1.0, 0.18], painel: true, fov: 50 },
  estante2: { camera: [-13.9, 1.45, 1.5], alvo: [-16.6, 0.95, 0.1], painel: false, fov: 50 },
  salao:    { camera: [-7.9, 2.15, 12.8], alvo: [-15.0, 0.7, 4.5], fov: 58, luz: 0 },
  salao2:   { camera: [-8.3, 2.2, -1.2], alvo: [-15.5, 0.6, 6.0], fov: 60, luz: 1 },
  visao1: { camera: [-9.2, 1.85, 7.0], alvo: [-15.8, 0.8, 0.8], fov: 56, luz: 1 },
  visao2: { camera: [-10.6, 1.7, 3.6], alvo: [-16.6, 0.9, 0.3], fov: 58, luz: 1 },
  visao3: { camera: [-8.6, 2.0, 10.5], alvo: [-16.0, 0.6, 3.0], fov: 52, luz: 1 },
  editor:   { camera: [-8.0, 2.4, 6.5], alvo: [-15.0, 0.4, 0.5], fov: 55, luz: 1 },
};
(async () => {
  const b = await chromium.launch({ args: ['--use-angle=swiftshader', '--enable-unsafe-swiftshader'] });
  const p = await b.newPage({ viewport: { width: 1920, height: 1080 } });
  p.on('pageerror', e => console.error('erro:', e.message));
  await p.goto('http://127.0.0.1:8765/_jogo.html'); await p.waitForFunction(() => window.pronto === true, null, { timeout: 180000 });
  for (const [nome, f] of Object.entries(FOTOS)) {
    if (process.argv[2] && !process.argv.slice(2).includes(nome)) continue;
    await p.evaluate(d => window.desenhar(d, 10), { modo: 'foto', limpo: true, duracao: 99, segmentos: [], ...f });
    await p.screenshot({ path: path.join(__dirname, 'img', nome + '.jpg'), type: 'jpeg', quality: 92 });
    console.log('foto', nome);
  }
  await b.close();
})();
