"""
生成一张《IceFishing》游戏UI 概念图 (760 x 1320, 竖版):
  - 顶部 HUD: 鱼饵 0/2 / 鱼标 10/10 / 鱼 0/100
  - 上半: 灰白天 + 雪山 + 雾气
  - 中段: 一条分界 + 圆冰洞
  - 下半: 蓝色透冰 + 冰脊气泡
  - 角色: 戴灰色绒帽 + 围巾的小老头坐左凳, 持短杆望向冰洞
  - 道具: 右侧红风杯 / 煤油灯 / 木箱
套路: 跟 generate_underwater.py 一样的 PIL 程序化画法 (本项目已经有这套基建)
"""
import os, random, math
from PIL import Image, ImageDraw, ImageFilter

random.seed(7)
OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)),
                   "Assets", "Art", "Backgrounds", "ice_fishing_scene.png")
os.makedirs(os.path.dirname(OUT), exist_ok=True)

W, H = 760, 1320

# ── 主图层 ──────────────────────────────────────────────
sky    = Image.new("RGBA", (W, H), (225, 230, 235, 255))
ice    = Image.new("RGBA", (W, H), (28, 70, 120, 255))
hud    = Image.new("RGBA", (W, H), (0, 0, 0, 0))
scene  = Image.new("RGBA", (W, H), (0, 0, 0, 0))

# ── 上半: 灰色雪天 + 雪山 ───────────────────────────────
def draw_sky(target):
    d = ImageDraw.Draw(target)
    # 顶层天空
    for y in range(int(H * 0.55)):
        t = y / (H * 0.55)
        # 顶端略带蓝、底端灰白
        r = int(225 - 25 * t)
        g = int(230 - 25 * t)
        b = int(238 - 30 * t)
        d.rectangle([0, y, W, y + 1], fill=(r, g, b))
    # 远雪山脉(剪影)
    rng = random.Random(1)
    mtn = []
    last_x = 0
    while last_x < W:
        peak_h = rng.randint(120, 220)
        peak_x = last_x + rng.randint(60, 130)
        mtn.append((peak_x, int(H * 0.45) - peak_h))
        last_x = peak_x
    # 闭合到底
    poly = mtn + [(W, int(H * 0.55)), (0, int(H * 0.55))]
    d.polygon(poly, fill=(190, 198, 205))
    # 更近山脉
    mtn2 = []
    last_x = 0
    while last_x < W:
        peak_h = rng.randint(60, 110)
        peak_x = last_x + rng.randint(50, 100)
        mtn2.append((peak_x, int(H * 0.55) - peak_h))
        last_x = peak_x
    poly2 = mtn2 + [(W, int(H * 0.55)), (0, int(H * 0.55))]
    d.polygon(poly2, fill=(165, 175, 185))

draw_sky(sky)

# 雾(柔光层)
fog = Image.new("RGBA", (W, int(H * 0.25)), (245, 245, 248, 0))
for y in range(fog.size[1]):
    a = int(180 * (1 - y / fog.size[1]))
    ImageDraw.Draw(fog).rectangle([0, y, W, y + 1], fill=(245, 245, 248, a))
fog = fog.filter(ImageFilter.GaussianBlur(8))
sky.paste(fog, (0, int(H * 0.30)), fog)

# ── 前景雪面(连地面) ────────────────────────────────
d_sk = ImageDraw.Draw(sky)
d_sk.rectangle([0, int(H * 0.55), W, int(H * 0.62)], fill=(232, 235, 240))
# 一些雪颗粒
rng = random.Random(2)
for _ in range(220):
    x = rng.randint(0, W)
    y = rng.randint(int(H * 0.55), int(H * 0.62))
    r = rng.randint(1, 2)
    d_sk.ellipse([x - r, y - r, x + r, y + r], fill=(255, 255, 255, 150))

# ── 冰洞(中部分界线下部) ──────────────────────────────
d_ic = ImageDraw.Draw(ice)
# 圆冰洞 - 居中, 上下各半跨
cx, cy = W // 2, int(H * 0.62)
hole_r = 130
# 上半:雪面中的洞 (深蓝水 + 黑色边框)
d_sk.ellipse([cx - hole_r, cy - hole_r, cx + hole_r, cy + hole_r], fill=(15, 35, 70))
d_sk.ellipse([cx - hole_r - 4, cy - hole_r - 4, cx + hole_r + 4, cy + hole_r + 4],
             outline=(60, 70, 80), width=4)
# 高光边 (右上)
d_sk.arc([cx - hole_r + 6, cy - hole_r + 6, cx + hole_r - 6, cy + hole_r - 6],
         start=300, end=345, fill=(255, 255, 255, 220), width=2)

# ── 下半: 水底视角(冰脊/气泡) ─────────────────────────
for y in range(int(H * 0.62), H):
    t = (y - H * 0.62) / (H * 0.38)
    r = int(28 + 5 * t)
    g = int(70 - 25 * t)
    b = int(120 - 40 * t)
    d_ic.rectangle([0, y, W, y + 1], fill=(r, g, b))
# 冰脊(纵向光柱)
rng = random.Random(3)
for _ in range(12):
    x = rng.randint(0, W)
    w_r = rng.randint(20, 60)
    for y in range(int(H * 0.62), H):
        a = int(60 * (1 - (y - H * 0.62) / (H * 0.38)))
        d_ic.rectangle([x - w_r, y, x + w_r, y + 1], fill=(160, 210, 255, a))
# 气泡
rng = random.Random(4)
for _ in range(70):
    bx = rng.randint(0, W)
    by = rng.randint(int(H * 0.62), H)
    rr = rng.randint(2, 10)
    d_ic.ellipse([bx - rr, by - rr, bx + rr, by + rr], outline=(200, 230, 255, 130))

# ── 合成背景(上 + 下) ──────────────────────────────────
scene.paste(sky, (0, 0))
# 中部分界线 (雪/水交接)
line = Image.new("RGBA", (W, int(H * 0.04)), (235, 240, 245, 0))
ImageDraw.Draw(line).rectangle([0, 0, W, line.size[1]], fill=(235, 240, 245, 255))
scene.paste(line, (0, int(H * 0.60)))
scene.paste(ice, (0, int(H * 0.62)), ice)

# 圆冰洞再画一次(覆盖下半水颜色)
draw = ImageDraw.Draw(scene)
draw.ellipse([cx - hole_r, cy - hole_r, cx + hole_r, cy + hole_r], fill=(12, 30, 60))
draw.ellipse([cx - hole_r - 4, cy - hole_r - 4, cx + hole_r + 4, cy + hole_r + 4],
             outline=(50, 60, 70), width=4)
# 洞里水也带小气泡
rng = random.Random(5)
for _ in range(15):
    bx = cx + rng.randint(-hole_r + 10, hole_r - 10)
    by = cy + rng.randint(-hole_r + 10, hole_r - 10)
    rr = rng.randint(1, 4)
    draw.ellipse([bx - rr, by - rr, bx + rr, by + rr], fill=(220, 240, 255, 180))

# ── 角色(简笔小老头) ──────────────────────────────────
def draw_fisher(target, x, y):
    d = ImageDraw.Draw(target)
    # 凳子
    d.rectangle([x - 28, y + 110, x + 28, y + 130], fill=(70, 50, 35))
    # 围巾
    d.ellipse([x - 22, y - 10, x + 22, y + 20], fill=(180, 40, 40))
    # 身体(深色大衣)
    d.rounded_rectangle([x - 30, y + 10, x + 30, y + 110], radius=12, fill=(70, 80, 95))
    # 头
    d.ellipse([x - 18, y - 32, x + 18, y + 8], fill=(245, 220, 195))
    # 帽子
    d.chord([x - 22, y - 50, x + 22, y - 8], start=180, end=360, fill=(180, 185, 190))
    d.rectangle([x - 24, y - 12, x + 24, y - 6], fill=(160, 165, 170))
    # 帽顶绒球
    d.ellipse([x - 5, y - 56, x + 5, y - 46], fill=(220, 222, 226))
    # 鱼竿(指向冰洞)
    rod_end = (cx + 10, cy + 4)
    rod_start = (x + 28, y + 35)
    d.line([rod_start, rod_end], fill=(90, 60, 30), width=3)
    # 鱼线
    d.line([rod_end, (rod_end[0] + 5, rod_end[1] + 200)], fill=(240, 240, 240, 180), width=1)

draw_fisher(scene, int(W * 0.20), int(H * 0.43))

# ── 道具桌(右侧) ─────────────────────────────────────
def draw_table(target, x, y):
    d = ImageDraw.Draw(target)
    # 桌
    d.rectangle([x - 60, y + 5, x + 60, y + 18], fill=(120, 88, 56))
    # 积雪
    d.rectangle([x - 60, y, x + 60, y + 6], fill=(250, 252, 255))
    # 桌腿
    d.rectangle([x - 50, y + 18, x - 46, y + 90], fill=(90, 60, 35))
    d.rectangle([x + 46, y + 18, x + 50, y + 90], fill=(90, 60, 35))
    # 灯
    d.rounded_rectangle([x - 20, y - 40, x + 20, y + 5], radius=8, fill=(60, 50, 40))
    d.polygon([(x - 12, y - 60), (x + 12, y - 60),
               (x + 20, y - 40), (x - 20, y - 40)], fill=(255, 200, 110))
    # 灯光圈(柔光)
    halo = Image.new("RGBA", (140, 60), (0, 0, 0, 0))
    hd = ImageDraw.Draw(halo)
    for r in range(50, 0, -4):
        a = int(60 * (1 - r / 50))
        hd.ellipse([70 - r, 30 - r // 2, 70 + r, 30 + r // 2], fill=(255, 220, 150, a))
    target.paste(halo, (x - 70, y - 90), halo)
    # 红色风杯
    cup_top = y - 70
    d.rectangle([x + 30, cup_top, x + 34, y + 5], fill=(80, 80, 90))
    d.ellipse([x + 18, cup_top - 14, x + 46, cup_top + 14], outline=(220, 40, 40), width=4)
    d.line([x + 30, cup_top - 14, x + 30, cup_top + 14], fill=(220, 40, 40), width=2)
    d.line([x + 18, cup_top, x + 46, cup_top], fill=(220, 40, 40), width=2)
    # 木箱
    d.rounded_rectangle([x - 50, y - 25, x - 10, y + 5], radius=3, fill=(120, 80, 45))
    d.rectangle([x - 50, y - 12, x - 10, y - 8], fill=(90, 60, 30))

draw_table(scene, int(W * 0.78), int(H * 0.46))

# ── HUD(顶部) ──────────────────────────────────────────
def draw_hud(target):
    d = ImageDraw.Draw(target)
    bg = Image.new("RGBA", (W, 110), (15, 22, 35, 220))
    target.paste(bg, (0, 0))
    # 三格面板
    panels = [
        ("鱼 饵", "0", "/2", (255, 200, 90)),
        ("鱼 标", "10", "/10", (220, 230, 240)),
        ("鱼", "0", "/100", (120, 200, 255)),
    ]
    pw = (W - 60) // 3
    for i, (lab, cur, tot, col) in enumerate(panels):
        x0 = 20 + i * (pw + 6)
        d.rounded_rectangle([x0, 18, x0 + pw, 92], radius=10, fill=(30, 40, 60))
        d.text((x0 + 16, 26), lab, fill=(180, 190, 210))
        # 大数字
        d.text((x0 + 16, 48), cur, fill=col)
        d.text((x0 + 70, 70), tot, fill=(140, 150, 170))

draw_hud(hud)
scene.alpha_composite(hud)

# ── 其它细节: 飘雪 ────────────────────────────────────
rng = random.Random(9)
snow = Image.new("RGBA", (W, H), (0, 0, 0, 0))
sd = ImageDraw.Draw(snow)
for _ in range(120):
    x = rng.randint(0, W)
    y = rng.randint(0, H)
    r = rng.randint(1, 3)
    sd.ellipse([x - r, y - r, x + r, y + r], fill=(255, 255, 255, 220))
scene.alpha_composite(snow)

# ── 保存 ──────────────────────────────────────────────
scene.convert("RGB").save(OUT, "PNG", optimize=True)
print(f"saved: {OUT}")
