using System;

namespace VexTile.Renderer.Mvt.AliFlux.Sources;

public class TileSourceException : Exception
{
    public TileSourceException(string message) : base(message) { }
}
