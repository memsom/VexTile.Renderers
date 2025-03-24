namespace VexTile.Common.Sources;

public interface IBasicTileSource
{
    Task<byte[]> GetTileAsync(int x, int y, int zoom);
}