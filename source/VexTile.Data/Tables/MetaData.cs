#nullable disable

using SQLite;
using VexTile.Common.Data;

namespace VexTile.Data.Tables;

[Table("metadata")]
internal class MetaData : IMetaData
{
    [Column("name")] public string Name { get; set; } = null!;
    [Column("value")] public string Value { get; set; } = null!;
}