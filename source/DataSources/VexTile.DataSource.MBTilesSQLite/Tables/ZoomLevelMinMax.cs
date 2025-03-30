using NetTopologySuite.IO.VectorTiles.Tiles;
using SQLite;

namespace VexTile.DataSource.MBTilesSQLite.Tables;

[Table("tiles")]
public class ZoomLevelMinMax
{
    [Column("tr_min")]
    public int YMin { get; set; }
    [Column("tr_max")]
    public int YMax { get; set; }
    [Column("tc_min")]
    public int XMin { get; set; }
    [Column("tc_max")]
    public int XMax { get; set; }

    public TileRange ToTileRange(int zoomLevel)
    {
        return new TileRange(XMin, YMin, XMax, YMax, zoomLevel);
    }
}
