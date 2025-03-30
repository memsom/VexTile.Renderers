namespace VexTile.Common.Data;

public interface ITile
{
    int X { get; set; }
    int Y { get; set; }
    int Zoom { get; set; }
    byte[]? TileData { get; set; }
}