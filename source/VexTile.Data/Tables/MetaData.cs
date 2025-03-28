#nullable disable

using SQLite;
using VexTile.Common;
using VexTile.Common.Tables;

namespace VexTile.Data.Tables;

[Table("metadata")]
internal class MetaData : IMetaData
{
    [Column("name")] public string Name { get; set; } = null!;
    [Column("value")] public string Value { get; set; } = null!;
}