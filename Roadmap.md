# WorldShapingWandsMod Roadmap

## Overview
This roadmap outlines the development plan for the WorldShapingWandsMod, a comprehensive Terraria mod that provides advanced world-shaping tools with professional-quality shape generation, outline systems, and user-friendly controls. The roadmap is structured around the features listed in `TODO_FEATURES.md`, organized into phases with priorities, and incorporates detailed analysis from ImproveGame's SpaceWand and WandOfConstruction systems.

## Current Status
- ✅ **Core Architecture**: Unified outline system via `OutlineHelper.cs`, per-player settings management, context-aware preview visibility (Default/Forced modes)
- ✅ **Shape System**: All basic shapes implemented (Rectangle, Ellipse, Diamond, Triangle, Line) with configurable parameters
- ✅ **UI/Controls**: Selection overlay, thickness controls, shape test commands
- ✅ **Preview System**: Intelligent preview visibility that adapts to player context (holding wand items)
- 🔄 **Build Status**: Compiles successfully, ready for testing and feature expansion

## ImproveGame Analysis Integration

### Key Insights from SpaceWand System
- **Advanced Item Recognition**: Sophisticated `PlaceType` system (Platform, Solid, Rope, Rail, GrassSeed, PlantPot) with intelligent inventory scanning
- **Shape-Based Selection**: Line, Corner, Square (Empty/Filled), Circle (Empty/Filled) with real-time tile calculation
- **Safety Systems**: Pick power validation, indestructible tile detection, graceful failure handling
- **Radial UI**: Three-page menu system (Material/Slope/Shape) with intuitive circular interface
- **Performance**: Screen culling, squared distance calculations, memory-efficient algorithms
- **Multiplayer**: Client-authority with server validation and packet-based synchronization

### Key Insights from WandOfConstruction System
- **Structure Management**: File-based saving/loading with TagCompound serialization
- **Preview Rendering**: RenderTarget2D-based overlays with individual tile highlighting
- **Placement Logic**: Coroutine-based generation with separate handling for single/multi-tile objects
- **Material Consumption**: Intelligent inventory management with item validation
- **Bug Awareness**: Known issues with multi-tile object rendering in previews

### Key Insights from Overlay Systems
- **Dual Approaches**: Individual tile highlighting (MarqueeSystem) vs rectangular overlays (GameRectangle)
- **IMarqueeItem Interface**: Standardized overlay behavior for different wand types
- **Performance Optimization**: Screen bounds checking and efficient rendering

## Feature Roadmap

### Phase 1: Core Enhancement (High Priority)
**Focus**: Stabilize and enhance existing systems with noise and preview improvements, incorporating ImproveGame's safety and performance patterns.

#### Boundary Noise System
- **Add Noise**: Implement procedural noise generation for shape boundaries
- **Regenerate Noise**: Allow dynamic noise regeneration for variety
- **Support Noise Bias**: Add bias types (Positive, Negative, Positive and Negative)
- **Bias Amount**: Configurable amplitude control (MaxAmplitude)
- **ImproveGame Inspiration**: Efficient algorithms with squared distance calculations

#### Preview Mode Enhancements
- **All Modes Features**:
  - Display tiles to be destroyed and added in real-time
  - Show resource consumption warnings
- **Normal Mode Exclusive**:
  - Block consumption tracking
  - Required items display
  - Indestructible block warnings
- **Commit/Discard Actions**:
  - Apply changes with inventory updates
  - Proper intersection resolution for overlapping shapes
- **ImproveGame Inspiration**: Real-time overlay updates with color-coded feedback (purple for valid, red for invalid)

#### Regular Mode Improvements
- Remove warnings for immediate actions
- Disable noise in regular mode
- Implement max rectangular area limits
- Add drag-and-select points mode
- **ImproveGame Inspiration**: Shape algorithms with performance optimizations

### Phase 2: Advanced Wand System (High Priority)
**Focus**: Implement sophisticated wand functionality with ImproveGame-inspired UI and item recognition.

#### SimpleWands (Apply Actions) - Enhanced with SpaceWand Analysis
- **Two Variations per Wand**: Cycle modes via right-click (Click-and-drag vs Click1-Click2)
- **Persistence**: Configurable start mode that persists on hotbar changes
- **Controls**: Right-click to cancel selection or open settings UI
- **Overlay Display**: Show shape overlays during selection
- **Wand Types**:
  - **Building Wand**: Apply block types from inventory with `PlaceType` recognition (Platform, Solid, Rope, Rail, GrassSeed, PlantPot)
  - **Destruction Wand**: Remove blocks/walls with toggle controls and pick power validation
  - **Replacement Wand**: Replace blocks with configurable source/target and safe replacement logic
- **Settings UI**: Radial menu system with three pages (Material/Slope/Shape) inspired by SpaceWand
- **ImproveGame Inspiration**: Advanced item matching conditions, tile wand support, slope application logic

#### Wand of Wiring
- Import functionality from existing WiringWandMod
- Integrate with shape selection system

#### Simple Wand Display
- Display current wand settings (selection mode, block type)
- **ImproveGame Inspiration**: Real-time UI feedback and tooltip system

### Phase 3: Designer Wands with Structure Management (Medium Priority)
**Focus**: Implement sophisticated selection and structure tools, incorporating WandOfConstruction patterns.

#### Selection Wand
- Shape selection UI integration
- Boolean operations: Add/Remove/Intersect/XOR
- **ImproveGame Inspiration**: Rectangular selection with `SelectorItem` inheritance

#### DrawingWand
- Draw within selections without placing tiles
- Preview future tile placements
- **ImproveGame Inspiration**: PreviewRenderer with RenderTarget2D for complex overlays

#### ReplacementWand
- Enhanced replacement with preview
- **ImproveGame Inspiration**: Safe tile replacement with pick power validation

#### EraserWand
- Air placement for clearing areas
- **ImproveGame Inspiration**: Destruction logic with indestructible tile handling

#### CommitWand
- Apply tracked changes with inventory management
- Resource tracking for commits
- **ImproveGame Inspiration**: Coroutine-based placement with material consumption

#### MovingSelection
- Dynamic selection offsetting
- Repositioning overlays
- **ImproveGame Inspiration**: Origin offset handling for precise positioning

### Phase 4: Quality of Life & Advanced Features (Medium Priority)
**Focus**: Add convenience features and resolve concerns, with ImproveGame's performance and safety focus.

#### Undo System
- Undo for regular mode actions
- Multi-level undo for preview mode commits
- Complex logic handling for reversible operations
- **ImproveGame Inspiration**: Graceful failure handling and validation systems

#### Wall Operations
- Toggle wall destruction/placement
- UI integration without clutter
- **ImproveGame Inspiration**: Separate wall generation in placement coroutines

#### HollowShape Enhancements
- Line thickness controls (+/- adjustments)
- Inwards hollowing (industry standard)
- **ImproveGame Inspiration**: Precise shape controls with performance optimizations

#### Dimension Display
- Show WxH dimensions in overlays
- **ImproveGame Inspiration**: TextDisplayType.All for comprehensive information

#### Memory Management
- Max canvas area limits
- Proper memory handling for large selections
- **ImproveGame Inspiration**: Screen culling and bounds checking

### Phase 5: Expert Features & Polish (Low Priority)
**Focus**: Future enhancements with full ImproveGame integration.

#### Additional Shape Modes
- Flood fill operations
- Advanced boolean operations
- **ImproveGame Inspiration**: Extensible shape system with new `ShapeType` additions

#### Wiring Visualization
- Visual overlays for wiring
- **ImproveGame Inspiration**: Enhanced visualization tools

#### Mod Integration
- Support for modded tiles/blocks
- Compatibility with other building mods
- **ImproveGame Inspiration**: TileLoader.CanPlace() validation and ModSystems integration

#### Structure Saving/Loading
- Save and load complex tile arrangements
- File-based structure management
- **ImproveGame Inspiration**: QoLStructure class with TagCompound serialization

#### Multiplayer Support
- Client-authority placement with server validation
- Packet-based synchronization
- **ImproveGame Inspiration**: SpaceWandOperation packets and coroutine sync

## Implementation Plan

### Development Guidelines
1. **Modular Architecture**: Continue using centralized helpers (OutlineHelper, GeometryHelper)
2. **Per-Player State**: Maintain WandPlayer for individual settings
3. **UI Consistency**: Implement radial menu system inspired by SpaceWand's three-page design
4. **Performance**: Adopt ImproveGame's optimizations (screen culling, squared distance, memory management)
5. **Safety**: Implement pick power validation and indestructible tile handling
6. **Item Recognition**: Use PlaceType system for intelligent inventory scanning
7. **Testing**: Comprehensive testing for each feature phase with multiplayer validation

### Technical Considerations
- **Intersection Resolution**: Implement proper tile conflict resolution with safe replacement logic
- **Inventory Management**: Handle complex consumption/replacement with tile wand support
- **Preview Performance**: Optimize overlay rendering using MarqueeSystem and GameRectangle approaches
- **Save/Load**: Implement TagCompound-based persistence for wand settings and structures
- **Multiplayer**: Add packet-based synchronization for placement operations
- **ImproveGame Compatibility**: Ensure features complement rather than conflict with existing mods

### Risk Mitigation
- **Complex Logic**: Start with simple implementations, add complexity iteratively
- **UI Saturation**: Radial menu design prevents clutter while providing comprehensive options
- **Performance Issues**: Memory limits, background processing for large operations, screen culling
- **Balance Concerns**: Configurable limits to prevent exploitation, pick power validation
- **Multi-tile Bugs**: Careful implementation of preview rendering to avoid WandOfConstruction issues

### Architecture Improvements
- **IMarqueeItem Interface**: Standardize overlay behavior across wand types
- **SelectorItem Base Class**: Use for rectangular selection operations
- **Coroutine System**: Implement for large placement operations
- **FileOperator Class**: Add for structure saving/loading functionality

## Timeline & Milestones

### Milestone 1: Phase 1 Completion (2-3 weeks)
- Noise system implementation with performance optimizations
- Enhanced preview modes with color-coded feedback
- Regular mode improvements with shape algorithm optimizations

### Milestone 2: Phase 2 Completion (4-6 weeks)
- Advanced wand system with radial UI and PlaceType recognition
- Settings UI with three-page menu system
- Simple wand display with real-time feedback

### Milestone 3: Phase 3 Completion (6-8 weeks)
- Designer wands with structure management
- Advanced selection tools with boolean operations
- Commit system with coroutine-based placement

### Milestone 4: Phase 4-5 Completion (8-12 weeks)
- QoL features with safety systems
- Advanced features with multiplayer support
- Polish and optimization with full ImproveGame integration

## Dependencies & Resources
- **Terraria ModLoader**: Latest stable version
- **ImproveGame Analysis**: Comprehensive understanding of SpaceWand, WandOfConstruction, and overlay systems
- **Testing Environment**: Dedicated test world for feature validation and multiplayer testing
- **Community Feedback**: Beta testing and user input for UI/UX refinement
- **Performance Tools**: Profiling tools to match ImproveGame's optimization standards

## Success Metrics
- **Functionality**: All planned features implemented with ImproveGame-level sophistication
- **Performance**: Match or exceed ImproveGame's efficiency (screen culling, memory management)
- **Usability**: Intuitive radial UI with comprehensive options and clear feedback
- **Safety**: Robust indestructible tile handling and pick power validation
- **Compatibility**: Seamless integration with ImproveGame and other building mods
- **Multiplayer**: Reliable synchronization for all placement operations
- **Community**: Positive feedback and adoption with professional-quality features

---

*This roadmap has been updated to incorporate detailed analysis from ImproveGame's SpaceWand and WandOfConstruction systems, ensuring the WorldShapingWandsMod achieves similar levels of sophistication, performance, and user experience. Regular updates will be made to reflect current status and priorities.*