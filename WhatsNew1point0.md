# You thought I was done? I came back stronger than ever!

## 1.0.0 is a Massive Update!

## Visual Indicators

Every wand action that you can do now has a unique icon that appears above the character. Also, the selection overlay becomes more opaque depending on the remaining steps until the next action. Every wand has a unique color overlay.

## Torches are here!

Two new wand families, Torch Wheel Wand and Wand of Torches: Torch Wheel drives along the ground placing torches with a constant spacing. It follows any irregular terrain, climbs up, down, and around corners. It doesn't destroy pots and herbs! But you can configure it to do so if you want.

Wand of Torches is another on the World Shaping Wands signature selection-based wands. It lets you place torches with efficient tiling patterns (Manhattan/diamond) or just the usual grid. You can replace existing torches, convert biome torches into their current biome variant, or remove torches in bulk. It respects modded biome torches too! 

## Wand of Fluids, you can waterbend now!

Wand of Fluids didn't make it to v0.1.0, I didn't want it to be just a wand that spawned liquid in an area indiscrimenately, so I implemented THREE algorithms for fluid placement!

### Full Liquid

You know how it works, it places liquid in every tile in the shape.

#### Coat in bubble

Do you want to have a floating liquid without building a tile frame, or build floating liquid or any shape you want? This mode coats the outline of your shape with bubbles, stopping it from flowing out

### Rain Fill

This mode places liquid in a way that simulates rain filling up the shape. It starts from the top and fills downwards, respecting gravity and allowing for natural pooling. It's perfect for creating ponds, lakes, or filling in large areas with a more organic liquid distribution.

### Pocket Fill:

This mode identifies enclosed pockets or basins. Every single place where the area could hold water. No spilling, all the filling!

## Wand of Delimitation

Do you want to just work within a specific area without safekeeping the surrounding area? The Wand of Delimitation lets you define a custom-shaped constraint area that all your other wands will respect. It's like a stencil or mask: you paint the area you want to work in, and all your other tools automatically respect that boundary.

## Wand of Molding

Is it annoying to draw your custom shapes, emblems every single time? The Wand of Molding lets you create and reuse custom shapes by "molding" them out of basic shapes. You can add and remove from your mold until it's perfect, then it becomes available as a Custom Shape option in all your other wands!

















