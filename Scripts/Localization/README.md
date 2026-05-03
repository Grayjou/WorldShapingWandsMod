# Localization Scripts

WSW localization utilities for HJSON locale maintenance, spreadsheet review workflows, and lint checks.

## New Tooltip Workflow (S6)

## 1) Export tooltip spreadsheet

Exports tooltip rows from a locale file into:
`dev_notes/localization/tooltips_review.csv`

Columns:
- `ItemName`
- `LocalizationKey`
- `TooltipText`
- `Notes`
- `Status`

`Notes` and `Status` are preserved across re-export by `LocalizationKey`.

```powershell
"c:/Users/RYZEN 9/Documents/My Games/Terraria/tModLoader/ModSources/WorldShapingWandsMod/.venv/Scripts/python.exe" Scripts/Localization/tooltip_spreadsheet.py export
```

Optional locale override:

```powershell
"c:/Users/RYZEN 9/Documents/My Games/Terraria/tModLoader/ModSources/WorldShapingWandsMod/.venv/Scripts/python.exe" Scripts/Localization/tooltip_spreadsheet.py export --locale en-US
```

## 2) Import tooltip edits from spreadsheet

Dry-run by default (no writes unless `--apply` is passed).

```powershell
"c:/Users/RYZEN 9/Documents/My Games/Terraria/tModLoader/ModSources/WorldShapingWandsMod/.venv/Scripts/python.exe" Scripts/Localization/tooltip_spreadsheet.py import
"c:/Users/RYZEN 9/Documents/My Games/Terraria/tModLoader/ModSources/WorldShapingWandsMod/.venv/Scripts/python.exe" Scripts/Localization/tooltip_spreadsheet.py import --apply
```

## 3) Shared parser module

`_tooltip_locale_parser.py` provides the narrow line-aware parser used by:
- `tooltip_spreadsheet.py`
- tooltip-focused lint content checks

It supports single-line and triple-quoted tooltip values and in-place updates with minimal disturbance.

## 4) Lint content rules (optional mode)

`lint_localization.py` now supports optional content checks:
- `LOC001`: stray `\n` outside string contexts
- `LOC002`: literal `\n` inside triple-quoted values
- `LOC003`: trailing whitespace in values (warning)
- `LOC004`: dotted keys nested inside section scopes (error smell check)

Run:

```powershell
"c:/Users/RYZEN 9/Documents/My Games/Terraria/tModLoader/ModSources/WorldShapingWandsMod/.venv/Scripts/python.exe" Scripts/Localization/lint_localization.py --check-content-rules
```

Suppress `LOC002` on specific lines with:

```plaintext
# noqa: LOC002
```

(on the relevant triple-value line)
