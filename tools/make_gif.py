import glob
import os
import sys
from PIL import Image

src = r"F:\DSH space\gif-frames"
out = r"F:\DSH space\desktop-todo\assets\demo.gif"

files = sorted(glob.glob(os.path.join(src, "*.png")))
if not files:
    print("no frames")
    sys.exit(1)

W, H = 440, 506
raw = [Image.open(f).convert("RGB").resize((W, H), Image.LANCZOS) for f in files]

# 每帧停留时间（毫秒）
durations = []
for f in files:
    n = int(os.path.basename(f)[:3])
    if n <= 3:
        durations.append(420)      # 初始状态停留
    elif n <= 5:
        durations.append(240)      # 悬停 / 按下
    elif n <= 18:
        durations.append(70)       # 拖动过程
    elif n == 19:
        durations.append(420)      # 停在目标格
    elif n == 20:
        durations.append(420)      # 松手
    elif n == 21:
        durations.append(700)      # 结果
    else:
        durations.append(900)      # 结束停留

pal = raw[0].quantize(colors=256, method=Image.Quantize.MEDIANCUT)
frames = [im.quantize(palette=pal, dither=Image.Dither.FLOYDSTEINBERG) for im in raw]

frames[0].save(
    out,
    save_all=True,
    append_images=frames[1:],
    duration=durations,
    loop=0,
    optimize=True,
    disposal=2,
)

print("frames:", len(frames))
print("size KB:", round(os.path.getsize(out) / 1024, 1))
print("total ms:", sum(durations))
