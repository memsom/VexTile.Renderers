using System;
using System.Collections.Generic;
using VexTile.Common.Data;

namespace VexTile.Common.Sources;

public interface ITileDataSource: IDisposable
{
    IEnumerable<IMetaData> GetMetaData();
    ITile? GetTile(int x, int y, int zoom);
}