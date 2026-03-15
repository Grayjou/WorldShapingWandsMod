"""
Compiles HJSON localization files to JSON for tModLoader consumption.
Shows errors if the HJSON is malformed or contains unsupported constructs.
"""

import hjson
import json
import re
import sys
from pathlib import Path
import os
DIR_PATH = r"Localization"
CONTEXT_RADIUS = 4


# ── Patterns ──────────────────────────────────────────────────────────────────

# tModLoader format placeholders  {0}  {1}  {2} ...
_RE_FORMAT_ARG   = re.compile(r'\{\d+\}')
# tModLoader item tag              [i:123]  [i:{0}]
_RE_ITEM_TAG     = re.compile(r'$$i:[^$$]+\]')
# tModLoader colour tag            [c/RRGGBB:text]
_RE_COLOUR_TAG   = re.compile(r'$$c/[0-9a-fA-F]{6}:[^$$]*\]')

# Error location patterns
_RE_TMOD         = re.compile(r'[Aa]t line (\d+),\s*column (\d+)')
_RE_PYJSON       = re.compile(r'line (\d+) column (\d+)')


# ── Error helpers ─────────────────────────────────────────────────────────────

def extract_error_location(exc: Exception) -> tuple[int | None, int | None]:
    msg = str(exc)
    for pattern in (_RE_TMOD, _RE_PYJSON):
        m = pattern.search(msg)
        if m:
            return int(m.group(1)), int(m.group(2))
    return None, None


def show_context(lines: list[str], error_line: int, col: int | None,
                 radius: int = CONTEXT_RADIUS) -> None:
    total = len(lines)
    start = max(0, error_line - radius - 1)
    end   = min(total, error_line + radius)

    print("  Source context:")
    for i in range(start, end):
        lineno  = i + 1
        content = lines[i].rstrip('\n')
        marker  = " >>> " if lineno == error_line else "     "
        print(f"  {marker}{lineno:>4}: {content}")

    if col is not None:
        prefix_width = len("       ") + len(f"{error_line:>4}: ")
        print(" " * prefix_width + " " * (col - 1) + "^")


# ── Structural validation ─────────────────────────────────────────────────────

def strip_known_tags(text: str) -> str:
    """
    Remove all constructs that legitimately contain braces so the
    remaining text can be checked for truly bare/broken braces.

    Removed in order:
      1. tModLoader colour tags  [c/RRGGBB:...]
      2. tModLoader item tags    [i:...]
      3. tModLoader format args  {0} {1} {2} ...
    """
    text = _RE_COLOUR_TAG.sub('', text)
    text = _RE_ITEM_TAG.sub('', text)
    text = _RE_FORMAT_ARG.sub('', text)
    return text


def validate_structure(data: dict) -> list[str]:
    """
    Walk the parsed data and flag real tModLoader HJSON problems.
    Returns a list of warning strings (empty list = all clear).
    """
    warnings: list[str] = []

    def _walk(node, path: str = "") -> None:
        if isinstance(node, dict):
            for key, value in node.items():
                _walk(value, f"{path}.{key}" if path else key)

        elif isinstance(node, str):
            # ── Empty tooltip check ───────────────────────────────────────
            # Only warn if the tooltip key suggests it should have real content.
            # Intentionally-blank stubs use ""  and that is fine for tModLoader.
            # We warn only when the value is whitespace-only but NOT the
            # explicit empty-string token (i.e. the author forgot to fill it in
            # rather than deliberately leaving it blank).
            if "Tooltip" in path and node == "" :
                # Suppress — explicit "" is a valid tModLoader stub.
                pass
            elif "Tooltip" in path and node.strip() == "" and node != "":
                warnings.append(
                    f"Tooltip is blank (only whitespace) at '{path}' — "
                    f"use \"\" for an intentional empty tooltip"
                )

            # ── Bare brace check ──────────────────────────────────────────
            # Strip all known legitimate brace-containing constructs first.
            stripped = strip_known_tags(node)
            bad = re.findall(r'[{}]', stripped)
            if bad:
                warnings.append(
                    f"Unrecognised brace(s) {bad} at '{path}'\n"
                    f"    value: {node!r}\n"
                    f"    If this is a format string, use {{0}} {{1}} etc.\n"
                    f"    If it is literal text, the brace may cause a parse error in-game."
                )

    _walk(data)
    return warnings


# ── Compile ───────────────────────────────────────────────────────────────────

def compile_hjson_to_json(hjson_path: str) -> str:
    path = Path(hjson_path)

    if not path.exists():
        raise FileNotFoundError(f"File not found: {hjson_path}")

    raw   = path.read_text(encoding='utf-8')
    lines = raw.splitlines()

    try:
        data = hjson.loads(raw)
    except Exception as exc:
        error_line, error_col = extract_error_location(exc)

        print()
        print("=" * 64)
        print(f"  HJSON PARSE ERROR  —  '{path.name}'")
        print("=" * 64)

        if error_line is not None:
            print(f"  Line {error_line}"
                  + (f", Column {error_col}" if error_col else ""))
            print()
            show_context(lines, error_line, error_col)
        else:
            print(f"  Could not determine exact location.")
            print(f"  Raw error: {exc}")
            print()
            print("  First 10 lines of file:")
            for i, line in enumerate(lines[:10], 1):
                print(f"       {i:>4}: {line}")

        print()
        print("  Common causes:")
        print("  1. Extra or missing closing brace  }")
        print("  2. Multiline tooltip not wrapped in triple-quotes:")
        print("       Tooltip:")
        print("           '''")
        print("           Line one.")
        print("           Line two.")
        print("           '''")
        print("  3. Continuation lines not indented inside a value block.")
        print("=" * 64)
        print()
        sys.exit(1)

    return json.dumps(data, indent=4, ensure_ascii=False)


# ── Entry point ───────────────────────────────────────────────────────────────

if __name__ == "__main__":
    for filename in os.listdir(DIR_PATH):
        if filename.endswith(".hjson"):
            this_path = os.path.join(DIR_PATH, filename)
        else:
            continue
        print(f"Compiling: {this_path}\n")

        json_output = compile_hjson_to_json(this_path)

        data     = json.loads(json_output)
        warnings = validate_structure(data)

        if warnings:
            print(f"  {len(warnings)} warning(s):")
            for w in warnings:
                print(f"    ⚠  {w}")
            print()
        else:
            print("  ✓ No warnings.\n")

        print("✓ Parse successful!\n")
        print("-" * 64)
        print(json_output)