"""Narração com a voz Piper pt-BR (faber) + conferência automática: cada fala é transcrita pelo Whisper e comparada à legenda."""
import sys, os, re, json, unicodedata
import numpy as np, soundfile as sf, sherpa_onnx

V2, T = sys.argv[1], sys.argv[2]
somente = set(a.split(":")[0] for a in sys.argv[3:])
so_segmento = {(a.split(":")[0], int(a.split(":")[1])) for a in sys.argv[3:] if ":" in a}  # ex.: jogo_vr:3
M = os.path.join(T, "modelos")
r = json.load(open(os.path.join(V2, "roteiro.json"), encoding="utf8"))
d = os.path.join(M, f"vits-piper-pt_BR-{r['voz']}-medium")
tts = sherpa_onnx.OfflineTts(sherpa_onnx.OfflineTtsConfig(model=sherpa_onnx.OfflineTtsModelConfig(
    vits=sherpa_onnx.OfflineTtsVitsModelConfig(model=os.path.join(d, f"pt_BR-{r['voz']}-medium.onnx"),
        tokens=os.path.join(d, "tokens.txt"), data_dir=os.path.join(d, "espeak-ng-data"), noise_scale=0.667, noise_scale_w=0.8),
    num_threads=4)))
rec = sherpa_onnx.OfflineRecognizer.from_whisper(encoder=os.path.join(M, "sherpa-onnx-whisper-small/small-encoder.int8.onnx"),
    decoder=os.path.join(M, "sherpa-onnx-whisper-small/small-decoder.int8.onnx"), tokens=os.path.join(M, "sherpa-onnx-whisper-small/small-tokens.txt"),
    language="pt", task="transcribe", num_threads=4)

def norm(s):
    s = unicodedata.normalize("NFKD", s.lower()); s = "".join(c for c in s if not unicodedata.combining(c))
    s = s.replace("c#", "c sharp").replace("3d", "3 d")
    return re.sub(r"[^a-z0-9 ]", " ", re.sub(r"\([^)]*\)", " ", s)).split()
def wer(ref, hyp):
    a, b = norm(ref), norm(hyp); dp = list(range(len(b) + 1))
    for i in range(1, len(a) + 1):
        prev, dp[0] = dp[0], i
        for j in range(1, len(b) + 1):
            cur = min(dp[j] + 1, dp[j - 1] + 1, prev + (a[i - 1] != b[j - 1])); prev, dp[j] = dp[j], cur
    return dp[len(b)] / max(1, len(a))

tempos_path = os.path.join(V2, "tempos.json")
tempos = json.load(open(tempos_path)) if os.path.exists(tempos_path) else {}
relatorio = []
for cena in r["cenas"]:
    if somente and cena["id"] not in somente: continue
    lista = []
    for i, seg in enumerate(cena["segmentos"]):
        if any(c == cena["id"] for c, _ in so_segmento) and (cena["id"], i) not in so_segmento:
            lista.append(tempos[cena["id"]][i]); continue  # mantém o áudio que já foi aprovado
        a = tts.generate(seg["fala"], sid=0, speed=r["velocidade"])
        x = np.array(a.samples, dtype=np.float32); sr = a.sample_rate
        idx = np.where(np.abs(x) > 0.006)[0]
        if len(idx): x = x[max(0, idx[0] - int(0.04 * sr)): idx[-1] + int(0.25 * sr)]
        nome = f"{cena['id']}_{i:02d}.wav"; sf.write(os.path.join(V2, "audio", nome), x, sr)
        s = rec.create_stream(); s.accept_waveform(sr, x); rec.decode_stream(s); hip = s.result.text.strip()
        e = wer(seg["legenda"], hip)
        lista.append({"arquivo": nome, "duracao": len(x) / sr, "sr": sr})
        relatorio.append((e, cena["id"], i, hip))
    tempos[cena["id"]] = lista
json.dump(tempos, open(tempos_path, "w"), indent=1)
print("WER medio: %.3f" % (sum(e for e, *_ in relatorio) / len(relatorio)))
for e, c, i, hip in sorted(relatorio, reverse=True)[:8]:
    print(f"{e:.2f} {c}[{i}] -> {hip}")
total = sum(s["duracao"] for c in tempos.values() for s in c)
print("fala total: %.1f s" % total)
