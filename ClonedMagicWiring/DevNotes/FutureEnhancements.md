# Future Enhancements & Implementation Ideas

## High Priority

### Performance Optimizations
- Profile tile iteration for large shapes (>1000 tiles)
- **Cache shape calculations** - Store previous start/end points and only recalculate when they change
- **Incremental shape updates** - For rectangles, calculate delta between old and new dimensions rather than full recalculation
- **Optimize ShapeHelper** - Make it independent with built-in caching:
  - Store last calculated bounding box dimensions
  - Only recalculate when start or end point changes
  - For rectangles, use optimized algorithm to get new area from old dimensions
- Investigate draw call batching for overlay rendering

### UI/UX Improvements
- **Show selection dimensions** - Display width×height (e.g., "15×12 tiles") in overlay or UI panel
- **Display tile count** - Show total number of tiles that will be affected
- **Cancellation feedback** - Show "Cancelled" text that fades away after 700ms when operation is cancelled
- **Blue overlay for remove mode** - Use blue instead of red for better visual distinction (DONE)
- Add keybinds for quick mode/shape switching
- Implement undo/redo system for wiring operations
- Add sound effects for successful operations
- Consider adding haptic feedback indicators

### Shape Additions
- Ellipse/Circle shapes (filled & hollow)
- Line tool (straight line between two points)
- Polygon tool (click multiple points)
- Flood fill for wiring (like paint bucket)
- Noise/randomization parameter for existing shapes

## Important: Wiring vs Artistic Shapes

**Note for ShapeHelper**: The current shapes are designed for artistic/geometric placement. However, wiring operations require **shortest and organized connections** rather than pure geometric shapes. Future enhancements should consider:

- **Pathfinding-based wiring** - Use A* or similar to find optimal wire paths
- **Connection ordering** - Ensure wires connect in logical sequence for circuit functionality
- **Avoid redundant paths** - Minimize overlapping or unnecessary wire segments
- **Smart routing** - Route around obstacles or existing structures
- **Wire optimization** - Post-process shapes to remove unnecessary tiles for functional wiring

This distinction should be clearly documented in ShapeHelper's purpose and future implementations should offer both "geometric" and "optimized wiring" modes.

## Medium Priority

### Quality of Life
- Show tile count in preview overlay
- Add copy/paste functionality for wire patterns
- Implement wire pattern templates/presets
- Add measurement/ruler tool
- Cost calculator (materials needed preview)

### Advanced Features
- Pattern library system (save/load wire patterns)
- Multi-layer operations (place multiple wire colors at once)
- Smart wiring suggestions based on contraption type
- Integration with Journey Mode research

### Accessibility
- Colorblind mode (alternative colors/patterns for overlay)
- Alternative control schemes
- Configurable overlay opacity per-shape
- High contrast mode option

## Low Priority / Future Considerations

### Integration Features
- API for other mods to trigger wiring operations
- Export/import wire patterns as files
- Statistics tracking (total wires placed, favorite shapes, etc.)
- Achievement system for complex contraptions

### Creative Tools
- Wire art mode (decorative non-functional wiring)
- Animation preview for mechanical setups
- Circuit simulator/tester
- Schematic/blueprint system

## Technical Debt
- Consider refactoring ShapeHelper into separate strategy classes
- Evaluate whether WiringSettings should be instance-based vs static
- Review networking packet structure for future extensibility
- Add comprehensive unit tests for shape generation algorithms

## Community Requests
- (Track user suggestions here)
- Add link to issue tracker: https://github.com/Grayjou/MagicWiring/issues
