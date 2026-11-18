const { chromium } = require('playwright'); const { spawn } = require('child_process');
(async () => {
  const saida = process.argv[2];
  const b = await chromium.launch(); const p = await b.newPage({ viewport: { width: 640, height: 360 } });
  await p.goto('http://127.0.0.1:8765/gerar_clipe.html'); await p.waitForFunction(() => window.ok === true);
  const ff = spawn('ffmpeg', ['-y', '-loglevel', 'error', '-f', 'image2pipe', '-c:v', 'png', '-framerate', '30', '-i', '-', '-c:v', 'libx264', '-profile:v', 'main', '-preset', 'slow', '-crf', '20', '-pix_fmt', 'yuv420p', '-movflags', '+faststart', '-an', saida], { stdio: ['pipe', 'inherit', 'inherit'] });
  for (let i = 0; i < 180; i++) { await p.evaluate(t => window.quadro(t), i / 30); ff.stdin.write(await p.locator('#c').screenshot({ type: 'png' })); }
  ff.stdin.end(); await new Promise(r => ff.on('close', r)); await b.close();
})();
