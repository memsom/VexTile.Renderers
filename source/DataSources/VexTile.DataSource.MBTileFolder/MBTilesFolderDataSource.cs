using NetTopologySuite.IO.VectorTiles.Tiles;
using VexTile.Common.Sources;

namespace VexTile.DataSource.MBTilesFolder;

public class MBTilesFolderDataSource : IDataSource
{
    public MBTilesFolderDataSource(string path = ".\\")
    {
        Path = path;
    }

    public string Path { get; }

    public async Task<byte[]?> GetTileAsync(Tile tile)
    {
        string qualifiedPath = Path
                .Replace("{x}", tile.X.ToString())
                .Replace("{y}", tile.Y.ToString())
                .Replace("{z}", tile.Zoom.ToString());

        if (!File.Exists(qualifiedPath))
            return null;

        byte[]? result = null;

        await new Task(() => new Task(() => File.ReadAllBytes(qualifiedPath)).RunSynchronously());

        return result;
    }
}
