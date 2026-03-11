import os
from PIL import Image, ImageDraw
import numpy as np
from dataclasses import dataclass
from enum import Enum

class Color(Enum):
    RED = (255, 0, 0)
    GREEN = (0, 255, 0)
    BLUE = (0, 0, 255)
    PINK = (255, 180, 180)

@dataclass
class GenericAsset:
    path: str
    color: Color
    size: tuple[int, int]
    padding: int
    def generate(self):
        if not os.path.exists(self.path):
            print(f"Generating generic asset at {self.path} with color {self.color} and size {self.size} (padding={self.padding})")
            w, h = self.size
            p = self.padding
            my_size = (w + p * 2, h + p * 2)
            # RGBA array initialized to zeros -> fully transparent padding (alpha=0)
            my_arr = np.zeros((my_size[1], my_size[0], 4), dtype=np.uint8)
            r, g, b = self.color
            # Fill only the inner region with full alpha (255). No antialiasing, hard edges.
            my_arr[p:p + h, p:p + w] = (r, g, b, 255)
            dirpath = os.path.dirname(self.path)
            if dirpath:
                os.makedirs(dirpath, exist_ok=True)
            Image.fromarray(my_arr, 'RGBA').save(self.path)

@dataclass
class DrawnAsset:
    """Asset with a custom draw function instead of a solid fill."""
    path: str
    size: tuple[int, int]
    padding: int
    draw_func: object  # callable(draw: ImageDraw, w: int, h: int) -> None

    def generate(self):
        if not os.path.exists(self.path):
            w, h = self.size
            p = self.padding
            total = (w + p * 2, h + p * 2)
            img = Image.new('RGBA', total, (0, 0, 0, 0))
            # Create a sub-image for drawing within the padded region
            inner = Image.new('RGBA', (w, h), (0, 0, 0, 0))
            draw = ImageDraw.Draw(inner)
            self.draw_func(draw, w, h)
            img.paste(inner, (p, p))
            dirpath = os.path.dirname(self.path)
            if dirpath:
                os.makedirs(dirpath, exist_ok=True)
            img.save(self.path)
            print(f"Generating drawn asset at {self.path} ({total[0]}x{total[1]})")

@dataclass
class WandAsset(GenericAsset):
    """Asset generator for wand items with shared parameters."""
    ITEMS_DIR = r"Content\Items"
    
    def __init__(self, name: str, color):
        super().__init__(
            path=f"{self.ITEMS_DIR}\\{name}.png",
            color=color,
            size=(28, 28),
            padding=2
        )

@dataclass
class IconAsset(GenericAsset):
    """Asset generator for small UI icon buttons (slope selectors, etc.)."""
    ICONS_DIR = r"Assets\Icons"
    
    def __init__(self, name: str, color=(140, 140, 140)):
        super().__init__(
            path=f"{self.ICONS_DIR}\\{name}.png",
            color=color,
            size=(14, 14),
            padding=1
        )

# ====== Shape drawing helpers ======
C_FILL = (180, 180, 180, 255)
C_LINE = (200, 200, 200, 255)

def _shape_icon(name, draw_fn):
    return DrawnAsset(
        path=f"Assets\\Icons\\{name}.png",
        size=(14, 14),
        padding=1,
        draw_func=draw_fn
    )

def _draw_rect_filled(d, w, h):
    d.rectangle([1, 1, w-2, h-2], fill=C_FILL)

def _draw_rect_hollow(d, w, h):
    d.rectangle([1, 1, w-2, h-2], outline=C_LINE, width=2)

def _draw_ellipse_filled(d, w, h):
    d.ellipse([1, 1, w-2, h-2], fill=C_FILL)

def _draw_ellipse_hollow(d, w, h):
    d.ellipse([1, 1, w-2, h-2], outline=C_LINE, width=2)

def _draw_diamond_filled(d, w, h):
    cx, cy = w//2, h//2
    pts = [(cx, 1), (w-2, cy), (cx, h-2), (1, cy)]
    d.polygon(pts, fill=C_FILL)

def _draw_diamond_hollow(d, w, h):
    cx, cy = w//2, h//2
    pts = [(cx, 1), (w-2, cy), (cx, h-2), (1, cy)]
    d.polygon(pts, outline=C_LINE)
    # draw again slightly inset for visibility
    d.polygon(pts, outline=C_LINE)

def _draw_triangle_filled(d, w, h):
    pts = [(w//2, 1), (w-2, h-2), (1, h-2)]
    d.polygon(pts, fill=C_FILL)

def _draw_triangle_hollow(d, w, h):
    pts = [(w//2, 1), (w-2, h-2), (1, h-2)]
    d.polygon(pts, outline=C_LINE)

def _draw_edge(d, w, h):
    d.rectangle([1, 1, w-2, h-2], outline=C_LINE, width=1)

def _draw_cardinal(d, w, h):
    d.line([(1, h-2), (w-2, 1)], fill=C_LINE, width=2)

# ====== Object Type drawing helpers ======
C_OBJ = (200, 180, 140, 255)  # warm tan for object type icons

def _draw_obj_solid(d, w, h):
    """Solid block: filled square with subtle inner highlight."""
    d.rectangle([2, 2, w-3, h-3], fill=(139, 90, 43, 255))
    d.rectangle([3, 3, w-4, h-4], fill=(160, 110, 60, 255))

def _draw_obj_platform(d, w, h):
    """Platform: flat horizontal slab at top third."""
    d.rectangle([1, 3, w-2, 6], fill=(150, 111, 51, 255))
    # small support lines
    d.line([(3, 7), (3, h-2)], fill=(120, 90, 40, 255), width=1)
    d.line([(w-4, 7), (w-4, h-2)], fill=(120, 90, 40, 255), width=1)

def _draw_obj_rope(d, w, h):
    """Rope: vertical squiggly line."""
    cx = w // 2
    d.line([(cx, 1), (cx, h-2)], fill=(139, 119, 101, 255), width=2)
    # small knots
    d.rectangle([cx-1, 4, cx+1, 5], fill=(160, 140, 120, 255))
    d.rectangle([cx-1, 9, cx+1, 10], fill=(160, 140, 120, 255))

def _draw_obj_rail(d, w, h):
    """Rail: two horizontal lines with cross-ties."""
    d.line([(1, 4), (w-2, 4)], fill=(150, 150, 150, 255), width=1)
    d.line([(1, 9), (w-2, 9)], fill=(150, 150, 150, 255), width=1)
    for x in range(3, w-2, 3):
        d.line([(x, 4), (x, 9)], fill=(120, 120, 120, 255), width=1)

def _draw_obj_grass_seed(d, w, h):
    """Grass seed: small sprout/seedling icon."""
    cx = w // 2
    # stem
    d.line([(cx, h-3), (cx, 5)], fill=(0, 150, 0, 255), width=1)
    # leaves
    d.line([(cx, 6), (cx+2, 4)], fill=(0, 180, 0, 255), width=1)
    d.line([(cx, 7), (cx-2, 5)], fill=(0, 180, 0, 255), width=1)
    # seed base
    d.rectangle([cx-1, h-3, cx+1, h-2], fill=(120, 80, 40, 255))

def _draw_obj_planter(d, w, h):
    """Planter box: small pot shape."""
    # pot body (trapezoid approximation)
    d.rectangle([2, 3, w-3, 4], fill=(140, 100, 60, 255))  # rim
    d.polygon([(3, 5), (w-4, 5), (w-5, h-2), (4, h-2)], fill=(100, 70, 46, 255))

def _draw_obj_tile(d, w, h):
    """Tile (for replacement panel): solid brick with mortar lines."""
    d.rectangle([1, 1, w-2, h-2], fill=(150, 130, 110, 255))
    d.line([(1, h//2), (w-2, h//2)], fill=(120, 100, 80, 255), width=1)
    d.line([(w//2, 1), (w//2, h//2)], fill=(120, 100, 80, 255), width=1)
    d.line([(w//3, h//2), (w//3, h-2)], fill=(120, 100, 80, 255), width=1)

def _draw_obj_seeds(d, w, h):
    """Seeds: scattered dots."""
    dots = [(3,4), (7,3), (5,7), (9,8), (4,10), (8,11), (10,5)]
    for x, y in dots:
        if x < w-1 and y < h-1:
            d.point((x, y), fill=(0, 150, 0, 255))
            d.point((x+1, y), fill=(0, 130, 0, 255))

def _draw_obj_air(d, w, h):
    """Air: X mark (erase/empty)."""
    d.line([(3, 3), (w-4, h-4)], fill=(200, 200, 200, 255), width=2)
    d.line([(w-4, 3), (3, h-4)], fill=(200, 200, 200, 255), width=2)

def _draw_half_ellipse_h_filled(d, w, h):
    # Horizontal half-ellipse: flat top, curved bottom
    d.pieslice([1, -(h-2), w-2, h-2], start=0, end=180, fill=C_FILL)

def _draw_half_ellipse_h_hollow(d, w, h):
    d.pieslice([1, -(h-2), w-2, h-2], start=0, end=180, outline=C_LINE)
    d.line([(1, 1), (w-2, 1)], fill=C_LINE, width=1)

def _draw_half_ellipse_v_filled(d, w, h):
    # Vertical half-ellipse: flat left, curved right
    d.pieslice([1, 1, (w-2)*2, h-2], start=270, end=90, fill=C_FILL)

def _draw_half_ellipse_v_hollow(d, w, h):
    d.pieslice([1, 1, (w-2)*2, h-2], start=270, end=90, outline=C_LINE)
    d.line([(1, 1), (1, h-2)], fill=C_LINE, width=1)


# ====== Asset definitions ======

wand_of_destruction_instant_asset = WandAsset(
    name="WandOfDestructionInstant",
    color=Color.RED.value  # Red for instant mode
)

wand_of_destruction_select_asset = WandAsset(
    name="WandOfDestructionSelect", 
    color=(255, 100, 150)  # Middle point between red and pink
)

wand_of_destruction_confirm_asset = WandAsset(
    name="WandOfDestructionConfirm",
    color=Color.PINK.value  # Pink for confirm mode
)

wand_of_destruction_stamp_asset = WandAsset(
    name="WandOfDestructionStamp",
    color=(255, 150, 200)  # Lighter pink for stamp mode
)

wand_of_building_instant_asset = WandAsset(
    name="WandOfBuildingInstant",
    color=(170, 0, 255)  # Saturated fuchsia for instant mode
)

wand_of_building_select_asset = WandAsset(
    name="WandOfBuildingSelect", 
    color=(198, 83, 255)  # Milder fuchsia for select mode
)

wand_of_building_confirm_asset = WandAsset(
    name="WandOfBuildingConfirm",
    color=(255, 100, 100)  # Reddish for confirm mode
)

wand_of_building_stamp_asset = WandAsset(
    name="WandOfBuildingStamp",
    color=(255, 150, 150)  # Lighter red for stamp mode
)

wand_of_replacement_instant_asset = WandAsset(
    name="WandOfReplacementInstant",
    color=(180, 100, 255)
)
wand_of_replacement_select_asset = WandAsset(
    name="WandOfReplacementSelect", 
    color=(200, 150, 255)
)
wand_of_replacement_confirm_asset = WandAsset(
    name="WandOfReplacementConfirm",
    color=(220, 200, 255)
)

wand_of_replacement_stamp_asset = WandAsset(
    name="WandOfReplacementStamp",
    color=(230, 220, 255)
)

wand_of_wiring_instant_asset = WandAsset(
    name="WandOfWiringInstant",
    color=(255, 200, 50)  # Gold
)
wand_of_wiring_select_asset = WandAsset(
    name="WandOfWiringSelect", 
    color=(100, 200, 255)  # Light blue
)
wand_of_wiring_confirm_asset = WandAsset(
    name="WandOfWiringConfirm",
    color=(100, 255, 150)  # Mint
)

wand_of_wiring_stamp_asset = WandAsset(
    name="WandOfWiringStamp",
    color=(150, 255, 200)  # Light mint
)

wand_of_protection_instant_asset = WandAsset(
    name="WandOfProtectionInstant",
    color=(100, 80, 200)  # Deep purple for instant
)
wand_of_protection_select_asset = WandAsset(
    name="WandOfProtectionSelect",
    color=(130, 100, 220)  # Medium purple for select
)
wand_of_protection_confirm_asset = WandAsset(
    name="WandOfProtectionConfirm",
    color=(160, 130, 240)  # Light purple for confirm
)
wand_of_protection_stamp_asset = WandAsset(
    name="WandOfProtectionStamp",
    color=(180, 160, 255)  # Lightest purple for stamp
)

# === Slope icon assets (14x14, 1px padding, gray placeholder) ===
slope_default_icon = IconAsset(name="SlopeDefault")
slope_half_icon = IconAsset(name="SlopeHalf")
slope_bottom_right_icon = IconAsset(name="SlopeBottomRight")
slope_bottom_left_icon = IconAsset(name="SlopeBottomLeft")
slope_top_right_icon = IconAsset(name="SlopeTopRight")
slope_top_left_icon = IconAsset(name="SlopeTopLeft")

# === Misc UI icon assets ===
arrow_right_icon = IconAsset(name="ArrowRight", color=(200, 200, 200))

# === Shape icon assets (14x14 drawn, 1px padding) ===
shape_rect_filled   = _shape_icon("ShapeRectFilled",   _draw_rect_filled)
shape_rect_hollow   = _shape_icon("ShapeRectHollow",   _draw_rect_hollow)
shape_ellipse_filled = _shape_icon("ShapeEllipseFilled", _draw_ellipse_filled)
shape_ellipse_hollow = _shape_icon("ShapeEllipseHollow", _draw_ellipse_hollow)
shape_diamond_filled = _shape_icon("ShapeDiamondFilled", _draw_diamond_filled)
shape_diamond_hollow = _shape_icon("ShapeDiamondHollow", _draw_diamond_hollow)
shape_triangle_filled = _shape_icon("ShapeTriangleFilled", _draw_triangle_filled)
shape_triangle_hollow = _shape_icon("ShapeTriangleHollow", _draw_triangle_hollow)
shape_edge           = _shape_icon("ShapeEdge",           _draw_edge)
shape_cardinal       = _shape_icon("ShapeCardinal",       _draw_cardinal)
shape_half_ellipse_h_filled = _shape_icon("ShapeHalfEllipseHFilled", _draw_half_ellipse_h_filled)
shape_half_ellipse_h_hollow = _shape_icon("ShapeHalfEllipseHHollow", _draw_half_ellipse_h_hollow)
shape_half_ellipse_v_filled = _shape_icon("ShapeHalfEllipseVFilled", _draw_half_ellipse_v_filled)
shape_half_ellipse_v_hollow = _shape_icon("ShapeHalfEllipseVHollow", _draw_half_ellipse_v_hollow)

# === Object type icon assets (14x14 drawn, 1px padding) ===
def _obj_icon(name, draw_fn):
    return DrawnAsset(
        path=f"Assets\\Icons\\{name}.png",
        size=(14, 14),
        padding=1,
        draw_func=draw_fn
    )

obj_solid      = _obj_icon("ObjSolid",      _draw_obj_solid)
obj_platform   = _obj_icon("ObjPlatform",   _draw_obj_platform)
obj_rope       = _obj_icon("ObjRope",       _draw_obj_rope)
obj_rail       = _obj_icon("ObjRail",       _draw_obj_rail)
obj_grass_seed = _obj_icon("ObjGrassSeed",  _draw_obj_grass_seed)
obj_planter    = _obj_icon("ObjPlanter",    _draw_obj_planter)
obj_tile       = _obj_icon("ObjTile",       _draw_obj_tile)
obj_seeds      = _obj_icon("ObjSeeds",      _draw_obj_seeds)
obj_air        = _obj_icon("ObjAir",        _draw_obj_air)

assets = [
    wand_of_destruction_instant_asset,
    wand_of_destruction_select_asset,
    wand_of_destruction_confirm_asset,
    wand_of_destruction_stamp_asset,

    wand_of_building_instant_asset,
    wand_of_building_select_asset,
    wand_of_building_confirm_asset,
    wand_of_building_stamp_asset,

    wand_of_replacement_instant_asset,
    wand_of_replacement_select_asset,
    wand_of_replacement_confirm_asset,
    wand_of_replacement_stamp_asset,

    wand_of_wiring_instant_asset,
    wand_of_wiring_select_asset,
    wand_of_wiring_confirm_asset,
    wand_of_wiring_stamp_asset,

    wand_of_protection_instant_asset,
    wand_of_protection_select_asset,
    wand_of_protection_confirm_asset,
    wand_of_protection_stamp_asset,

    slope_default_icon,
    slope_half_icon,
    slope_bottom_right_icon,
    slope_bottom_left_icon,
    slope_top_right_icon,
    slope_top_left_icon,
    arrow_right_icon,
    # Shape icons
    shape_rect_filled,
    shape_rect_hollow,
    shape_ellipse_filled,
    shape_ellipse_hollow,
    shape_diamond_filled,
    shape_diamond_hollow,
    shape_triangle_filled,
    shape_triangle_hollow,
    shape_edge,
    shape_cardinal,
    shape_half_ellipse_h_filled,
    shape_half_ellipse_h_hollow,
    shape_half_ellipse_v_filled,
    shape_half_ellipse_v_hollow,
    # Object type icons
    obj_solid,
    obj_platform,
    obj_rope,
    obj_rail,
    obj_grass_seed,
    obj_planter,
    obj_tile,
    obj_seeds,
    obj_air,
]

def generate_generic_assets_if_not_exists(assets=assets):
    for asset in assets:
        asset.generate()
    

if __name__ == "__main__":    generate_generic_assets_if_not_exists()