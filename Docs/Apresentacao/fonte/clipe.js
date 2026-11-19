// Animação "Azulejos de São Luís" (6 s, em loop perfeito): é o vídeo que o jogo toca no painel dos livros
// (Assets/CulturaSaoLuis/AzulejosSaoLuis.mp4) e o mesmo desenho aparece na simulação do vídeo de apresentação.
export const DURACAO_CLIPE = 6;
export function desenharClipe(g, w, h, t, azulejo) {
  const p = (t % DURACAO_CLIPE) / DURACAO_CLIPE, lado = h / 4; // 4 azulejos na altura
  g.save();
  g.fillStyle = '#f3efe4'; g.fillRect(0, 0, w, h);
  const ox = -p * lado, oy = -p * lado * 0; // desliza exatamente 1 azulejo por volta
  for (let y = -1; y * lado < h + lado; y++) for (let x = -1; x * lado < w + lado * 2; x++) g.drawImage(azulejo, ox + x * lado, oy + y * lado, lado + 0.6, lado + 0.6);
  // brilho do vidrado passando
  const cx = (-0.45 + 1.9 * p) * w, gr = g.createLinearGradient(cx - w * 0.22, 0, cx + w * 0.22, h * 0.35);
  gr.addColorStop(0, 'rgba(255,255,255,0)'); gr.addColorStop(0.5, 'rgba(255,255,255,0.42)'); gr.addColorStop(1, 'rgba(255,255,255,0)');
  g.fillStyle = gr; g.fillRect(0, 0, w, h);
  // vinheta
  const v = g.createRadialGradient(w / 2, h / 2, h * 0.35, w / 2, h / 2, w * 0.7);
  v.addColorStop(0, 'rgba(10,20,50,0)'); v.addColorStop(1, 'rgba(10,20,50,0.38)'); g.fillStyle = v; g.fillRect(0, 0, w, h);
  // legenda
  const s = w / 640, bx = 22 * s, by = h - 96 * s, bw = 404 * s, bh = 74 * s;
  g.fillStyle = 'rgba(16,30,70,0.86)'; g.beginPath(); g.roundRect(bx, by, bw, bh, 12 * s); g.fill();
  g.fillStyle = '#ffffff'; g.font = `800 ${27 * s}px Inter`; g.textBaseline = 'alphabetic'; g.fillText('São Luís do Maranhão', bx + 18 * s, by + 33 * s);
  g.fillStyle = '#c9d6f5'; g.font = `500 ${15.5 * s}px Inter`; g.fillText('Azulejos do Centro Histórico · Patrimônio Mundial', bx + 18 * s, by + 58 * s);
  g.restore();
}
