using System.Collections.Generic;

namespace VexTile.Renderer.Mvt.AliFlux;

public class TileInfo(
    int x,
    int y,
    double zoom,
    double sizeX = 512,
    double sizeY = 512,
    double scale = 1,
    List<string> layerWhiteList = null)
{

    // this is used for a Pbf at the moment, as we don't need the x/y for that
    public TileInfo(
        double zoom,
        double sizeX = 512, double sizeY = 512,
        double scale = 1,
        List<string> layerWhiteList = null)
        : this(0, 0, zoom, sizeX, sizeY, scale, layerWhiteList)
    {
    }

    public int X { get; } = x;
    public int Y { get; } = y;
    public double Zoom { get; } = zoom;
    public double SizeX { get; } = sizeX;
    public double SizeY { get; } = sizeY;
    public double Scale { get; } = scale;
    public List<string> LayerWhiteList { get; } = layerWhiteList;

    public double ScaledSizeX => SizeX * Scale;
    public double ScaledSizeY => SizeY * Scale;
}