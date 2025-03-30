using BenchmarkDotNet.Attributes;
using SQLite;
using VexTile.Common.Sources;
using VexTile.DataSources.MBTiles;
using VexTile.Readers.Mapbox;

namespace VexTile.VectorTileReaders.Benchmarks
{
    [MemoryDiagnoser]
    public class Benchmarks
    {
        IDataSource _dataSource;
        IVectorTileReader _tileReader;

        [GlobalSetup]
        public void Setup()
        {
            string path = "..\\..\\..\\..\\..\\..\\..\\..\\..\\tiles\\zurich.mbtiles";

            var connection = new SQLiteConnectionString(path, (SQLiteOpenFlags)1, false);

            _dataSource = new MBTilesDataSource(connection, determineZoomLevelsFromTilesTable: true, determineTileRangeFromTilesTable: true);
            _tileReader = new MapboxTileReader(_dataSource);
        }

        [Benchmark]
        public void ReadVectorTile()
        {
            var tile = _tileReader.ReadVectorTile(new NetTopologySuite.IO.VectorTiles.Tiles.Tile(8580, 10645, 14)).ConfigureAwait(false).GetAwaiter().GetResult();
        }
    }
}
