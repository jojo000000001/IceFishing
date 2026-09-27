"""
生成冰钓水下背景图:
  - WaterTile: 水下瓦片 (632x1124), 可水平+垂直无缝平铺
  - LeftIce_0/1: 左冰壁变种
  - RightIce_0/1: 右冰壁变种
"""

import random
import os
from PIL import Image, ImageDraw, ImageFilter

random.seed(42)

BASE = os.path.dirname(os.path.abspath(__file__))
OUT_DIR = os.path.join(BASE, "Assets", "Art", "Backgrounds")
os.makedirs(OUT_DIR, exist_ok=True)


# ─────────────────────────────────────────────────────────
# 底层工具
# ─────────────────────────────────────────────────────────

def blend(dst: Image, src: Image, mask: Image, alpha: float):
    """按 alpha 混合 src 到 dst (两者尺寸须一致)"""
    tmp = src.copy()
    tmp.putalpha(int(255 * alpha))
    m = mask.copy()
    m.putalpha(int(255 * alpha))
    dst.paste(tmp, (0, 0), m)


def add_bubbles(img: Image, seed_offset: int, count: int = 60, max_alpha: float = 0.45):
    """在半透明 mask 上画圆形气泡，中心最亮"""
    w, h = img.size
    draw = ImageDraw.Draw(img, "RGBA")

    rng = random.Random(seed_offset)
    for _ in range(count):
        cx = rng.randint(0, w)
        cy = rng.randint(h // 4, h)
        r = rng.randint(1, max(2, w // 22))
        # 外环淡、中间亮
        alpha = int(max_alpha * rng.uniform(0.4, 1.0) * 255)
        draw.ellipse([cx - r, cy - r, cx + r, cy + r],
                     fill=(190, 225, 255, alpha))
        if r >= 3:
            hi = r // 3
            draw.ellipse([cx - hi, cy - hi, cx + hi, cy + hi],
                         fill=(230, 245, 255, int(alpha * 0.8)))

    return img


def add_light_rays(img: Image, seed_offset: int, ray_count: int = 6):
    """从上往下渐变光柱"""
    w, h = img.size
    draw = ImageDraw.Draw(img, "RGBA")
    rng = random.Random(seed_offset + 1)

    for _ in range(ray_count):
        x = rng.randint(int(w * 0.05), int(w * 0.95))
        half_w = rng.randint(w // 20, w // 8)
        top_a = rng.randint(15, 35)
        bot_a = 0
        for y in range(0, h):
            t = y / h
            a = int(top_a * (1 - t))
            draw.rectangle([x - half_w, y, x + half_w, y + 1],
                           fill=(180, 220, 255, a))
        half_w = int(half_w * 0.4)
        top_a = int(top_a * 0.6)
        for y in range(0, h):
            t = y / h
            a = int(top_a * (1 - t))
            draw.rectangle([x - half_w, y, x + half_w, y + 1],
                           fill=(210, 240, 255, a))

    return img


# ─────────────────────────────────────────────────────────
# 水下瓦片 (WaterTileA/B/C 共享同一张图，seed 不同产生差异)
# ─────────────────────────────────────────────────────────

def make_water_tile(seed: int, filename: str):
    W, H = 632, 1124

    # 1) 深水渐变背景
    bg = Image.new("RGBA", (W, H), (6, 28, 68, 255))
    grad = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    gdraw = ImageDraw.Draw(grad)
    for y in range(H):
        t = y / H  # 0=顶 1=底
        # 顶部偏浅蓝(深水透光)，底部偏深蓝
        r = int(6  + (0 - 6)  * t)
        g = int(28 + (5 - 28) * t)
        b = int(68 + (25 - 68) * t)
        gdraw.rectangle([0, y, W, y + 1], fill=(r, g, b, 255))
    bg = Image.alpha_composite(bg, grad)

    # 2) 沿 Y 轴重复的气泡层 (mask only → 合入透明度通道)
    rng = random.Random(seed)
    bubble_mask = Image.new("L", (W, H), 0)
    bd = ImageDraw.Draw(bubble_mask)

    # 沿 H 重复的单位高度
    tile_h = H // 3
    for ty in range(3):
        base_y = ty * tile_h
        for _ in range(25):
            bx = rng.randint(0, W)
            by = base_y + rng.randint(0, tile_h)
            r = rng.randint(2, W // 18)
            a = rng.randint(30, 110)
            bd.ellipse([bx - r, by - r, bx + r, by + r], fill=a)
            if r >= 4:
                hi = r // 3
                bd.ellipse([bx - hi, by - hi, bx + hi, by + hi],
                           fill=min(a + 40, 255))

    # 高斯模糊 → 柔和边缘
    bubble_mask = bubble_mask.filter(ImageFilter.GaussianBlur(radius=max(W // 40, 4)))

    # 将 mask 叠加到图像 alpha
    bg = alpha_overlay(bg, bubble_mask)  # 白色亮区 → alpha 增加

    # 3) 光柱 (seed 偏移以区别 A/B/C)
    rays = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    add_light_rays(rays, seed + 100)
    bg = Image.alpha_composite(bg, rays)

    # 4) 底部深色渐变 (模拟远景消隐)
    fade = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    fdraw = ImageDraw.Draw(fade)
    for y in range(H // 2, H):
        t = (y - H // 2) / (H // 2)
        fdraw.rectangle([0, y, W, y + 1],
                        fill=(0, 5, 20, int(140 * t)))
    bg = Image.alpha_composite(bg, fade)

    out_path = os.path.join(OUT_DIR, filename)
    bg.save(out_path, "PNG")
    print(f"  ✓ {filename}  ({W}×{H})")
    return out_path


# ─────────────────────────────────────────────────────────
# 冰壁 (左/右各两变种)
# ─────────────────────────────────────────────────────────

def make_ice_wall(side: str, variant: int, filename: str):
    """
    side   : 'left' | 'right'
    variant: 0 | 1  (两种随机纹理)
    输出宽度约 300 px，高度与 WaterTile 相同 (1124 px)
    """
    W, H = 300, 1124

    # 基础冰色 (略带蓝绿)
    base_top   = (185, 218, 238, 255)
    base_mid   = (130, 178, 215, 255)
    base_bot   = (55,  100, 165, 255)

    # 垂直渐变
    bg = Image.new("RGBA", (W, H), (0, 0, 0, 255))
    grad = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    gdraw = ImageDraw.Draw(grad)
    for y in range(H):
        t = y / H
        if t < 0.3:
            c = tuple(int(a + (b - a) * (t / 0.3)) for a, b in zip(base_top, base_mid))
        else:
            c = tuple(int(a + (b - a) * ((t - 0.3) / 0.7)) for a, b in zip(base_mid, base_bot))
        gdraw.rectangle([0, y, W, y + 1], fill=c + (255,))
    bg = Image.alpha_composite(bg, grad)

    rng = random.Random(1000 if side == "left" else 2000 + variant * 300)
    # 冰裂缝 / 暗纹
    ice_overlay = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    od = ImageDraw.Draw(ice_overlay)
    for _ in range(8 + variant * 4):
        x0 = rng.randint(0, W)
        y0 = rng.randint(0, H)
        segs = []
        x, y = x0, y0
        for _ in range(rng.randint(3, 7)):
            dx = rng.randint(-60, 60)
            dy = rng.randint(10, 40)
            x += dx
            y += dy
            segs.append((max(0, min(W - 1, x)), max(0, min(H - 1, y))))
        if len(segs) >= 2:
            for i in range(len(segs) - 1):
                a = rng.randint(30, 80)
                od.line([segs[i], segs[i + 1]],
                        fill=(40, 70, 130, a), width=rng.randint(1, 3))

    bg = Image.alpha_composite(bg, ice_overlay)

    # 冰块边缘 (底部有一些凸起造型)
    chunk_overlay = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    cd = ImageDraw.Draw(chunk_overlay)
    for _ in range(5 + variant * 3):
        bx = rng.randint(0, W)
        by = rng.randint(H // 2, H)
        bw = rng.randint(20, 80)
        bh = rng.randint(10, 40)
        a = rng.randint(20, 60)
        cd.polygon(
            [(bx, by), (bx + bw, by), (bx + bw - bw // 3, by - bh),
             (bx + bw // 3, by - bh)],
            fill=(200, 230, 250, a))

    bg = Image.alpha_composite(bg, chunk_overlay)

    # 右侧冰壁需要翻转
    if side == "right":
        bg = bg.transpose(Image.FLIP_LEFT_RIGHT)

    # 外侧渐黑 (向屏内)
    edge_mask = Image.new("L", (W, H), 255)
    ed = ImageDraw.Draw(edge_mask)
    if side == "left":
        for x in range(W):
            t = x / W
            a = int(200 * t)
            ed.rectangle([x, 0, x + 1, H], fill=a)
    else:
        for x in range(W):
            t = 1 - x / W
            a = int(200 * t)
            ed.rectangle([x, 0, x + 1, H], fill=a)

    bg = alpha_multiply(bg, edge_mask)

    out_path = os.path.join(OUT_DIR, filename)
    bg.save(out_path, "PNG")
    print(f"  ✓ {filename}  ({W}×{H})")
    return out_path


# ─────────────────────────────────────────────────────────
# 工具: alpha 通道运算
# ─────────────────────────────────────────────────────────

def alpha_overlay(bg_rgba: Image, overlay_rgba: Image) -> Image:
    """overlay 的 R 通道作为亮度，加到 bg 的 alpha 上（加法混合）"""
    bg = bg_rgba.copy()
    a = bg.split()[3]   # L
    o = overlay_rgba.split()[0]  # R
    a_np = np.array(a, dtype=np.int32)
    o_np = np.array(o, dtype=np.int32)
    new_a = np.clip(a_np + o_np, 0, 255).astype(np.uint8)
    bg.putalpha(Image.fromarray(new_a, "L"))
    return bg


def alpha_multiply(bg_rgba: Image, mask_l: Image) -> Image:
    """用 mask L 的亮度削减 bg alpha（边缘渐隐）"""
    bg = bg_rgba.copy()
    a = bg.split()[3]
    a_np = np.array(a, dtype=np.float32)
    m_np = np.array(mask_l, dtype=np.float32) / 255.0
    new_a = (a_np * m_np).astype(np.uint8)
    bg.putalpha(Image.fromarray(new_a, "L"))
    return bg


# ─────────────────────────────────────────────────────────
# 主入口
# ─────────────────────────────────────────────────────────

if __name__ == "__main__":
    print("Generating underwater backgrounds...")

    print("\n[Water Tiles]")
    make_water_tile(0,  "WaterTileA.png")
    make_water_tile(42, "WaterTileB.png")
    make_water_tile(99, "WaterTileC.png")

    print("\n[Ice Walls]")
    make_ice_wall("left",  0, "LeftIce_0.png")
    make_ice_wall("left",  1, "LeftIce_1.png")
    make_ice_wall("right", 0, "RightIce_0.png")
    make_ice_wall("right", 1, "RightIce_1.png")

    print("\nDone.")
