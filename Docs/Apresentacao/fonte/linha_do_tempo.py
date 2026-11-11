"""Monta a linha do tempo (início de cada fala) e a trilha de narração completa."""
import json, os, sys
import numpy as np, soundfile as sf

V = sys.argv[1]
roteiro = json.load(open(os.path.join(V, "roteiro.json"), encoding="utf8"))
tempos = json.load(open(os.path.join(V, "tempos.json")))
SR = 24000
ENTRADA, PAUSA, SAIDA = 0.7, 0.45, 1.1

cenas, faixa, inicio_global = [], [], 0.0
for cena in roteiro["cenas"]:
    entrada = 1.2 if cena.get("tresD") else ENTRADA
    t, segs = entrada, []
    audio = [np.zeros(int(entrada * SR), np.float32)]
    for seg, info in zip(cena["segmentos"], tempos[cena["id"]]):
        a, sr = sf.read(os.path.join(V, "audio", info["arquivo"]), dtype="float32")
        assert sr == SR
        segs.append({"inicio": round(t, 4), "fim": round(t + len(a) / SR, 4), "passo": seg["passo"], "legenda": seg["legenda"]})
        audio += [a, np.zeros(int(PAUSA * SR), np.float32)]
        t += len(a) / SR + PAUSA
    duracao = t - PAUSA + SAIDA
    bloco = np.concatenate(audio)
    alvo = int(round(duracao * 30)) * (SR // 30)  # número inteiro de quadros (30 fps)
    bloco = np.pad(bloco, (0, max(0, alvo - len(bloco))))[:alvo]
    faixa.append(bloco)
    cenas.append({"id": cena["id"], "tresD": bool(cena.get("tresD")), "inicioGlobal": round(inicio_global, 4),
                  "duracao": round(alvo / SR, 4), "segmentos": segs})
    inicio_global += alvo / SR

total = np.concatenate(faixa)
# normaliza com folga
pico = np.max(np.abs(total)); total = total / pico * 0.89 if pico > 0 else total
sf.write(os.path.join(V, "narracao.wav"), total, SR)
json.dump({"fps": 30, "cenas": cenas}, open(os.path.join(V, "linha_do_tempo.json"), "w", encoding="utf8"), ensure_ascii=False, indent=1)
print("duração total: %.1f s (%d:%02d)" % (len(total) / SR, len(total) // SR // 60, len(total) // SR % 60))
