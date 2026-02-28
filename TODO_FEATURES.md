

Preview Mode:
    All Modes:
        These tiles will be destroyed: (*tiles)
        These tiles will be added: (*tiles)
    Normal Mode Exclusive:
        These blocks will be consumed (*blocks)
        These items are required: (*items)
        These blocks won't be destroyed (*indestructible blocks, lack pickaxe power)
    Commit:
        Apply changes and do inventory changes
    Discard:
        Discard selection
    Proper intersection solve:
        Example:
            1. FullCircle at (0,0) with radius 5 (76 block 1)
            2. FullCircle at (1,2) with radius 1 (39 block 2) (36 intersections with block 1)
            Total: 40 block 1, 39 block 2
Regular Mode:
    No warnings, one at a time
    Noise unavailable
    Max Rectangular Area
    Drag and Select points mode
Preview and Regular Mode:
    Undo
    Select Points Mode
Preview Mode:
    Undo past few actions, undo commit restore state before commit
    Move relative (Move the actions and recalculate destroyed and added tiles)
    Max Canvas Area (Proper Memory management)

SimpleWands(Apply Actions):
    -Three variations per wand, right clicking them in the inventory makes them cycle like the shellphone does ✅ IMPLEMENTED
    -Variations are modes: OneClick (click and drag), TwoClick (click start, click end), ThreeClick (click start, click end, click confirm). OneClick applies on release, TwoClick applies on second left click, ThreeClick applies on third left click. ✅ IMPLEMENTED
    -Start on ClickMode persists on hotbar change (Configurable)
    -Right click while selecting cancels action ✅ IMPLEMENTED
    -Right click while not selecting, opens the wand settings UI
    -Show overlays of shapes ✅ IMPLEMENTED
        -Overlays are clipped to screen bounds; tiles offscreen are not drawn (consider drawing indicators at screen edges or full rendering)
    Wands:
        Building: ✅ IMPLEMENTED
            -Applies block type in the selection. Block is the first in the inventory (left to right, up to down) ✅ IMPLEMENTED
            -If block runs out apply behaviour (config: next block, interrupt, cancel action) (uses infinite resource config)
            -IF block replacement is on, replaces any block in the selection with the selected block (Check that it can be destroyed via pickaxe power and breakable tile) (not implemented yet)
            SettingsUI:
                Object (Open UI selector)(block, platform, rope, planter box, rails) (placeholder message)
                Slope (Open UI selector) (Terraria six slopes) (not implemented)
                Shape (Open UI) (Shape Selector, Hollow vs Filled Mode, Outline width controls, etc) (not implemented)
        Destruction: ✅ IMPLEMENTED
            -Removes all block types in the selection (check can destroy tile) ✅ IMPLEMENTED
                SettingsUI:
                    Toggle Tile destruction ✅ IMPLEMENTED
                    Toggle wall destruction ✅ IMPLEMENTED
                    Shape (Open UI) (Shape Selector, Hollow vs Filled Mode, Outline width controls, etc) ✅ IMPLEMENTED
        Replacement (Huge!):
            -In the selection, replaces every instance of the first block in the inventory with the second (Check can destroy tile)
            SettingsUI:
            Shape (Open UI) (Shape Selector, Hollow vs Filled Mode, Outline width controls, etc)
            Object1 (Open UI selector)(block, platform, rope, planter box, rails, seeds, air)
            Object2 (Open UI selector)(block, platform, rope, planter box, rails, seeds, air)
    Wand of Wiring:
        Import from my existing WiringWandMod
    Simple Wand Display:
        Display Current Wand settings (SelectionMode, Block type)

Concerns:
    Undoing:
        Complex logic, for building, have to check if replacements are reversible (complicated with honey blocks, tiles that don't drop themselves always), same thing with destruction and replacement if the tile to replace has this property as well.
    Wall Operations:
        How to implement without saturating the UI (even more :S)
    Doubts:
        Do I need a wand of destruction if wand of replacement has air?


DesignerWands (Selection, Commit, Application):
    Wands with this symbol (?) represent wands which functionality could be merged with other wand prior
    Selection Wand:
        Shape (Open UI selector)(block, platform, rope, planter box, rails)
        Add/Remove/Intersect/XOR
    DrawingWand:
        Draw inside the selection with the first inventory block, doesn't quite place tiles, but indicates a future tile that can be commited
        Draw A shape (Of the suppported ones with selected block) inside the selection
    ReplacementWand (?):
        Same Functions as the drawing wand but replacees the first inventory tile with the second one
    EraserWand (?):
        Same Functions as the DrawingWand, but draws air
    CommitWand(?):
        Commit the changes, takes the tile inventories
    Tracking:
        Tracking necessary resources for committing the changes
    MovingSelection:
        Offsetting selection dynamically, repositioning or moving specific units. Example: build a preview house then move around with the overlay to see where one wants it better
Concerns:
    Same as SimpleWands
    Tile Overlay display (Additionally for modded)
    Wiring visualization :/
    Doubts:
        Do I add filling the entire selection, filling some block with flood fill?
    I must me missing a bunch more options and things

