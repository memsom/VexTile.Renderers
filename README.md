# VexTile.Renderers
VexTile is a MapBox VectorTile rendering engine suite. 

This project aims to take the various renderers that are currently inaccessible to mapping engines like [Mapsui](https://github.com/Mapsui/Mapsui) and provide a drop in tile source for offline (and in the future online) maps tiles in Mapbox Vector Tile format.

The current inplementation is a continuation of the [AliFlux/VectorTileRenderer](https://github.com/AliFlux/VectorTileRenderer) written by Ali Ashraf. This engine is capable of rendering tiles, but currently needs work to improve the rendering quality.

In the future we hope to look at getting other engines integrated. This is an ongoing efort and we are very hopeful that others will contribute.

Build status: [![Build status](https://ci.appveyor.com/api/projects/status/pc9smglg8hiejk7t/branch/main?svg=true)](https://ci.appveyor.com/project/memsom/vextile-renderers/branch/main)

A basic example of rendering a tile:

```csharp
// create a canvas. SkiaSharp is currently the only renderer supported
var canvas = new SkiaCanvas();

// locate a file - this file is located in the tiles directory
string path = "zurich.mbtiles";

// create an instance of the renderer.
// it is possible to specify an existing SQLiteConnection, but for this example we will use the path
var renderer = new TileRenderer(path, VectorStyleKind.Basic);

// render a tile. {0,0} at zoom level 0 is the entire globe in a single tile, so it is a good
// first tile to use in any aituation where you want to test a renderer.
var tile = await renderer.RenderTileAsync(canvas, 0,0,0);
```

Tiles are generated in PNG format. It would also be trivial to provide an alternative implementaion in other formats by augmenting the class that implements ICanvas.



