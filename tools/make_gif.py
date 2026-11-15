"""
Frames recorded by DodgeExperiment -> README animation and a film strip for the report.
  DodgeBall.exe -autoThrow -seed 4 -throws 12 -interval 1.2 -recordFrames <dir> -fps 20 -logFile <dir>/player.log
                -screen-width 960 -screen-height 540 -screen-fullscreen 0
  python tools/make_gif.py <dir>
The player log says at which recorded frame each throw left the camera; an object reaches the character
about 0.25 s (5 frames at 20 fps) later.
"""
import re
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

ARRIVAL = 5
# seed 4, what happened in each pictured throw (decisions from the same seed run headless)
STRIP = [(1, "1. atış: yeşil küp, gereksiz yere eğildi"), (2, "2. atış: yeşil küp, ayakta kaldı"),
         (5, "5. atış: kırmızı top, eğilmedi ve çarptı"), (6, "6. atış: kırmızı top, eğildi"),
         (11, "11. atış: kırmızı top, eğildi"), (12, "12. atış: kırmızı küp, ayakta kaldı")]

src_dir = Path(sys.argv[1])
frames = [Image.open(p).convert("RGB") for p in sorted(src_dir.glob("frame_*.png"))]
log = (src_dir / "player.log").read_text(encoding="utf-8", errors="replace")
throw_frame = {int(t): int(f) for t, f in re.findall(r"throw (\d+) key \d at recorded frame (\d+)", log)}
out = Path("docs/week08/images")
out.mkdir(parents=True, exist_ok=True)

small = [f.resize((432, 243), Image.LANCZOS).quantize(64) for f in frames[::2]]  # 10 frames per second
small[0].save(out / "dodgeball.gif", save_all=True, append_images=small[1:], duration=100, loop=0, optimize=True)

font = ImageFont.truetype("C:/Windows/Fonts/arial.ttf", 15)
w, h = 400, 225
strip = Image.new("RGB", (3 * w + 40, 2 * (h + 28) + 10), (255, 255, 255))
draw = ImageDraw.Draw(strip)
for k, (throw, label) in enumerate(STRIP):
    i = min(throw_frame[throw] + ARRIVAL, len(frames) - 1)
    x, y = 10 + (k % 3) * (w + 10), 5 + (k // 3) * (h + 28)
    strip.paste(frames[i].resize((w, h), Image.LANCZOS), (x, y))
    draw.text((x + 4, y + h + 5), label, fill=(30, 30, 30), font=font)
strip.save(out / "dodgeball_filmstrip.png")
print((out / "dodgeball.gif").stat().st_size // 1024, "KB")
