"""Divide cada cena em trechos e renderiza 4 em paralelo (node render2.js trecho ...)."""
import json, os, subprocess, sys
from concurrent.futures import ThreadPoolExecutor
V2 = sys.argv[1]; WEB = sys.argv[2]; somente = set(sys.argv[3:])
l = json.load(open(os.path.join(V2, "linha_do_tempo.json"), encoding="utf8"))
jobs = []
for c in l["cenas"]:
    if somente and c["id"] not in somente: continue
    passo = 240 if c["tipo"] == "3d" else 600
    for q0 in range(0, c["quadros"], passo):
        q1 = min(c["quadros"], q0 + passo)
        jobs.append((c["id"], q0, q1, os.path.join(V2, "trechos", f"{c['id']}_{q0:05d}.mp4")))
jobs.sort(key=lambda j: -(j[2] - j[1]) * (3 if j[0] in ("abertura", "cenario", "jogo_vr", "jogo_pc", "encerramento") else 1))
def roda(j):
    r = subprocess.run(["node", os.path.join(WEB, "render2.js"), "trecho", j[0], str(j[1]), str(j[2]), j[3]], capture_output=True, text=True, cwd=WEB)
    msg = (r.stdout + r.stderr).strip().splitlines()
    print(j[0], j[1], j[2], "OK" if r.returncode == 0 else "FALHOU", " | ".join(m for m in msg if "404" not in m)[-300:], flush=True)
    return r.returncode
with ThreadPoolExecutor(4) as ex: res = list(ex.map(roda, jobs))
print("FIM", "falhas:", sum(1 for r in res if r))
