# Fine Alpine grass meshes

These three meshes are original procedural geometry authored by `FineAlpineGrassSetup` in Topaz. They contain no vendor mesh vertices or textures. The low, upright and shade detail prefabs use these meshes with the existing private PNB foliage materials, so their material textures still require the locally restored Synty packages.

`Topaz/Generation/Configure Luminous Alpine` already invokes `FineAlpineGrassSetup.Apply` after preparing the detail wrappers. Use `Topaz/Generation/Configure Fine Alpine Grass` alone for targeted regeneration of these mesh/material bindings when the wrappers and private textures are present. Low grass uses twelve triangular blades (36 vertices / 12 triangles); upright/shade grass retain curved blades. The three Terrain detail channels and their world placement remain unchanged.
