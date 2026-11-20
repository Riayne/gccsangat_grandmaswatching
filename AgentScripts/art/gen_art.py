"""Generates placeholder art for the jam game into Assets/Art.

Run: python AgentScripts/art/gen_art.py   (needs: pip install pillow numpy)
All coordinates are in the 1080x1920 reference frame (portrait), y from top.
Writes Assets/Art/*.png and AgentScripts/art/layout.json (world positions for the scene builder).
"""
import json, math, os, random
from PIL import Image, ImageDraw, ImageFilter

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "Assets", "Art")
os.makedirs(OUT, exist_ok=True)
S = 2  # supersampling
OUTLINE = (26, 16, 24, 255)
layout = {}
random.seed(7)


class Cv:
    def __init__(self, w, h, ox=0, oy=0, bg=(0, 0, 0, 0)):
        self.w, self.h, self.ox, self.oy = w, h, ox, oy
        self.img = Image.new("RGBA", (w * S, h * S), bg)
        self.d = ImageDraw.Draw(self.img)

    def p(self, x, y):
        return ((x - self.ox) * S, (y - self.oy) * S)

    def box(self, x0, y0, x1, y1):
        a, b = self.p(x0, y0)
        c, d = self.p(x1, y1)
        return [a, b, c, d]

    def rect(self, x0, y0, x1, y1, fill, ol=OUTLINE, w=3):
        self.d.rectangle(self.box(x0, y0, x1, y1), fill=fill, outline=ol, width=w * S if ol else 0)

    def rrect(self, x0, y0, x1, y1, r, fill, ol=OUTLINE, w=3):
        self.d.rounded_rectangle(self.box(x0, y0, x1, y1), r * S, fill=fill, outline=ol, width=w * S if ol else 0)

    def ell(self, x0, y0, x1, y1, fill, ol=OUTLINE, w=3):
        self.d.ellipse(self.box(x0, y0, x1, y1), fill=fill, outline=ol, width=w * S if ol else 0)

    def circ(self, x, y, r, fill, ol=OUTLINE, w=3):
        self.ell(x - r, y - r, x + r, y + r, fill, ol, w)

    def poly(self, pts, fill, ol=OUTLINE, w=3):
        pp = [self.p(x, y) for x, y in pts]
        self.d.polygon(pp, fill=fill)
        if ol:
            self.d.line(pp + [pp[0]], fill=ol, width=w * S, joint="curve")

    def line(self, pts, fill=OUTLINE, w=3):
        self.d.line([self.p(x, y) for x, y in pts], fill=fill, width=int(w * S), joint="curve")

    def save(self, name):
        im = self.img.resize((self.w, self.h), Image.LANCZOS)
        im.save(os.path.join(OUT, name + ".png"))
        return im


def put(name, x0, y0, x1, y1, order, cv=None):
    """Record a sprite's reference rect -> world position (PPU 100, origin at frame centre)."""
    layout[name] = {
        "x": round(((x0 + x1) / 2 - 540) / 100, 4),
        "y": round((960 - (y0 + y1) / 2) / 100, 4),
        "w": round((x1 - x0) / 100, 4),
        "h": round((y1 - y0) / 100, 4),
        "order": order,
    }


def overlay(base, layer):
    base.alpha_composite(layer)


# ---------------------------------------------------------------- geometry
FLOOR = 864
KITCHEN = (50, 400, 290, FLOOR)
BEDROOM = (790, 400, 1030, FLOOR)
HALL = (425, 530, 655, FLOOR)
WINDOW = (390, 190, 690, 470)
SOFA = (30, 930, 430, 1290)

# ---------------------------------------------------------------- background
def background():
    W, H, OY = 1080, 2400, -240
    c = Cv(W, H, 0, OY)
    d = c.d
    # wall: vertical gradient
    for y in range(OY, FLOOR + 1, 4):
        t = (y - OY) / (FLOOR - OY)
        col = (int(34 + 20 * t), int(58 + 18 * t), int(66 + 14 * t), 255)
        c.rect(0, y, W, y + 4, col, ol=None)
    # dado band
    c.rect(0, FLOOR - 70, W, FLOOR, (40, 52, 58, 255), ol=None)
    for x in range(0, W, 36):
        c.poly([(x, FLOOR - 70), (x + 18, FLOOR - 55), (x + 36, FLOOR - 70)], (180, 140, 60, 255), ol=None)
    c.rect(0, FLOOR - 6, W, FLOOR, (25, 20, 22, 255), ol=None)
    # floor tiles
    tile = 90
    for row, y in enumerate(range(FLOOR, 2160 + tile, tile)):
        for col, x in enumerate(range(-tile, W + tile, tile)):
            xo = x + (tile // 2 if row % 2 else 0)
            base = (98, 70, 58, 255) if (row + col) % 2 else (124, 96, 76, 255)
            c.rect(xo, y, xo + tile, y + tile, base, ol=(60, 40, 34, 255), w=2)
            cx, cy = xo + tile / 2, y + tile / 2
            c.poly([(cx, cy - 22), (cx + 22, cy), (cx, cy + 22), (cx - 22, cy)], (150, 118, 70, 255), ol=None)
            c.circ(cx, cy, 6, (70, 110, 110, 255), ol=None)
    # doorways
    for (x0, y0, x1, y1), hinge_left in ((KITCHEN, True), (BEDROOM, False), (HALL, True)):
        c.rect(x0 - 18, y0 - 18, x1 + 18, y1, (92, 54, 30, 255), w=3)
        c.rect(x0, y0, x1, y1, (14, 10, 18, 255), ol=None)
        # swung-open door panel in the dark
        dw = (x1 - x0) * 0.22
        if hinge_left:
            c.poly([(x0, y0), (x0 + dw, y0 + 25), (x0 + dw, y1 - 10), (x0, y1)], (70, 40, 22, 255), w=2)
        else:
            c.poly([(x1, y0), (x1 - dw, y0 + 25), (x1 - dw, y1 - 10), (x1, y1)], (70, 40, 22, 255), w=2)
    # window opening: moonlit sky
    x0, y0, x1, y1 = WINDOW
    for y in range(y0, y1, 3):
        t = (y - y0) / (y1 - y0)
        c.rect(x0, y, x1, y + 3, (int(40 + 40 * t), int(60 + 50 * t), int(120 + 60 * t), 255), ol=None)
    c.circ(x1 - 70, y0 + 60, 32, (235, 240, 255, 255), ol=None)
    for _ in range(25):
        sx, sy = random.uniform(x0, x1), random.uniform(y0, y1 - 60)
        c.circ(sx, sy, 2, (220, 230, 255, 255), ol=None)
    # calligraphy frame (abstract strokes)
    c.rect(815, 215, 1005, 345, (196, 150, 50, 255), w=3)
    c.rect(830, 230, 990, 330, (20, 70, 50, 255), w=2)
    c.line([(850, 300), (880, 260), (905, 300), (930, 255), (960, 305)], (230, 190, 80, 255), 5)
    c.line([(860, 315), (975, 315)], (230, 190, 80, 255), 4)
    c.circ(900, 250, 5, (230, 190, 80, 255), ol=None)
    c.circ(945, 245, 5, (230, 190, 80, 255), ol=None)
    # ceiling fan
    c.rect(535, OY, 545, 70, (60, 50, 45, 255), ol=None)
    c.ell(505, 60, 575, 100, (110, 80, 55, 255))
    c.poly([(505, 80), (330, 70), (330, 95), (505, 92)], (130, 95, 65, 255))
    c.poly([(575, 80), (750, 70), (750, 95), (575, 92)], (130, 95, 65, 255))
    # hanging bulb
    c.line([(170, OY), (170, 175)], (30, 25, 25, 255), 3)
    c.rect(160, 170, 180, 185, (80, 80, 80, 255), w=2)
    c.ell(152, 182, 188, 228, (255, 230, 140, 255), w=2)
    # almirah
    c.rrect(860, 900, 1050, 1320, 6, (128, 142, 156, 255))
    c.line([(955, 910), (955, 1310)], OUTLINE, 3)
    c.rect(940, 1080, 950, 1130, (220, 220, 210, 255), w=2)
    c.rect(960, 1080, 970, 1130, (220, 220, 210, 255), w=2)
    c.rect(870, 915, 940, 1060, (150, 165, 178, 255), ol=None)
    c.rect(970, 915, 1040, 1060, (150, 165, 178, 255), ol=None)
    # TV table, TV, doily, Rooh Afza, chai cups
    c.rect(540, 1130, 840, 1160, (110, 66, 36, 255))
    c.rect(555, 1160, 575, 1320, (90, 54, 30, 255))
    c.rect(805, 1160, 825, 1320, (90, 54, 30, 255))
    c.rrect(560, 975, 740, 1130, 10, (50, 50, 55, 255))
    c.rect(575, 990, 725, 1110, (25, 35, 45, 255), w=2)
    c.line([(590, 1000), (640, 1000)], (60, 90, 110, 255), 4)
    for i in range(9):  # lace doily scallops
        c.circ(575 + i * 19, 972, 11, (240, 236, 225, 255), w=1)
    c.rect(565, 962, 735, 975, (240, 236, 225, 255), ol=None)
    c.rrect(752, 1040, 786, 1130, 6, (190, 20, 40, 255))
    c.rect(760, 1015, 778, 1042, (190, 20, 40, 255), w=2)
    c.rect(755, 1070, 783, 1100, (250, 240, 200, 255), w=1)
    for cx in (800, 826):
        c.ell(cx - 14, 1122, cx + 14, 1132, (235, 235, 235, 255), w=2)
        c.rrect(cx - 10, 1100, cx + 10, 1126, 4, (245, 245, 245, 255), w=2)
        c.ell(cx - 8, 1100, cx + 8, 1106, (150, 95, 50, 255), ol=None)
    # warm bulb glow + vignette
    img = c.img
    glow = Image.new("RGBA", img.size, (0, 0, 0, 0))
    gd = ImageDraw.Draw(glow)
    gx, gy = c.p(170, 205)
    for r in range(900, 0, -30):
        a = int(70 * (1 - r / 900) ** 2)
        gd.ellipse([gx - r * S, gy - r * S, gx + r * S, gy + r * S], fill=(255, 190, 90, a))
    img.alpha_composite(glow)
    vig = Image.new("RGBA", img.size, (0, 0, 0, 0))
    vd = ImageDraw.Draw(vig)
    for i in range(40):
        a = int(4 * i)
        vd.rectangle([i * 12 * S // 2, i * 12 * S // 2, img.size[0] - i * 12 * S // 2, img.size[1] - i * 12 * S // 2],
                     outline=(5, 5, 20, max(0, 120 - a)), width=6 * S)
    img.alpha_composite(vig)
    c.save("bg_lounge")
    put("bg_lounge", 0, OY, W, OY + H, 0)


def moonbeam():
    x0, y0, x1, y1 = 330, 300, 900, 1500
    c = Cv(x1 - x0, y1 - y0, x0, y0)
    for i in range(60):
        t = i / 60
        a = int(55 * (1 - t))
        top_l, top_r = WINDOW[0] + 10, WINDOW[2] - 10
        bl, br = 360 + 0, 880
        yy = y0 + (y1 - y0) * t
        yy2 = y0 + (y1 - y0) * (t + 1 / 60)
        lx = top_l + (bl - top_l) * t
        rx = top_r + (br - top_r) * t
        lx2 = top_l + (bl - top_l) * (t + 1 / 60)
        rx2 = top_r + (br - top_r) * (t + 1 / 60)
        c.poly([(lx, yy), (rx, yy), (rx2, yy2), (lx2, yy2)], (130, 170, 255, a), ol=None)
    c.img = c.img.filter(ImageFilter.GaussianBlur(10 * S))
    c.save("moonbeam")
    put("moonbeam", x0, y0, x1, y1, 1)


def window_jali():
    x0, y0, x1, y1 = WINDOW[0] - 25, WINDOW[1] - 25, WINDOW[2] + 25, WINDOW[3] + 35
    c = Cv(x1 - x0, y1 - y0, x0, y0)
    wx0, wy0, wx1, wy1 = WINDOW
    wood = (120, 72, 38, 255)
    # frame
    c.rect(x0, y0, x1, wy0, wood)
    c.rect(x0, wy1, x1, y1, wood)
    c.rect(x0, wy0, wx0, wy1, wood)
    c.rect(wx1, wy0, x1, wy1, wood)
    # jali lattice: star/diamond holes
    step = 50
    for gy in range(wy0, wy1 + 1, step):
        c.line([(wx0, gy), (wx1, gy)], wood, 7)
    for gx in range(wx0, wx1 + 1, step):
        c.line([(gx, wy0), (gx, wy1)], wood, 7)
    for gy in range(wy0, wy1, step):
        for gx in range(wx0, wx1, step):
            cx, cy = gx + step / 2, gy + step / 2
            c.poly([(cx, cy - 18), (cx + 18, cy), (cx, cy + 18), (cx - 18, cy)], None, ol=wood, w=4)
    c.line([(wx0 + (wx1 - wx0) / 2, wy0), (wx0 + (wx1 - wx0) / 2, wy1)], wood, 10)
    c.save("window_jali")
    put("window_jali", x0, y0, x1, y1, 6)


def curtains():
    x0, y0, x1, y1 = 330, 160, 750, 520
    c = Cv(x1 - x0, y1 - y0, x0, y0)
    c.rect(x0, y0, x1, y0 + 14, (90, 60, 40, 255))
    red, dark, gold = (140, 30, 60, 255), (100, 18, 42, 255), (220, 170, 60, 255)
    c.poly([(x0, y0 + 10), (x0 + 120, y0 + 10), (x0 + 70, y1), (x0, y1)], red)
    c.poly([(x1, y0 + 10), (x1 - 120, y0 + 10), (x1 - 70, y1), (x1, y1)], red)
    for i in range(4):
        c.line([(x0 + 20 + i * 25, y0 + 15), (x0 + 15 + i * 13, y1 - 5)], dark, 4)
        c.line([(x1 - 20 - i * 25, y0 + 15), (x1 - 15 - i * 13, y1 - 5)], dark, 4)
    c.line([(x0 + 120, y0 + 12), (x0 + 70, y1)], gold, 6)
    c.line([(x1 - 120, y0 + 12), (x1 - 70, y1)], gold, 6)
    c.save("curtains")
    put("curtains", x0, y0, x1, y1, 7)


def door_panel():
    x0, y0, x1, y1 = KITCHEN
    c = Cv(x1 - x0, y1 - y0, x0, y0)
    c.rect(x0, y0, x1, y1, (122, 74, 40, 255), w=4)
    for (a, b, cc, dd) in ((x0 + 25, y0 + 30, x1 - 25, y0 + 200), (x0 + 25, y0 + 240, x1 - 25, y1 - 30)):
        c.rect(a, b, cc, dd, (140, 88, 50, 255), w=3)
        c.rect(a + 12, b + 12, cc - 12, dd - 12, (110, 66, 36, 255), ol=None)
    c.circ(x1 - 35, y0 + 230, 9, (220, 190, 80, 255), w=2)
    c.save("door_panel")
    put("door_kitchen", *KITCHEN, 3)
    put("door_bedroom", *BEDROOM, 3)
    put("door_hall", *HALL, 3)


def light_strip():
    w, h = 256, 64
    c = Cv(w, h)
    for y in range(h):
        t = y / h
        a = int(230 * (t ** 1.5))
        c.rect(0, y, w, y + 1, (255, 210, 120, a), ol=None)
    c.img = c.img.filter(ImageFilter.GaussianBlur(6 * S))
    # fade edges
    im = c.save("light_strip")
    for name, (x0, y0, x1, y1) in (("strip_kitchen", KITCHEN), ("strip_bedroom", BEDROOM), ("strip_hall", HALL)):
        put(name, x0, y1 - 46, x1, y1 + 6, 2)
    put("strip_window", WINDOW[0], WINDOW[3] + 10, WINDOW[2], WINDOW[3] + 50, 8)
    put("strip_sofa", SOFA[0] + 40, SOFA[3] - 30, SOFA[2] + 40, SOFA[3] + 20, 9)


def sofa():
    x0, y0, x1, y1 = SOFA
    c = Cv(x1 - x0, y1 - y0, x0, y0)
    must, maroon, wood = (201, 147, 46, 255), (130, 30, 50, 255), (90, 54, 30, 255)
    c.rrect(x0 + 10, y0 + 10, x1 - 10, y0 + 200, 40, must)               # backrest
    for i in range(3):
        cx = x0 + 75 + i * 125
        c.circ(cx, y0 + 90, 8, (160, 110, 30, 255), w=2)
    c.rrect(x0, y0 + 150, x0 + 70, y1 - 60, 25, must)                    # arms
    c.rrect(x1 - 70, y0 + 150, x1, y1 - 60, 25, must)
    c.rrect(x0 + 55, y0 + 170, x1 - 55, y1 - 70, 18, (215, 160, 60, 255))   # seat
    c.rrect(x0 + 70, y0 + 120, x0 + 170, y0 + 210, 20, maroon)          # cushions
    c.rrect(x1 - 170, y0 + 120, x1 - 70, y0 + 210, 20, maroon)
    c.rect(x0 + 30, y1 - 70, x1 - 30, y1 - 40, (170, 120, 40, 255))
    for lx in (x0 + 40, x1 - 60):
        c.rect(lx, y1 - 42, lx + 20, y1 - 2, wood)
    c.save("sofa")
    put("sofa", x0, y0, x1, y1, 8)


def charpai():
    x0, y0, x1, y1 = 0, 1420, 660, 1910
    c = Cv(x1 - x0, y1 - y0, x0, y0)
    wood, rope = (150, 95, 45, 255), (210, 185, 130, 255)
    # legs
    for lx in (30, 590):
        c.rrect(lx, 1500, lx + 40, 1905, 10, wood)
        c.ell(lx - 8, 1640, lx + 48, 1690, (170, 110, 55, 255))
    # woven bed surface
    c.rect(20, 1450, 640, 1545, rope)
    for i in range(-10, 40):
        c.line([(20 + i * 22, 1450), (20 + i * 22 + 95, 1545)], (175, 145, 95, 255), 3)
        c.line([(20 + i * 22 + 95, 1450), (20 + i * 22, 1545)], (190, 160, 105, 255), 3)
    c.rect(x0, 1450, x1, 1545, None) if False else None
    c.rrect(10, 1438, 650, 1462, 8, wood)   # top rail
    c.rrect(10, 1535, 650, 1558, 8, wood)   # bottom rail
    # rilli quilt draped in front
    cols = [(200, 40, 60, 255), (240, 190, 50, 255), (30, 110, 120, 255), (245, 240, 230, 255), (30, 40, 90, 255)]
    qx0, qy0, qx1, qy1 = 40, 1550, 620, 1870
    c.rect(qx0, qy0, qx1, qy1, cols[0], w=3)
    sq = 58
    for r, yy in enumerate(range(qy0 + 10, qy1 - 10, sq)):
        for k, xx in enumerate(range(qx0 + 10, qx1 - 10, sq)):
            col = cols[(r + k) % len(cols)]
            c.rect(xx, yy, min(xx + sq - 6, qx1 - 10), min(yy + sq - 6, qy1 - 10), col, ol=None)
            cx, cy = xx + sq / 2 - 3, yy + sq / 2 - 3
            c.poly([(cx, cy - 14), (cx + 14, cy), (cx, cy + 14), (cx - 14, cy)], cols[(r + k + 2) % len(cols)], ol=None)
    for xx in range(qx0, qx1 + 1, 20):
        c.poly([(xx, qy1), (xx + 10, qy1 + 18), (xx + 20, qy1)], cols[1], ol=None)
    c.save("charpai")
    put("charpai", x0, y0, x1, y1, 20)


def stool_box():
    # stool (peerhi)
    x0, y0, x1, y1 = 680, 1600, 990, 1890
    c = Cv(x1 - x0, y1 - y0, x0, y0)
    wood = (140, 85, 40, 255)
    c.rect(700, 1650, 725, 1885, wood)
    c.rect(945, 1650, 970, 1885, wood)
    c.rrect(685, 1605, 985, 1660, 8, (165, 105, 52, 255))
    c.rect(700, 1780, 970, 1800, wood)
    c.save("stool")
    put("stool", x0, y0, x1, y1, 21)
    # box body (front) with open top full of cookies
    x0, y0, x1, y1 = 650, 1340, 1010, 1625
    c = Cv(x1 - x0, y1 - y0, x0, y0)
    c.poly([(670, 1390), (990, 1390), (1000, 1420), (660, 1420)], (120, 30, 30, 255))  # inner back rim
    for i in range(14):  # cookies peeking out of the open top
        cx = 690 + (i % 7) * 46 + (12 if i >= 7 else 0)
        cy = 1395 - (12 if i >= 7 else 0)
        c.circ(cx, cy, 26, (196, 140, 70, 255), w=3)
        for _ in range(4):
            c.circ(cx + random.uniform(-14, 14), cy + random.uniform(-12, 12), 4, (70, 40, 20, 255), ol=None)
    c.rrect(655, 1405, 1005, 1620, 10, (215, 45, 50, 255), w=4)
    c.rect(655, 1405, 1005, 1440, (245, 200, 60, 255), w=3)
    c.rrect(690, 1460, 970, 1585, 14, (255, 245, 225, 255), w=3)   # label panel (Urdu text is drawn by UI)
    for k in range(6):
        c.circ(700 + k * 53, 1600, 7, (245, 200, 60, 255), ol=None)
    c.save("cookie_box")
    put("cookie_box", x0, y0, x1, y1, 26)
    put("box_label", 690, 1460, 970, 1585, 0)
    # lid hinged at the back, opened upward
    x0, y0, x1, y1 = 660, 1200, 1000, 1400
    c = Cv(x1 - x0, y1 - y0, x0, y0)
    c.poly([(670, 1395), (990, 1395), (975, 1215), (685, 1215)], (200, 40, 45, 255), w=4)
    c.poly([(690, 1380), (970, 1380), (958, 1232), (700, 1232)], (245, 200, 60, 255), w=2)
    c.circ(830, 1305, 40, (196, 140, 70, 255), w=3)
    c.save("box_lid")
    put("box_lid", x0, y0, x1, y1, 19)


SKIN, SKIN_D = (196, 140, 100, 255), (160, 105, 72, 255)
HAIR = (20, 16, 18, 255)


def bacha():
    # EATING: sitting on charpai, hand in the box, cheeks full
    x0, y0, x1, y1 = 290, 1060, 780, 1610
    c = Cv(x1 - x0, y1 - y0, x0, y0)
    green, green_d = (40, 150, 70, 255), (25, 110, 50, 255)
    # legs dangling + chappals
    for lx in (395, 470):
        c.rrect(lx, 1440, lx + 50, 1570, 16, (220, 220, 210, 255))
        c.ell(lx - 8, 1560, lx + 60, 1590, (40, 90, 170, 255))
        c.rrect(lx + 6, 1550, lx + 44, 1575, 8, SKIN)
    # torso
    c.rrect(370, 1270, 560, 1470, 40, green)
    c.line([(465, 1280), (465, 1330)], green_d, 4)
    c.rect(372, 1380, 558, 1400, (255, 255, 255, 255), ol=None)
    # reaching arm to the box
    c.poly([(530, 1300), (700, 1380), (690, 1420), (520, 1350)], green)
    c.circ(715, 1405, 26, SKIN)
    # other arm (holding cookie to mouth)
    c.poly([(390, 1300), (350, 1360), (400, 1250), (420, 1280)], green)
    c.circ(400, 1235, 22, SKIN)
    c.circ(408, 1222, 16, (196, 140, 70, 255), w=2)
    # head
    c.ell(380, 1090, 560, 1280, SKIN, w=4)
    c.circ(395, 1225, 32, SKIN_D, ol=OUTLINE, w=3)   # puffed cheeks
    c.circ(545, 1225, 32, SKIN_D, ol=OUTLINE, w=3)
    c.ell(380, 1090, 560, 1280, None, w=4)
    # messy hair
    pts = [(372, 1170), (360, 1110), (395, 1120), (390, 1070), (430, 1100), (445, 1050), (475, 1095),
           (505, 1055), (515, 1100), (560, 1075), (550, 1125), (580, 1140), (565, 1170), (470, 1140)]
    c.poly(pts, HAIR, w=3)
    # big guilty eyes (looking sideways)
    for ex in (430, 505):
        c.ell(ex - 26, 1160, ex + 26, 1218, (255, 255, 255, 255), w=3)
        c.circ(ex - 10, 1192, 13, (30, 20, 20, 255), ol=None)
        c.circ(ex - 14, 1186, 4, (255, 255, 255, 255), ol=None)
    c.line([(400, 1150), (450, 1158)], HAIR, 5)
    c.line([(485, 1158), (535, 1150)], HAIR, 5)
    # full mouth with crumbs
    c.ell(445, 1238, 495, 1262, (120, 40, 40, 255), w=3)
    for k in range(5):
        c.circ(445 + k * 12, 1272 + (k % 2) * 6, 3, (196, 140, 70, 255), ol=None)
    c.save("bacha_eat")
    put("bacha_eat", x0, y0, x1, y1, 25)

    # HIDING: only the hair tuft peeks above the charpai rail
    x0, y0, x1, y1 = 370, 1380, 560, 1480
    c = Cv(x1 - x0, y1 - y0, x0, y0)
    c.ell(395, 1430, 535, 1520, SKIN, w=3)
    pts = [(385, 1460), (378, 1415), (405, 1425), (410, 1385), (440, 1415), (460, 1382), (478, 1412),
           (505, 1388), (510, 1420), (545, 1410), (540, 1460)]
    c.poly(pts, HAIR, w=3)
    c.save("bacha_hide")
    put("bacha_hide", x0, y0, x1, y1, 15)


def dadi():
    # base sprite drawn in a local 260x460 box; placed per spawn by the builder
    W, H = 260, 460
    c = Cv(W, H)
    white, print_c, dots = (245, 245, 240, 255), (130, 40, 80, 255), (240, 200, 90, 255)
    # shalwar + feet
    c.rect(85, 380, 175, 445, (235, 235, 225, 255))
    c.ell(70, 438, 125, 458, (110, 70, 40, 255))
    c.ell(135, 438, 190, 458, (110, 70, 40, 255))
    # kameez (printed)
    c.poly([(70, 170), (190, 170), (215, 400), (45, 400)], print_c, w=3)
    for yy in range(190, 395, 28):
        for xx in range(60, 210, 28):
            c.circ(xx + (yy // 28 % 2) * 14, yy, 4, dots, ol=None)
    # tasbih hand
    c.poly([(70, 190), (40, 300), (62, 305), (90, 210)], print_c)
    c.circ(52, 312, 16, SKIN)
    for k in range(9):
        a = k / 9 * math.pi * 2
        c.circ(52 + 20 * math.cos(a), 340 + 20 * math.sin(a), 5, (30, 120, 90, 255), w=1)
    # raised arm holding chappal
    c.poly([(185, 185), (235, 90), (255, 100), (205, 205)], print_c)
    c.circ(244, 85, 17, SKIN)
    c.rrect(220, 5, 265, 85, 18, (40, 90, 170, 255), w=3)
    c.line([(228, 50), (258, 50)], (230, 230, 230, 255), 5)
    # head + dupatta
    c.poly([(60, 60), (200, 60), (215, 230), (45, 230)], white, w=3)   # dupatta drape
    c.ell(80, 45, 180, 165, SKIN, w=3)
    c.poly([(70, 95), (90, 25), (170, 25), (190, 95), (175, 60), (85, 60)], white, w=3)
    # thick glasses, wide eyes
    for ex in (108, 152):
        c.circ(ex, 105, 22, (255, 255, 255, 255), w=6)
        c.circ(ex, 105, 9, (20, 20, 20, 255), ol=None)
        c.circ(ex - 3, 101, 3, (255, 255, 255, 255), ol=None)
    c.line([(130, 105), (130, 105)], OUTLINE, 6)
    c.line([(88, 80), (125, 88)], (200, 200, 200, 255), 5)
    c.line([(135, 88), (172, 80)], (200, 200, 200, 255), 5)
    c.ell(115, 135, 145, 150, (120, 40, 40, 255), w=2)   # open mouth
    c.line([(95, 125), (100, 130)], SKIN_D, 2)
    c.line([(165, 125), (160, 130)], SKIN_D, 2)
    im = c.save("dadi")
    # bust (head + shoulders) for the window spawn
    bust = im.crop((0, 0, W, 230))
    bust.save(os.path.join(OUT, "dadi_bust.png"))
    layout["dadi_size"] = {"w": W / 100, "h": H / 100}


def chappal():
    W, H = 220, 320
    c = Cv(W, H)
    c.rrect(30, 10, 190, 310, 75, (40, 90, 170, 255), w=5)
    c.rrect(50, 30, 170, 290, 60, (60, 120, 200, 255), ol=None)
    c.line([(60, 90), (110, 150), (160, 90)], (240, 240, 240, 255), 18)
    c.circ(110, 150, 10, (240, 240, 240, 255), w=2)
    c.save("chappal")


def crack():
    W, H = 1080, 1920
    c = Cv(W, H)
    cx, cy = 560, 900
    rnd = random.Random(3)
    for k in range(16):
        ang = k / 16 * math.pi * 2 + rnd.uniform(-0.15, 0.15)
        x, y = cx, cy
        pts = [(x, y)]
        length = rnd.uniform(500, 1300)
        steps = 12
        for s in range(steps):
            ang += rnd.uniform(-0.3, 0.3)
            x += math.cos(ang) * length / steps
            y += math.sin(ang) * length / steps
            pts.append((x, y))
        c.line(pts, (255, 255, 255, 230), 5)
        c.line(pts, (40, 40, 50, 160), 2)
    for r in (60, 130, 230):
        ring = []
        for k in range(24):
            a = k / 24 * math.pi * 2
            rr = r * rnd.uniform(0.8, 1.2)
            ring.append((cx + math.cos(a) * rr, cy + math.sin(a) * rr))
        c.line(ring + [ring[0]], (255, 255, 255, 200), 3)
    c.circ(cx, cy, 40, (255, 255, 255, 120), ol=None)
    c.save("crack")


def truck_frame():
    """9-slice frame in truck-art style for UI panels (slice 40px)."""
    W = H = 160
    c = Cv(W, H)
    c.rrect(0, 0, W, H, 18, (230, 40, 90, 255), ol=None)
    cols = [(255, 210, 40, 255), (30, 170, 120, 255), (40, 120, 230, 255), (250, 120, 30, 255)]
    k = 0
    for x in range(8, W - 8, 16):
        c.poly([(x, 6), (x + 8, 20), (x + 16, 6)], cols[k % 4], ol=None)
        c.poly([(x, H - 6), (x + 8, H - 20), (x + 16, H - 6)], cols[k % 4], ol=None)
        k += 1
    for y in range(8, H - 8, 16):
        c.poly([(6, y), (20, y + 8), (6, y + 16)], cols[k % 4], ol=None)
        c.poly([(W - 6, y), (W - 20, y + 8), (W - 6, y + 16)], cols[k % 4], ol=None)
        k += 1
    c.rrect(26, 26, W - 26, H - 26, 10, (255, 255, 255, 255), ol=None)
    c.rrect(30, 30, W - 30, H - 30, 8, (25, 20, 45, 255), ol=None)
    for (x, y) in ((13, 13), (W - 13, 13), (13, H - 13), (W - 13, H - 13)):
        c.circ(x, y, 9, (255, 230, 60, 255), ol=(120, 20, 50, 255), w=2)
    c.save("truck_frame")


def bubble():
    W, H = 160, 160
    c = Cv(W, H)
    c.rrect(4, 4, W - 4, H - 4, 40, (255, 250, 235, 255), ol=(60, 20, 40, 255), w=6)
    c.save("bubble")


def chotu():
    """Little brother in striped pajamas with a lollipop; drawn 170x300, feet at the bottom."""
    W, H = 170, 300
    c = Cv(W, H)
    stripe_a, stripe_b = (90, 140, 220, 255), (200, 220, 250, 255)
    c.ell(45, 285, 80, 298, (200, 60, 60, 255), w=2)
    c.ell(90, 285, 125, 298, (200, 60, 60, 255), w=2)
    c.rrect(45, 130, 125, 290, 22, stripe_a, w=3)
    for y in range(140, 285, 18):
        c.rect(48, y, 122, y + 8, stripe_b, ol=None)
    c.rrect(45, 130, 125, 290, 22, None, w=3)
    c.poly([(120, 150), (150, 110), (160, 118), (128, 165)], stripe_a)   # arm up holding lollipop
    c.circ(155, 108, 10, SKIN)
    c.line([(155, 105), (155, 60)], (240, 240, 240, 255), 4)
    c.circ(155, 50, 18, (240, 60, 140, 255), w=3)
    c.line([(145, 45), (165, 55)], (255, 255, 255, 255), 3)
    c.ell(35, 25, 135, 140, SKIN, w=3)                          # head
    c.poly([(35, 75), (45, 25), (85, 10), (125, 25), (135, 75), (110, 45), (60, 45)], HAIR, w=3)
    for ex in (68, 102):                                         # big curious eyes
        c.ell(ex - 15, 70, ex + 15, 104, (255, 255, 255, 255), w=3)
        c.circ(ex, 90, 8, (30, 20, 20, 255), ol=None)
        c.circ(ex - 3, 86, 3, (255, 255, 255, 255), ol=None)
    c.ell(75, 115, 95, 128, (150, 50, 60, 255), w=2)              # "ooh" mouth
    c.save("chotu")


def golden_cookie():
    W = H = 160
    c = Cv(W, H)
    for k in range(12):
        a = k / 12 * math.pi * 2
        c.poly([(80, 80), (80 + 78 * math.cos(a - 0.12), 80 + 78 * math.sin(a - 0.12)),
                (80 + 78 * math.cos(a + 0.12), 80 + 78 * math.sin(a + 0.12))], (255, 230, 120, 140), ol=None)
    c.circ(80, 80, 42, (255, 200, 40, 255), ol=(150, 90, 10, 255), w=4)
    c.circ(80, 80, 32, (255, 220, 90, 255), ol=None)
    for (x, y) in ((68, 70), (92, 74), (78, 92), (95, 92), (65, 88)):
        c.circ(x, y, 5, (190, 120, 20, 255), ol=None)
    c.circ(66, 64, 6, (255, 255, 230, 255), ol=None)
    c.save("golden_cookie")
    put("golden_cookie", 760, 1255, 920, 1415, 27)


def vignette():
    """Red glow around the screen edges, used while Dadi is watching."""
    W, H = 270, 480
    im = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    px = im.load()
    for y in range(H):
        for x in range(W):
            dx = min(x, W - 1 - x) / (W * 0.5)
            dy = min(y, H - 1 - y) / (H * 0.5)
            d = min(dx, dy)
            a = max(0.0, 1 - d / 0.35)
            px[x, y] = (200, 20, 40, int(200 * a ** 2))
    im.save(os.path.join(OUT, "vignette.png"))


def icon_steps():
    W = H = 128
    c = Cv(W, H)
    white = (255, 255, 255, 255)
    for (x, y) in ((40, 80), (88, 44)):
        c.ell(x - 13, y - 14, x + 13, y + 30, white, ol=None)            # sole
        for k, (tx, tr) in enumerate(((-11, 5), (-3, 5), (5, 4.5), (12, 4))):
            c.circ(x + tx, y - 22 + abs(tx) * 0.4, tr, white, ol=None)  # toes
    c.save("icon_steps")


if __name__ == "__main__":
    background(); moonbeam(); window_jali(); curtains(); door_panel(); light_strip()
    sofa(); charpai(); stool_box(); bacha(); dadi(); chappal(); crack(); truck_frame(); bubble()
    chotu(); golden_cookie(); vignette(); icon_steps()
    # Dadi spawn placements: (centre x, feet y, scale, sprite, order)
    spawns = {
        "kitchen": {"cx": 170, "feet": FLOOR + 4, "scale": 1.0, "sprite": "dadi", "order": 4, "pan": -0.9, "tap": KITCHEN},
        "bedroom": {"cx": 910, "feet": FLOOR + 4, "scale": 1.0, "sprite": "dadi", "order": 4, "pan": 0.9, "tap": BEDROOM},
        "hall": {"cx": 540, "feet": FLOOR + 4, "scale": 0.74, "sprite": "dadi", "order": 4, "pan": 0.0, "tap": HALL},
        "window": {"cx": 560, "feet": WINDOW[3] + 30, "scale": 1.0, "sprite": "dadi_bust", "order": 5, "pan": 0.15, "tap": WINDOW},
        "sofa": {"cx": 230, "feet": 1160, "scale": 1.0, "sprite": "dadi", "order": 5, "pan": -0.5, "tap": SOFA},
    }
    for k, s in spawns.items():
        x0, y0, x1, y1 = s["tap"]
        s["tapRect"] = {"x": ((x0 + x1) / 2 - 540) / 100, "y": (960 - (y0 + y1) / 2) / 100,
                        "w": (x1 - x0) / 100, "h": (y1 - y0) / 100}
        s["feetWorld"] = {"x": (s["cx"] - 540) / 100, "y": (960 - s["feet"]) / 100}
        del s["tap"]
    layout["spawns"] = spawns
    with open(os.path.join(os.path.dirname(__file__), "layout.json"), "w") as f:
        json.dump(layout, f, indent=1)
    # Same data in a simple text format for AgentScripts/BuildScene.cs.
    unity = {"sprites": [], "spawns": []}
    for k, v in layout.items():
        if k in ("spawns", "dadi_size"):
            continue
        unity["sprites"].append(dict(name=k, **v))
    unlock = {"kitchen": 1, "bedroom": 1, "hall": 1, "window": 5, "sofa": 7}   # night the spawn unlocks
    kind = {"kitchen": "Door", "bedroom": "Door", "hall": "Door", "window": "Window", "sofa": "Sofa"}
    for k, s in spawns.items():
        unity["spawns"].append({"id": k, "kind": kind[k], "unlockAt": unlock[k], "pan": s["pan"], "scale": s["scale"],
                                "sprite": s["sprite"], "order": s["order"], "feetX": s["feetWorld"]["x"],
                                "feetY": s["feetWorld"]["y"], "tapX": s["tapRect"]["x"], "tapY": s["tapRect"]["y"],
                                "tapW": s["tapRect"]["w"], "tapH": s["tapRect"]["h"]})
    # Plain text, one item per line: "S name x y w h order" and "P id kind sprite unlockAt pan scale feetX feetY tapX tapY tapW tapH order"
    with open(os.path.join(os.path.dirname(__file__), "layout_unity.txt"), "w") as f:
        for v in unity["sprites"]:
            f.write("S {name} {x} {y} {w} {h} {order}\n".format(**v))
        for v in unity["spawns"]:
            f.write("P {id} {kind} {sprite} {unlockAt} {pan} {scale} {feetX} {feetY} {tapX} {tapY} {tapW} {tapH} {order}\n".format(**v))
    print("ok", sorted(os.listdir(OUT)))
