# Space Wand Menu System Analysis

## Overview
The Space Wand in ImproveGame features a sophisticated radial menu system with three main modes (Material, Slope, Shape), each containing six sub-options. This menu is implemented through a custom UI framework and provides an intuitive way to configure wand settings.

## Architecture

### Core Components

#### 1. SpaceWandGUI Class
- **Location**: `UI/SpaceWand/SpaceWandGUI.cs`
- **Purpose**: Main UI state managing the entire menu system
- **Key Features**:
  - Manages visibility and animation timers
  - Handles page switching between Material/Slope/Shape modes
  - Contains arrays of RoundButtons for the 6 options per page

#### 2. SelectionButton Class
- **Location**: `UI/SpaceWand/SelectionButton.cs`
- **Purpose**: Circular selector for switching between the three main modes
- **Implementation**:
  - Uses three SelectionPiece objects arranged in a radial pattern
  - Determines hovered piece based on mouse angle from center
  - Clicking different sectors switches the current page

#### 3. RoundButton Class
- **Purpose**: Individual circular buttons for each option
- **Features**: Icon display, selection highlighting, hover tooltips

### Menu Structure

#### Three Main Pages

1. **Material Page** (PlaceType)
   - Options: Platform, Solid, Rope, Rail, GrassSeed, PlantPot
   - Uses item textures from `TextureAssets.Item[]`
   - Controls what type of material to place

2. **Slope Page** (BlockType - Terraria enum)
   - Options: SlopeDownRight, SlopeDownLeft, HalfBlock, SlopeUpLeft, SlopeUpRight, Solid
   - Uses custom textures from `UI/SpaceWand/{blockType}`
   - Controls tile shaping/slope configuration

3. **Shape Page** (ShapeType)
   - Options: Line, Corner, CircleFilled, CircleEmpty, SquareEmpty, SquareFilled
   - Uses custom textures from `UI/SpaceWand/{shapeType}`
   - Controls selection shape for placement area

### Page Management

#### Page Switching Logic
```csharp
public enum PageType : int
{
    Material,
    Slope,
    Shape
}

public static PageType CurrentPage = PageType.Material;
```

#### Setup Methods
Each page has a dedicated setup method:
- `SetupMaterialPage()`: Configures buttons with item icons and PlaceType actions
- `SetupSlopePage()`: Configures buttons with slope icons and BlockType actions  
- `SetupShapePage()`: Configures buttons with shape icons and ShapeType actions

#### Dynamic UI Rebuilding
```csharp
private void SetupPage()
{
    MainPanel.RemoveAllChildren(); // Clear existing buttons
    
    switch (CurrentPage)
    {
        case PageType.Material: SetupMaterialPage(); break;
        case PageType.Slope: SetupSlopePage(); break;
        case PageType.Shape: SetupShapePage(); break;
    }
    
    MainPanel.Append(ModeButton); // Re-add the central selector
}
```

### Interaction Flow

1. **Opening**: Right-click with Space Wand calls `UISystem.Instance.SpaceWandGUI.ProcessRightClick(this)`
2. **Page Selection**: Click central SelectionButton sectors to switch modes
3. **Option Selection**: Click outer RoundButtons to choose specific options
4. **Closing**: Right-click anywhere or switch items to close

### Visual Design

#### Layout
- Central circular SelectionButton (3 sectors for modes)
- 6 RoundButtons arranged around the center
- Semi-transparent background with purple accent colors

#### Assets
- Custom textures in `UI/SpaceWand/` folder
- Item icons for Material page
- Custom icons for Slope and Shape pages

#### Animation
- Smooth open/close animations via `AnimationTimer`
- Hover effects and selection highlighting
- Sound effects for interactions

### Data Binding

#### State Synchronization
- Menu options directly modify `SpaceWand` instance properties:
  - `SpaceWand.PlaceType`
  - `SpaceWand.BlockType` 
  - `SpaceWand.ShapeType`
- Changes persist and affect wand behavior immediately

#### Localization
- Button tooltips use `GetText($"SpaceWandGUI.{optionName}")`
- Supports internationalization

### Integration with Game Systems

#### UI Framework
- Extends `UIState` for integration with Terraria's UI system
- Registered in `UISystem` and added to interface layers
- Positioned relative to mouse cursor

#### Item Integration
- Activated via `IModItem.RightClick()` override
- Checks for valid items and conditions before opening
- Closes automatically when switching away from the wand

This menu system provides a clean, efficient way to configure the complex multi-dimensional settings of the Space Wand, with each dimension (material, shaping, area) accessible through an intuitive radial interface.</content>
<parameter name="filePath">c:\Users\RYZEN 9\Documents\Cloned\SummariesAndAnalysis\Analysis\ImproveGame\SpaceWand_Menu_System.md