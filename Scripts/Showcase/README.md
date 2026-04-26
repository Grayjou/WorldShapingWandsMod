# Scripts/Showcase

Automation for marketing & guide imagery. Both scripts are **idempotent** and
**non-destructive** — they only ever (over)write inside `ShowcaseAssets/`.

| Script | Purpose | Output |
|--------|---------|--------|
| `wand_showcase_generator.py` | 4-wand grids of inventory sprites | `ShowcaseAssets/Wands/wand_showcase_NN.png` |
| `ui_showcase_generator.py`   | Per-panel UI mock from `panel_specs.json` | `ShowcaseAssets/UIPanels/panel_*.png` |

## Quick start

```powershell
# from repo root, with the venv activated
python Scripts\Showcase\wand_showcase_generator.py --dry-run
python Scripts\Showcase\wand_showcase_generator.py             # writes 13 PNGs (49 wands ÷ 4)

python Scripts\Showcase\ui_showcase_generator.py --dry-run
python Scripts\Showcase\ui_showcase_generator.py               # one PNG per panel
python Scripts\Showcase\ui_showcase_generator.py --callouts    # number each button
```

## Why JSON specs for the UI generator?

Headlessly rendering the live tModLoader C# UI requires booting the game.
That's slow and fragile. The script instead consumes a small declarative
spec (`panel_specs.json`) — one entry per panel — that mirrors the panel's
structure 1:1 and references real icon assets from `Assets/Icons/`. When a
panel changes, update the JSON; no game launch needed.

## Adding a new panel

1. Open `panel_specs.json`.
2. Add an entry keyed by short name:
   ```jsonc
   "torches": {
     "title": "Wand of Torches",
     "width": 320,
     "sections": [
       { "header": "Mode", "icons": ["TorchModes/ModePlace", "TorchModes/ModeRemove"] },
       { "header": "Pattern", "label": "Grid / Diamond / Manhattan" }
     ]
   }
   ```
3. Run `python Scripts\Showcase\ui_showcase_generator.py --panel torches`.

`label` rows render as plain text; `icons` rows render as a grid of icon
buttons; `swatches` renders the 31-entry paint palette.
