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
        readonly string _path = "..\\..\\..\\..\\..\\tiles\\zurich.mbtiles";

        IVectorTileReader? _tileReader;
        Tile _tile = new Tile(8580, 10645, 14);
        byte[]? _data;

        [GlobalSetup]
        public void Setup()
        {
            var dataSource = new MBTilesDataSource(_path);
            
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
