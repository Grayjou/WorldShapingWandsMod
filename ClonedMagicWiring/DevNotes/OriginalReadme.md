# MagicWiring Mod

A tModLoader mod that adds a Wand of Wiring tool for efficient placement and removal of wires and actuators in Terraria.

## Features

- Channeled wand item for placing/removing wires and actuators
- Supports rectangular and diamond shapes
- UI panel for settings (modes, shapes, wire types)
- Preview overlay showing affected tiles
- Multiplayer support

## Installation

1. Ensure tModLoader is installed for Terraria.
2. Place the mod files in your ModSources directory.
3. Build the mod using `dotnet build`.
4. The built mod will be in the bin/Debug/net8.0/ directory.
5. Copy the .tmod file to your Mods directory and enable it in tModLoader.

## Usage

- Craft or obtain the Wiring Wand item.
- Hold the wand and channel it to select an area.
- Use right-click to open the settings UI.
- Choose place/remove mode, shape (rectangle/diamond), and wire types/actuator.
- Release channel to apply the wiring operation.

## Assets Required

The mod requires the following PNG assets (32x32 for items/projectiles, 16x16 for UI icons):

- Content/Items/WiringWandItem.png (32x32)
- Content/Projectiles/WiringWandProjectile.png (16x16)
- UI/WireRed.png (16x16)
- UI/WireGreen.png (16x16)
- UI/WireBlue.png (16x16)
- UI/WireYellow.png (16x16)
- UI/Actuator.png (16x16)
- UI/Place.png (16x16)
- UI/Remove.png (16x16)
- UI/FilledRect.png (16x16)
- UI/HollowRect.png (16x16)
- UI/FilledDiamond.png (16x16)
- UI/HollowDiamond.png (16x16)

Without these assets, the mod will load but sprites will be missing.

## Troubleshooting

- If the mod doesn't compile, ensure .NET 8.0 SDK is installed.
- For runtime errors, check the tModLoader logs.
- In multiplayer, ensure all players have the mod installed.

## Development

- Built with tModLoader for Terraria 1.4+
- Target framework: .NET 8.0
- Uses Terraria APIs for tile manipulation and networking.