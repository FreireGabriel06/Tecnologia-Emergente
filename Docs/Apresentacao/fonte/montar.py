"""Junta os quadros de todas as cenas e a narração num MP4 (1080p, 30 fps, H.264 + AAC)."""
import json, os, subprocess, sys

V = sys.argv[1]
saida = sys.argv[2]
linha = json.load(open(os.path.join(V, "linha_do_tempo.json"), encoding="utf8"))
FPS = linha["fps"]
entradas, total_quadros = [], 0
for cena in linha["cenas"]:
    d = os.path.join(V, "quadros", cena["id"])
    info = json.load(open(os.path.join(d, "info.json")))
    linhas = open(os.path.join(d, "lista.txt"), encoding="utf8").read().strip().split("\n")
    pares = []
    for i in range(0, len(linhas) - 1, 2):
        arq = linhas[i][len("file '"):-1]
        dur = float(linhas[i + 1].split()[1])
        pares.append((arq, dur))
    n = int(round(cena["duracao"] * FPS))
    falta = n / FPS - sum(p[1] for p in pares)
    assert abs(falta) < 3.0 / FPS, (cena["id"], falta)
    pares[-1] = (pares[-1][0], max(1.0 / FPS, pares[-1][1] + falta))
    entradas += pares
    total_quadros += n

lista = os.path.join(V, "lista_total.txt")
with open(lista, "w", encoding="utf8") as f:
    for arq, dur in entradas:
        f.write(f"file '{arq}'\nduration {dur:.6f}\n")
    f.write(f"file '{entradas[-1][0]}'\n")

cmd = ["ffmpeg", "-y", "-hide_banner", "-loglevel", "error",
       "-f", "concat", "-safe", "0", "-i", lista, "-i", os.path.join(V, "narracao.wav"),
       "-vf", f"fps={FPS},format=yuv420p", "-frames:v", str(total_quadros),
       "-c:v", "libx264", "-preset", "slow", "-crf", "20", "-movflags", "+faststart",
       "-c:a", "aac", "-b:a", "160k", "-ar", "48000", "-af", "apad", "-shortest", saida]
print(" ".join(cmd[:6]), "...")
subprocess.run(cmd, check=True)
print("quadros:", total_quadros, "duração esperada: %.2f s" % (total_quadros / FPS))
