"""Composites the generated sprites using layout.json for a quick visual check."""
import json, os, sys
from PIL import Image
ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
A = os.path.join(ROOT, "Assets", "Art")
L = json.load(open(os.path.join(os.path.dirname(__file__), "layout.json")))
spawn = sys.argv[1] if len(sys.argv) > 1 else "kitchen"
mode = sys.argv[2] if len(sys.argv) > 2 else "eat"
canvas = Image.new("RGBA", (1080, 1920), (0, 0, 0, 255))
def place(img, name_or_rect, order):
    r = L[name_or_rect] if isinstance(name_or_rect, str) else name_or_rect
    w, h = int(r["w"] * 100), int(r["h"] * 100)
    im = img.resize((w, h)) if img.size != (w, h) else img
    x = int(r["x"] * 100 + 540 - w / 2); y = int(960 - r["y"] * 100 - h / 2)
    items.append((order, im, x, y))
items = []
def spr(n): return Image.open(os.path.join(A, n + ".png")).convert("RGBA")
door = spr("door_panel")
for n in ["bg_lounge", "moonbeam", "window_jali", "curtains", "sofa", "charpai", "stool", "cookie_box", "box_lid"]:
    place(spr(n), n, L[n]["order"])
place(spr("bacha_eat" if mode == "eat" else "bacha_hide"), "bacha_eat" if mode == "eat" else "bacha_hide", 25 if mode == "eat" else 15)
s = L["spawns"][spawn]
d = spr(s["sprite"]); sc = s["scale"]
w, h = d.size[0] * sc / 100, d.size[1] * sc / 100
place(d, {"x": s["feetWorld"]["x"], "y": s["feetWorld"]["y"] + h / 2, "w": w, "h": h}, s["order"])
place(spr("light_strip"), "strip_" + spawn, L["strip_" + spawn]["order"])
if mode == "door": place(door, "door_" + spawn, 3)
for o, im, x, y in sorted(items, key=lambda t: t[0]):
    canvas.alpha_composite(im, (x, y)) if x >= 0 and y >= 0 else canvas.paste(im, (x, y), im)
canvas.convert("RGB").resize((540, 960)).save(os.path.join(ROOT, "AgentScripts", "shots", f"preview_{spawn}_{mode}.png"))
