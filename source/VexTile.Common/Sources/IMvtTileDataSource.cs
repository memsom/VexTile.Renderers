using VexTile.Common.Tables;

namespace VexTile.Common.Sources;

public interface IMvtTileDataSource: IDisposable
{
    IEnumerable<IMetaData> GetMetaData();
    ITile? GetTile(int x, int y, int zoom);
}