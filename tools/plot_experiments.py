"""
Draws the report figures from docs/week08/data/*.csv and writes docs/week08/data/summary.json.

  python tools/plot_experiments.py
"""
import csv
import json
import statistics as st
from collections import defaultdict
from pathlib import Path

import matplotlib

matplotlib.use("Agg")
import matplotlib.pyplot as plt
import numpy as np

DATA = Path("docs/week08/data")
OUT = Path("docs/week08/images")
OUT.mkdir(parents=True, exist_ok=True)

INK, MUTED, GRID = "#2b2b2a", "#6b6a64", "#e4e3de"
BLUE, ORANGE, AQUA, GREY = "#2a78d6", "#eb6834", "#1baf7a", "#8a8981"
plt.rcParams.update({
    "font.size": 10, "axes.edgecolor": MUTED, "axes.labelcolor": INK, "xtick.color": MUTED,
    "ytick.color": MUTED, "axes.spines.top": False, "axes.spines.right": False, "axes.grid": True,
    "axes.axisbelow": True, "grid.color": GRID, "grid.linewidth": 0.8, "axes.titlesize": 11,
})
GATES = [("OR", "VEYA (OR)", BLUE), ("AND", "VE (AND)", ORANGE), ("XOR", "XOR", AQUA)]
TRUTH = {"OR": (0, 1, 1, 1), "AND": (0, 0, 0, 1), "XOR": (0, 1, 1, 0)}
POINTS = [(0, 0), (0, 1), (1, 0), (1, 1)]

SCENE_SET = [(0.5, 1, 1), (0, 0.8, 1), (0.2, 1, 1), (0.1, 0.5, 1), (0.3, 0.5, 1), (0.3, 0.8, 1), (0.5, 0.9, 1),
             (0.8, 1, 1), (0.1, 0, 0), (0.5, 0.3, 0), (0.6, 0.3, 0), (0.6, 0.1, 0), (0.7, 0, 0), (0.9, 0, 0)]
FIGURE_SET = [(0.8, 1, 1)] + SCENE_SET[1:12] + [(0, 0.7, 0), (0.9, 0, 0)]


def read(name):
    return list(csv.DictReader((DATA / name).open(encoding="utf-8")))


def class_points(ax, rows, size=55):
    """Class 1 = blue filled circle, class 0 = orange cross: shape repeats the colour."""
    for x, y, c in rows:
        if c == 1:
            ax.scatter(x, y, s=size, color=BLUE, edgecolor="white", linewidth=1.2, zorder=3)
        else:
            ax.scatter(x, y, s=size, color=ORANGE, marker="X", edgecolor="white", linewidth=0.8, zorder=3)


def boundary(ax, w1, w2, b, xs, **kw):
    """Line w1*x + w2*y + b = 0."""
    if abs(w2) < 1e-9:
        ax.axvline(-b / w1, **kw)
    else:
        ax.plot(xs, -(w1 * xs + b) / w2, **kw)


def fig_logic(epochs, runs):
    fig, axes = plt.subplots(1, 2, figsize=(13, 4.4))
    for g, label, color in GATES[:2]:
        first = [int(r["first_zero_error_epoch"]) for r in runs if r["gate"] == g]
        counts = np.bincount(first, minlength=12)[1:12]
        offset = -0.2 if g == "OR" else 0.2
        axes[0].bar(np.arange(1, 12) + offset, counts, width=0.38, color=color, label=label,
                    edgecolor="white", linewidth=1)
    axes[0].set_xticks(range(1, 12))
    axes[0].set_xlabel("İlk hatasız epoch")
    axes[0].set_ylabel("Tekrar sayısı (100 seed)")
    axes[0].set_title("VEYA ve VE kaç epoch'ta öğrenildi?")
    axes[0].legend(frameon=False)

    per = defaultdict(lambda: defaultdict(list))
    for r in epochs:
        per[r["gate"]][int(r["epoch"])].append(float(r["total_error"]))
    for g, label, color in GATES:
        es = sorted(e for e in per[g] if e <= 30)
        m = np.array([np.mean(per[g][e]) for e in es])
        s = np.array([np.std(per[g][e]) for e in es])
        axes[1].plot(es, m, color=color, lw=2, label=label)
        axes[1].fill_between(es, np.maximum(m - s, 0), m + s, color=color, alpha=0.12, lw=0)
        axes[1].annotate(f"{m[-1]:.1f}", (es[-1], m[-1]), xytext=(4, 0), textcoords="offset points",
                         va="center", fontsize=8, color=INK)
    axes[1].set_xlabel("Epoch")
    axes[1].set_ylabel("Epoch içindeki toplam hata")
    axes[1].set_ylim(-0.1, 4.4)
    axes[1].set_title("Epoch başına toplam hata (ortalama ± 1 std)")
    axes[1].legend(frameon=False)
    fig.tight_layout()
    fig.savefig(OUT / "logic_epochs.png", dpi=140)
    plt.close(fig)


def fig_boundaries(runs):
    fig, axes = plt.subplots(1, 3, figsize=(13, 5.0))
    xs = np.linspace(-0.4, 1.4, 50)
    for ax, (g, label, color) in zip(axes, GATES):
        for r in [r for r in runs if r["gate"] == g][:30]:
            boundary(ax, float(r["w1"]), float(r["w2"]), float(r["bias"]), xs, color=GREY, lw=0.8, alpha=0.45)
        class_points(ax, [(x, y, c) for (x, y), c in zip(POINTS, TRUTH[g])], size=120)
        ax.set_xlim(-0.4, 1.4)
        ax.set_ylim(-0.4, 1.4)
        ax.set_aspect("equal")
        ax.set_xlabel("input 1")
        ax.set_ylabel("input 2")
        ax.set_title(label + (": 30 tekrarın karar sınırları" if g != "XOR" else ": 1000 epoch sonrası sınırlar"))
    axes[2].text(0.5, -0.33, "Hiçbir doğru dört noktayı doğru ayırmıyor", ha="center", fontsize=9, color=INK)
    handles = [plt.Line2D([], [], marker="o", color="w", markerfacecolor=BLUE, markersize=9, label="çıktı 1"),
               plt.Line2D([], [], marker="X", color="w", markerfacecolor=ORANGE, markersize=9, label="çıktı 0"),
               plt.Line2D([], [], color=GREY, lw=1.5, label="öğrenilen karar sınırı")]
    fig.legend(handles=handles, loc="lower center", ncol=3, frameon=False)
    fig.tight_layout(rect=(0, 0.07, 1, 0.97))
    fig.savefig(OUT / "logic_boundaries.png", dpi=140)
    plt.close(fig)


def fig_grapher(runs):
    fig, axes = plt.subplots(1, 2, figsize=(13, 5.2))
    xs = np.linspace(0, 1.1, 50)
    for ax, (name, rows, title) in zip(axes, [("scene", SCENE_SET, "Sahnedeki 14 eleman: 100 tekrarın hepsi ayırıyor"),
                                              ("figure", FIGURE_SET, "Şekildeki tablo (balta = 0, 0.7): hiçbiri ayıramıyor")]):
        for r in [r for r in runs if r["set"] == name]:
            boundary(ax, float(r["w1"]), float(r["w2"]), float(r["bias"]), xs, color=GREY, lw=0.6, alpha=0.3)
        class_points(ax, rows)
        if name == "scene":
            for (tx, ty), lab in [((0.3, 0.9), "test (0.3, 0.9)"), ((0.8, 0.1), "test (0.8, 0.1)")]:
                ax.scatter(tx, ty, s=110, facecolor="none", edgecolor=INK, linewidth=1.6, zorder=4)
                ax.annotate(lab, (tx, ty), xytext=(8, 6), textcoords="offset points", fontsize=9, color=INK)
        else:
            ax.scatter(0, 0.7, s=230, facecolor="none", edgecolor=INK, linewidth=1.6, zorder=4)
            ax.annotate("balta (0, 0.7)", (0, 0.7), xytext=(12, -4), textcoords="offset points", fontsize=9, color=INK)
        ax.set_xlim(-0.05, 1.05)
        ax.set_ylim(-0.05, 1.1)
        ax.set_xlabel("Keskinlik (sharp)")
        ax.set_ylabel("Yenebilirlik (edible)")
        ax.set_title(title)
    handles = [plt.Line2D([], [], marker="o", color="w", markerfacecolor=BLUE, markersize=9, label="yiyecek (1)"),
               plt.Line2D([], [], marker="X", color="w", markerfacecolor=ORANGE, markersize=9, label="silah (0)"),
               plt.Line2D([], [], color=GREY, lw=1.5, label="200 epoch sonundaki karar sınırı (her seed bir çizgi)")]
    fig.legend(handles=handles, loc="lower center", ncol=3, frameon=False)
    fig.tight_layout(rect=(0, 0.06, 1, 1))
    fig.savefig(OUT / "grapher_boundaries.png", dpi=140)
    plt.close(fig)


def fig_drawray(runs):
    """The scene's DrawRay before and after the fix, in pixels, for the seed with the steepest line."""
    scene = [r for r in runs if r["set"] == "scene"]
    offsets = [abs(float(r["slope"])) * 100 for r in scene]  # end point error = slope * xoffset pixels
    r = max(scene, key=lambda r: abs(float(r["slope"])))
    m, c = float(r["slope"]), float(r["intercept"])
    scale, off = 500, 100
    x_px = np.array([off, 600 + off])
    true_y = m * (x_px - off) + c * scale + off
    old_y = np.array([c * scale + off, m * x_px[1] + c * scale + off])
    fig, ax = plt.subplots(figsize=(6.4, 5.6))
    for x, y, cl in SCENE_SET:
        px, py = x * scale + off, y * scale + off
        if cl == 1:
            ax.scatter(px, py, s=40, color=BLUE, edgecolor="white", zorder=3)
        else:
            ax.scatter(px, py, s=40, color=ORANGE, marker="X", edgecolor="white", zorder=3)
    ax.plot(x_px, old_y, color=ORANGE, lw=2, ls="--", label="DrawRay (eski): bitiş noktası slope × x2")
    ax.plot(x_px, true_y, color=BLUE, lw=2, label="DrawRay (düzeltilmiş) = gerçek karar sınırı")
    ax.text(110, 700, f"Bitiş noktasındaki fark: {abs(old_y[1] - true_y[1]):.0f} piksel", fontsize=9, color=INK)
    ax.set_xlim(0, 760)
    ax.set_ylim(0, 760)
    ax.set_aspect("equal")
    ax.set_xlabel("doku x (piksel)")
    ax.set_ylabel("doku y (piksel)")
    ax.set_title(f"Seed {r['seed']} ağırlıklarıyla çizilen çizgi (eğim {m:.2f})")
    ax.legend(frameon=False, loc="lower right", fontsize=8)
    fig.tight_layout()
    fig.savefig(OUT / "drawray_fix.png", dpi=140)
    plt.close(fig)
    return {"seed": int(r["seed"]), "slope": m, "intercept": c, "end_offset_px": round(float(abs(old_y[1] - true_y[1])), 1),
            "end_offset_px_mean_100_seeds": round(st.mean(offsets), 1), "end_offset_px_max_100_seeds": round(max(offsets), 1)}


def fig_dodge(throws):
    by = defaultdict(list)
    for r in throws:
        by[int(r["seed"])].append(r)
    n = max(int(r["throw"]) for r in throws)
    wrong = np.array([[1 - int(r["correct"]) for r in sorted(v, key=lambda r: int(r["throw"]))] for v in by.values()])
    red_hit = np.array([[int(r["hit"]) if r["key"] == "1" else 0 for r in sorted(v, key=lambda r: int(r["throw"]))]
                        for v in by.values()])
    fig, axes = plt.subplots(1, 2, figsize=(13, 4.4))
    x = np.arange(1, n + 1)
    axes[0].bar(x, 100 * wrong.mean(axis=0), color=BLUE, width=0.75, edgecolor="white", linewidth=1)
    axes[0].set_xlabel("Atış sırası")
    axes[0].set_ylabel("Yanlış karar veren tekrar (%)")
    axes[0].set_title(f"Hangi atışlarda yanlış karar verildi? ({len(by)} seed)")
    axes[0].set_xlim(0.3, n + 0.7)
    cum = red_hit.cumsum(axis=1).mean(axis=0)
    axes[1].plot(x, cum, color=ORANGE, lw=2)
    axes[1].annotate(f"{cum[-1]:.2f}", (x[-1], cum[-1]), xytext=(4, 0), textcoords="offset points", va="center",
                     fontsize=8, color=INK)
    axes[1].set_xlabel("Atış sırası")
    axes[1].set_ylabel("Kırmızı topun çarpma sayısı (birikimli)")
    axes[1].set_title("Karaktere çarpan kırmızı top sayısı (seed ortalaması)")
    axes[1].set_xlim(1, n + 3)
    fig.tight_layout()
    fig.savefig(OUT / "dodgeball_learning.png", dpi=140)
    plt.close(fig)


def summarize(epochs, runs, grunts, throws, drawray):
    s = {"logic": {}, "grapher": {}, "dodgeball": {}, "drawray": drawray}
    for g, _, _ in GATES:
        r = [x for x in runs if x["gate"] == g]
        first = [int(x["first_zero_error_epoch"]) for x in r if x["first_zero_error_epoch"]]
        errs = [float(x["total_error"]) for x in epochs if x["gate"] == g and int(x["epoch"]) == int(r[0]["epochs_run"])]
        s["logic"][g] = {
            "seeds": len(r), "converged": len(first), "epochs_run": int(r[0]["epochs_run"]),
            "first_zero_mean": round(st.mean(first), 2) if first else None,
            "first_zero_min": min(first) if first else None, "first_zero_max": max(first) if first else None,
            "correct_outputs": {k: sum(1 for x in r if x["correct"] == k) for k in "01234"},
            "last_epoch_error_mean": round(st.mean(errs), 2),
        }
    for name in ("scene", "figure"):
        r = [x for x in grunts if x["set"] == name]
        first = [int(x["first_all_correct_epoch"]) for x in r if x["first_all_correct_epoch"]]
        correct = [int(x["correct_after_200"]) for x in r]
        s["grapher"][name] = {
            "seeds": len(r), "converged": len(first),
            "first_mean": round(st.mean(first), 1) if first else None,
            "first_min": min(first) if first else None, "first_max": max(first) if first else None,
            "correct_after_200_mean": round(st.mean(correct), 2), "correct_after_200_min": min(correct),
            "correct_after_200_max": max(correct),
            "test_03_09_food": sum(1 for x in r if x["test_03_09"] == "1"),
            "test_08_01_food": sum(1 for x in r if x["test_08_01"] == "1"),
        }
    by = defaultdict(list)
    for r in throws:
        by[int(r["seed"])].append(r)
    wrong = [sum(1 - int(x["correct"]) for x in v) for v in by.values()]
    last = [max([int(x["throw"]) for x in v if x["correct"] == "0"] or [0]) for v in by.values()]
    red = [x for x in throws if x["key"] == "1"]
    s["dodgeball"] = {
        "seeds": len(by), "throws_per_seed": max(int(x["throw"]) for x in throws),
        "wrong_mean": round(st.mean(wrong), 2), "wrong_std": round(st.stdev(wrong), 2),
        "wrong_min": min(wrong), "wrong_max": max(wrong),
        "last_wrong_mean": round(st.mean(last), 1), "last_wrong_max": max(last),
        "seeds_without_mistake": wrong.count(0),
        "red_throws": len(red), "red_hits": sum(int(x["hit"]) for x in red),
        "wrong_after_throw_15": sum(1 for x in throws if int(x["throw"]) > 15 and x["correct"] == "0"),
        "stand_equals_hit": sum(1 for x in throws if (x["decision"] == "1") == (x["hit"] == "1")),
        "total_throws": len(throws),
        "final_weights_example": {k: by[1][-1][k] for k in ("w1", "w2", "bias")},
    }
    (DATA / "summary.json").write_text(json.dumps(s, indent=1, ensure_ascii=False), encoding="utf-8")


def main():
    epochs, runs = read("logic_epochs.csv"), read("logic_runs.csv")
    grunts, throws = read("grapher_runs.csv"), read("dodgeball_throws.csv")
    fig_logic(epochs, runs)
    fig_boundaries(runs)
    fig_grapher(grunts)
    drawray = fig_drawray(grunts)
    fig_dodge(throws)
    summarize(epochs, runs, grunts, throws, drawray)
    print("figures ->", OUT)


if __name__ == "__main__":
    main()
