using SQLite;
using VexTile.Common.Tables;

namespace VexTile.Data.Tables;

[Table("tiles")]
internal class Tile : ITile
{
    [Column("tile_column")]
    public int X { get; set; }
    [Column("tile_row")]
    public int Y { get; set; }
    [Column("zoom_level")]
    public int Zoom { get; set; }
    [Column("tile_data")]
    public byte[]? TileData { get; set; }
}