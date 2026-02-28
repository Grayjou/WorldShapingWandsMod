import os
from PIL import Image
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

assets = [
    wand_of_destruction_instant_asset,
    wand_of_destruction_select_asset,
    wand_of_destruction_confirm_asset,
    wand_of_building_instant_asset,
    wand_of_building_select_asset,
    wand_of_building_confirm_asset,
    wand_of_replacement_instant_asset,
    wand_of_replacement_select_asset,
    wand_of_replacement_confirm_asset,
    wand_of_wiring_instant_asset,
    wand_of_wiring_select_asset,
    wand_of_wiring_confirm_asset
]

def generate_generic_assets_if_not_exists(assets: list[GenericAsset] = assets):
    for asset in assets:
        asset.generate()
    

if __name__ == "__main__":    generate_generic_assets_if_not_exists()