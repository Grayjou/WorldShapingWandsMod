This folder is called SpriteWrapper. It's a little bit of a generic name, but it literally wraps sprites, like one on top of each other and then exports.
So it literally wraps sprites lol.

Aseprite's workflow is very annoying on repetitive actions, so I'm using a mixed workflow:
I export a base sprite from Aseprite, then I wrap them in a python script.

WandAction projectiles usually have three layers (two + transform):
- 0. Base Color Layer (Monochrome design)
- 2. Outline Layer (Pretty much white or close to white)
- 1. Convolution blur layer from outline → creates a glow effect shade. (Sometimes it's 3x3, 3x3 sharp or 5x5 blur, depending on the sprite)
And yes, you guessed it, It's very annoying to do that for 40+ spritesheets

```cpp
blur-5x5 5 5 2 2
  { 1 2 3 2 1
    2 3 4 3 2
    3 4 5 4 3
    2 3 4 3 2
    1 2 3 2 1 } auto auto rgba

blur-3x3 3 3 1 1
  { 1 2 1
    2 4 2
    1 2 1 } auto auto rgba

blur-3x3-hard 3 3 1 1
  { 0 1 0
    1 8 1
    0 1 0 } auto auto rgba

```
So I gotta do it in a script that reads the base and outline, sandwiches the blur in between, and then exports the final sprite sheet.
Tho tbh, I have no idea how that works

Scripts\SpriteWrapper\Projectiles\WandActions\Building\GrassSeeds\Base.png
Scripts\SpriteWrapper\Projectiles\WandActions\Building\GrassSeeds\config.json
```json
    {
        "convolution_matrix": {
            "type": "5x5",
            "layer": 1,
            "from_layer": 2
        },
        "layers": [
            {
                "name":"Base",
                "index": 0
            },
            {
                "name":"Outline",
                "index": 2
            }
        ],
        "export_to": "Content_Source\\Projectiles\\WandActions\\Building\\WandAction_BuildingGrassSeeds.png"
    }
```
\SpriteWrapper\Projectiles\WandActions\Building\GrassSeeds\Outline.png
Actually making all these dirs is a lot of work, so I gotta automate that too and generate a default config.json for each sprite sheet, which I can then customize if needed. Fluid for example, has more than two layers, because it has a separate layer for the liquid color.