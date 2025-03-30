using BenchmarkDotNet.Attributes;
using SQLite;
using VexTile.Common.Sources;
using VexTile.DataSources.MBTiles;
using VexTile.Readers.Mapbox;
using NetTopologySuite.IO.VectorTiles.Tiles;

namespace VexTile.VectorTileReaders.Benchmarks
{
    [MemoryDiagnoser]
    public class Benchmarks
    {
        IVectorTileReader? _tileReader;
        Tile _tile = new Tile(8580, 10645, 14);
        byte[]? _data;

        [GlobalSetup]
        public void Setup()
        {
            string path = "..\\..\\..\\..\\..\\..\\..\\..\\..\\tiles\\zurich.mbtiles";

            var connection = new SQLiteConnectionString(path, (SQLiteOpenFlags)1, false);
            var dataSource = new MBTilesDataSource(connection);
            
            _tileReader = new MapboxTileReader(dataSource);
            _data = dataSource.GetTileAsync(_tile).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        [Benchmark]
        public void ReadVectorTile()
        {
            var tile = _tileReader?.ReadVectorTile(_tile, _data).ConfigureAwait(false).GetAwaiter().GetResult();
        }
    }
}
