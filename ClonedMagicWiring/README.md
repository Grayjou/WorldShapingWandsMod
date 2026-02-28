# MagicWiring

A Terraria tModLoader mod that adds a magical **Wand of Wiring** tool for efficient placement and removal of wires and actuators.

## ✨ Features

- **Shape-Based Wiring**: Place wires and actuators in various shapes:
  - Rectangles (filled and hollow)
  - Diamonds (filled and hollow)
  - Triangles (filled and hollow)
  - Wire Kite (L-shaped vanilla pattern)
- **Multiple Modes**: Switch between placing and removing wires
- **All Wire Types**: Supports red, green, blue, and yellow wires plus actuators
- **Visual Preview**: See exactly which tiles will be affected before you commit
- **Settings Panel**: Easy-to-use UI for configuring your wiring operations
- **Multiplayer Compatible**: Works seamlessly in multiplayer environments

## 📦 Installation

### Via Steam Workshop (Recommended)
1. Subscribe to the mod on the Steam Workshop
2. Launch Terraria with tModLoader
3. Enable MagicWiring in the Mods menu

### Manual Installation
1. Ensure [tModLoader](https://github.com/tModLoader/tModLoader) is installed
2. Download the latest release from the [Releases page](../../releases)
3. Place the `.tmod` file in your Terraria Mods folder:
   - Windows: `%UserProfile%/Documents/My Games/Terraria/tModLoader/Mods`
   - Mac: `~/Library/Application Support/Terraria/tModLoader/Mods`
   - Linux: `~/.local/share/Terraria/tModLoader/Mods`
4. Enable the mod in tModLoader's Mods menu

## 🎮 How to Use

### Getting the Wand
Craft the Wand of Wiring at a Tinkerer's Workbench with:
- 1 Wire Kite
- 50 Wire
- 10 Actuators

### Using the Wand

**Basic Operation:**
1. Equip the Wand of Wiring
2. **Right-click** to open the settings panel
3. Configure your settings:
   - Choose **Place** or **Remove** mode
   - Select your wire type (Red/Green/Blue/Yellow) or Actuator
   - Pick a shape (Rectangle, Diamond, Triangle, Wire Kite)
   - Choose filled or hollow variant
4. **Left-click and drag** to select an area
5. Release to apply the wiring operation

**Tips:**
- The wand automatically shows wires when held
- A green overlay indicates placement mode
- A red overlay indicates removal mode
- Orange pulsing means you've hit the maximum distance limit
- Triangle orientation depends on your drag direction

## ⚙️ Configuration

The mod includes a configuration menu accessible from tModLoader's Mod Configuration menu:
- **Max Wiring Distance**: Limit the maximum area size (default: 200 tiles)
- **Interaction Mode**: Choose between Hold (drag) or Toggle (click-click) modes

## 🔧 Development

This mod is open-source! Contributions are welcome.

- **Built with**: tModLoader for Terraria 1.4+
- **Framework**: .NET 8.0
- **License**: See [LICENSE](LICENSE) file

### Building from Source
1. Clone this repository
2. Ensure .NET 8.0 SDK is installed
3. Place the repository in your ModSources folder
4. Run `dotnet build` or build via tModLoader

## 🐛 Issues & Feedback

Found a bug or have a suggestion? Please [open an issue](../../issues) on GitHub!

## 📜 License

This project is licensed under the terms specified in the [LICENSE](LICENSE) file.

---

*Happy wiring!* ⚡
