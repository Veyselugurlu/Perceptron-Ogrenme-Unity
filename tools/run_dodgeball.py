"""
Runs the DodgeBall build headless with automatic throws (seeds 1-30, 40 throws each) and writes
docs/week08/data/dodgeball_throws.csv (one row per throw). Seed 1 is run twice to check that a run
is reproducible.

  python tools/run_dodgeball.py      (build: Perceptron > Build DodgeBall Player)
"""
import shutil
import subprocess
import tempfile
from concurrent.futures import ThreadPoolExecutor
from pathlib import Path

EXE = Path("Builds/DodgeBall/DodgeBall.exe")
OUT = Path("docs/week08/data")
SEEDS = list(range(1, 31))
THROWS = 40


def run(seed, tmp, tag=""):
    csv = tmp / f"seed{seed}{tag}.csv"
    subprocess.run([str(EXE), "-batchmode", "-nographics", "-autoThrow", "-seed", str(seed),
                    "-throws", str(THROWS), "-experiment", str(csv),
                    "-logFile", str(tmp / f"seed{seed}{tag}.log")], check=True, timeout=600)
    return csv


def main():
    if not EXE.exists():
        raise SystemExit(f"build not found: {EXE}")
    OUT.mkdir(parents=True, exist_ok=True)
    tmp = Path(tempfile.mkdtemp(prefix="dodgeball_"))
    with ThreadPoolExecutor(max_workers=3) as pool:
        files = list(pool.map(lambda s: run(s, tmp), SEEDS))
    lines = []
    for i, f in enumerate(files):  # merged in seed order, one header
        rows = f.read_text(encoding="utf-8").splitlines()
        lines += rows if i == 0 else rows[1:]
    (OUT / "dodgeball_throws.csv").write_text("\n".join(lines) + "\n", encoding="utf-8")

    log = (tmp / "seed1.log").read_text(encoding="utf-8", errors="replace").splitlines()
    (OUT / "dodgeball_log_seed1.txt").write_text(
        "\n".join(l for l in log if l.startswith("DodgeExperiment") or l.strip() in ("0", "1")) + "\n", encoding="utf-8")

    again = run(1, tmp, "_repeat")
    same = again.read_bytes() == files[0].read_bytes()
    (OUT / "dodgeball_repeat_check.txt").write_text(
        f"seed 1 run twice, {THROWS} throws: {'identical CSV rows' if same else 'DIFFERENT CSV rows'}\n", encoding="utf-8")
    print("repeat identical:", same)
    shutil.rmtree(tmp, ignore_errors=True)


if __name__ == "__main__":
    main()
