# Draggable UIs in ImproveGame

## Overview
ImproveGame implements draggable user interfaces through custom panel classes that handle mouse input and position updates. This allows players to reposition UI windows according to their preferences, enhancing usability and customization.

## Core Components

### 1. SUIPanel Class
**Location**: `UIFramework/SUIElements/SUIPanel.cs`  
**Purpose**: The primary draggable panel used throughout the mod's UIs

#### Key Properties
```csharp
internal bool Draggable;  // Enables/disables dragging
internal bool Dragging;   // Current drag state
internal Vector2 Offset;  // Mouse offset from panel position
internal bool Resizeable; // Optional resizing capability
```

#### Constructor
```csharp
public SUIPanel(Color borderColor, Color backgroundColor, 
    float rounded = 12, float border = 2, bool draggable = false)
```

#### Drag Implementation
- **Mouse Down**: Captures offset between mouse position and panel top-left
- **Mouse Up**: Resets drag state
- **Draw Method**: Updates panel position based on mouse movement during drag

```csharp
if (Dragging)
{
    var left = Main.mouseX - Offset.X;
    var top = Main.mouseY - Offset.Y;
    // Optional snapping to grid
    if (DragIncrement.X > 0) left -= left % DragIncrement.X;
    if (DragIncrement.Y > 0) top -= top % DragIncrement.Y;
    SetPosPixels(left, top).Recalculate();
}
```

### 2. ModUIPanel Class
**Location**: `UIFramework/UIElements/ModUIPanel.cs`  
**Purpose**: Alternative draggable panel with similar functionality

#### Differences from SUIPanel
- Uses `Update()` method instead of `Draw()` for position updates
- Supports both dragging and resizing
- Has minimum size constraints for resizing

### 3. View.DragIgnore Property
**Location**: `UIFramework/BaseViews/View.cs`  
**Purpose**: Controls whether child elements should prevent parent dragging

```csharp
/// <summary>
/// 拖动忽略，默认为 <see langword="false"/> 不会影响长辈中可拖动元素拖动
/// </summary>
public bool DragIgnore;
```

## How to Make a UI Draggable

### Basic Setup
1. Create an `SUIPanel` instance
2. Set `Draggable = true` in the object initializer
3. Add it to your UI hierarchy

```csharp
MainPanel = new SUIPanel(UIStyle.PanelBorder, UIStyle.PanelBg)
{
    Shaded = true,
    Draggable = true,
    FinallyDrawBorder = true
};
```

### Advanced Configuration
```csharp
MainPanel = new SUIPanel(UIStyle.PanelBorder, UIStyle.PanelBg)
{
    Draggable = true,
    DragIncrement = new Vector2(8, 8), // Snap to 8-pixel grid
    Resizeable = true,                 // Allow resizing
    MinResizeWidth = 300,
    MinResizeHeight = 200
};
```

### Child Element Considerations
- Set `DragIgnore = true` on child elements that shouldn't interfere with dragging
- The drag logic checks if the clicked element is the panel itself or has `DragIgnore = true`

## Examples in ImproveGame

### Big Bag UI
```csharp
MainPanel = new SUIPanel(UIStyle.PanelBorder, UIStyle.PanelBg)
{
    Shaded = true,
    Draggable = true,
    FinallyDrawBorder = true
};
```

### Other Draggable UIs
- Extreme Storage interfaces
- Recipe search windows
- Configuration panels
- Most major UI windows in the mod

## Technical Details

### Mouse Interaction Handling
1. **LeftMouseDown**: Determines if click is for dragging or resizing based on position
2. **LeftMouseUp**: Clears drag/resize states
3. **Draw/Update**: Applies position changes during drag operations

### Position Management
- Uses pixel-based positioning (`SetPosPixels`)
- Supports both absolute and relative positioning
- Automatically recalculates layout after position changes

### Performance Considerations
- Only updates position when actively dragging
- Uses `Main.mouseX/Y` for real-time positioning
- Minimal overhead when not dragging

### Integration with Terraria UI System
- Inherits from `View` → `UIElement` for compatibility
- Integrates with Terraria's mouse interface system
- Respects UI layering and focus management

## Benefits
- **User Customization**: Players can position UIs to avoid screen clutter
- **Accessibility**: Better accommodation for different screen sizes/resolutions
- **Consistency**: Uniform drag behavior across all mod UIs
- **Intuitive**: Standard drag-and-drop interaction familiar to users

This drag system provides a polished, user-friendly experience that enhances the overall usability of ImproveGame's extensive UI suite.</content>
<parameter name="filePath">c:\Users\RYZEN 9\Documents\Cloned\SummariesAndAnalysis\Analysis\ImproveGame\Draggable_UIs.md