"""Gera um .wav por segmento do roteiro (Kokoro, pt-br) e um tempos.json com as durações."""
import json, sys, os
import soundfile as sf
import numpy as np
from kokoro_onnx import Kokoro

V = sys.argv[1]
T = os.path.join(V, "tts")
roteiro = json.load(open(os.path.join(V, "roteiro.json"), encoding="utf8"))
k = Kokoro(os.path.join(T, "kokoro-v1.0.onnx"), os.path.join(T, "voices-v1.0.bin"))
saida = os.path.join(V, "audio"); os.makedirs(saida, exist_ok=True)
modo = sys.argv[2] if len(sys.argv) > 2 else "audio"

tempos = {}
for cena in roteiro["cenas"]:
    lista = []
    for i, seg in enumerate(cena["segmentos"]):
        if modo == "fonemas":
            print(cena["id"], i, "|", k.tokenizer.phonemize(seg["fala"], "pt-br"))
            continue
        audio, sr = k.create(seg["fala"], voice=roteiro["voz"], speed=roteiro["velocidade"], lang="pt-br")
        audio = np.asarray(audio, dtype=np.float32)
        # corta silêncio do começo/fim
        limiar = 0.01
        idx = np.where(np.abs(audio) > limiar)[0]
        if len(idx): audio = audio[max(0, idx[0] - int(0.03 * sr)): idx[-1] + int(0.08 * sr)]
        nome = f"{cena['id']}_{i:02d}.wav"
        sf.write(os.path.join(saida, nome), audio, sr)
        lista.append({"arquivo": nome, "duracao": len(audio) / sr})
    tempos[cena["id"]] = lista
if modo != "fonemas":
    json.dump(tempos, open(os.path.join(V, "tempos.json"), "w"), indent=1)
    total = sum(s["duracao"] for c in tempos.values() for s in c)
    print("fala total (s):", round(total, 1))
