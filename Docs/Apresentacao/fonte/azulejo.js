// Azulejo português (inspirado nos casarões do Centro Histórico de São Luís).
// A mesma fórmula está em Assets/Scripts/Editor/SebraelabCulturaSaoLuis.cs (textura do jogo).
export const CORES = { esmalte: [243, 239, 228], cobalto: [31, 60, 136], claro: [111, 143, 208], rejunte: [214, 209, 197] };
export function amostra(x, y) { // x, y em [0,1): 0 esmalte, 1 cobalto, 2 azul claro, 3 rejunte
  const cx = x - 0.5, cy = y - 0.5, r = Math.hypot(cx, cy), th = Math.atan2(cy, cx);
  const dx = Math.min(x, 1 - x), dy = Math.min(y, 1 - y), dc = Math.hypot(dx, dy);
  if (dx < 0.006 || dy < 0.006) return 3;
  let c = 0;
  // cantos: um quarto de flor em cada canto (forma uma flor inteira entre 4 azulejos)
  const ac = Math.atan2(dy, dx), petC = 0.2 * (0.55 + 0.45 * Math.pow(Math.abs(Math.cos(2 * ac)), 0.8));
  if (dc < petC) c = 1;
  if (dc < 0.085) c = 0;
  if (dc < 0.045) c = 2;
  if (Math.abs(dc - 0.245) < 0.011) c = 2;
  // meio das bordas: folhas
  const e = [[cx, 0.5 - Math.abs(cy)], [cy, 0.5 - Math.abs(cx)]];
  for (const [u, v] of e) { if ((u / 0.075) ** 2 + (v / 0.06) ** 2 < 1) c = 1; if ((u / 0.035) ** 2 + (v / 0.028) ** 2 < 1) c = 0; }
  // diagonais finas ligando a roseta aos cantos
  if (Math.abs(Math.abs(cx) - Math.abs(cy)) < 0.009 * Math.SQRT2 && r > 0.33 && dc > 0.262) c = 2;
  // roseta central de 8 pétalas
  const pet = 0.27 * (0.42 + 0.58 * Math.pow(Math.abs(Math.cos(4 * th)), 0.75));
  if (Math.abs(r - 0.315) < 0.012) c = 2;
  if (r < pet) c = 1;
  const pet2 = 0.17 * (0.45 + 0.55 * Math.pow(Math.abs(Math.cos(4 * th)), 1.2));
  if (r < pet2 && r > 0.02) c = (r < pet2 - 0.022) ? 2 : 0;
  if (r < 0.075) c = 0;
  if (r < 0.042) c = 1;
  return c;
}
export function desenharAzulejo(canvas, n = 256, ss = 3) {
  canvas.width = canvas.height = n; const g = canvas.getContext('2d'), img = g.createImageData(n, n);
  const pal = [CORES.esmalte, CORES.cobalto, CORES.claro, CORES.rejunte];
  for (let j = 0; j < n; j++) for (let i = 0; i < n; i++) {
    let R = 0, G = 0, B = 0;
    for (let a = 0; a < ss; a++) for (let b = 0; b < ss; b++) { const p = pal[amostra((i + (a + 0.5) / ss) / n, (j + (b + 0.5) / ss) / n)]; R += p[0]; G += p[1]; B += p[2]; }
    const k = (j * n + i) * 4, q = ss * ss; img.data[k] = R / q; img.data[k + 1] = G / q; img.data[k + 2] = B / q; img.data[k + 3] = 255;
  }
  g.putImageData(img, 0, 0); return canvas;
}
