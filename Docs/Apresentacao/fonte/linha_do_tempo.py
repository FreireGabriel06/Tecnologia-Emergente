"""Linha do tempo v2: início de cada fala por cena (quadros inteiros a 30 fps) e a trilha de narração seca."""
import json, os, sys
import numpy as np, soundfile as sf

V2 = sys.argv[1]
r = json.load(open(os.path.join(V2, "roteiro.json"), encoding="utf8"))
tempos = json.load(open(os.path.join(V2, "tempos.json")))
SR, FPS = 22050, 30
PADRAO = {"entrada": 0.6, "pausa": 0.45, "saida": 0.9}
AJUSTES = {  # respiros para a imagem acompanhar a fala
    "abertura": {"entrada": 2.2, "pausas": {2: 1.0}, "saida": 1.0},
    "cenario": {"entrada": 1.0, "pausas": {1: 0.8}, "saida": 1.6},
    "jogo_vr": {"entrada": 0.9, "pausas": {1: 0.5, 2: 1.4, 3: 0.6, 4: 1.0, 5: 0.9}, "saida": 1.8},
    "jogo_pc": {"entrada": 0.8, "pausas": {1: 1.6}, "saida": 1.5},
    "sao_luis": {"entrada": 0.8, "saida": 1.2},
    "encerramento": {"entrada": 1.0, "pausas": {1: 0.8}, "saida": 3.2},
}
cenas, faixa, inicio = [], [], 0
for cena in r["cenas"]:
    aj = {**PADRAO, **AJUSTES.get(cena["id"], {})}
    t, segs, pedacos = aj["entrada"], [], [np.zeros(int(aj["entrada"] * SR), np.float32)]
    for i, (seg, info) in enumerate(zip(cena["segmentos"], tempos[cena["id"]])):
        pausa = aj.get("pausas", {}).get(i, aj["pausa"])
        if i > 0:
            pedacos.append(np.zeros(int(pausa * SR), np.float32)); t += pausa
        a, sr = sf.read(os.path.join(V2, "audio", info["arquivo"]), dtype="float32"); assert sr == SR
        segs.append({"inicio": round(t, 4), "fim": round(t + len(a) / SR, 4), "passo": seg["passo"], "legenda": seg["legenda"]})
        pedacos.append(a); t += len(a) / SR
    n = int(round((t + aj["saida"]) * FPS))
    bloco = np.concatenate(pedacos); alvo = n * SR // FPS
    faixa.append(np.pad(bloco, (0, max(0, alvo - len(bloco))))[:alvo])
    cenas.append({"id": cena["id"], "tipo": cena["tipo"], "modo": cena.get("modo", cena["id"]), "inicioGlobal": round(inicio / FPS, 4),
                  "quadros": n, "duracao": round(n / FPS, 4), "segmentos": segs})
    inicio += n
total = np.concatenate(faixa)
sf.write(os.path.join(V2, "narracao_seca.wav"), total, SR)
json.dump({"fps": FPS, "cenas": cenas}, open(os.path.join(V2, "linha_do_tempo.json"), "w", encoding="utf8"), ensure_ascii=False, indent=1)
for c in cenas: print(f"{c['id']:16s} {c['duracao']:6.1f} s")
print("total: %.1f s (%d:%02d)" % (inicio / FPS, inicio // FPS // 60, inicio // FPS % 60))
