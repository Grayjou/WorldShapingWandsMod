# Geometric Icons & Screenshot Overlay Tools

Utilities for visual QA of mod sprites composited over a 1440p game screenshot.

## screenshot_overlay.py

```text
python screenshot_overlay.py                          # Default overlay layout
python screenshot_overlay.py --config overlay.json    # Custom layout from JSON
python screenshot_overlay.py --icons-only             # Only overlay shape icons
python screenshot_overlay.py --projectiles-only       # Only overlay WandAction projectiles
python screenshot_overlay.py --scale 2                # 2× nearest-neighbor upscale
python screenshot_overlay.py --grid                   # Arrange all sprites in a grid
```

### WandAction modes

The WandAction sprite sheets contain four frames per sheet, one per
interaction mode. Use `--mode` to pick which frame the projectile grid
samples from each sheet:

```text
python screenshot_overlay.py                          # Instant only (default, back-compat)
python screenshot_overlay.py --mode select            # → overlay_projectiles_select.png
python screenshot_overlay.py --mode confirm           # → overlay_projectiles_confirm.png
python screenshot_overlay.py --mode stamp             # → overlay_projectiles_stamp.png
python screenshot_overlay.py --mode all               # All four files in one run
```

| Frame | Mode    | Output suffix                |
| ----- | ------- | ---------------------------- |
| 0     | instant | *(no suffix — bare filename)* |
| 1     | select  | `_select`                    |
| 2     | confirm | `_confirm`                   |
| 3     | stamp   | `_stamp`                     |

`instant` keeps the bare `overlay_projectiles.png` filename so existing
documentation links and the showcase-script b-roll references do not break.

A sheet that is not exactly 4 frames wide will fail loud rather than silently
slicing the wrong frame.

Outputs land in `Scripts/GeometricIcons/output/`.

### Design references

- `dev_notes/inbox/Cavendish 2026-04-19_Session_1/Patch_screenshot_overlay_modes.md`
