using SQLite;

namespace VexTile.DataSource.MBTiles.Tables;

[Table("tiles")]
public class ZoomLevel // I would rather just user 'int' instead of this class in Query, but can't get it to work
{
    [Column("level")]
    public int Level { get; set; }
}
