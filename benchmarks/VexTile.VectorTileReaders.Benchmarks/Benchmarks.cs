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
        readonly string _path = "..\\..\\..\\..\\..\\..\\..\\..\\..\\tiles\\zurich.mbtiles";

        IVectorTileReader? _tileReader;
        List<Tile> _tiles = new List<Tile> { new Tile(134, 166, 8), new Tile(8580, 10645, 14), new Tile(8581, 10645, 14), new Tile(8580, 10644, 14) };
        List<byte[]?> _data = new List<byte[]?>();

        [GlobalSetup]
        public void Setup()
        {
            var dataSource = new MBTilesDataSource(_path);

            _tileReader = new MapboxTileReader(dataSource);

            foreach (var tile in _tiles)
                _data.Add(dataSource.GetTileAsync(tile).ConfigureAwait(false).GetAwaiter().GetResult());
        }

        [Benchmark]
        [Arguments(0)]
        [Arguments(1)]
        [Arguments(2)]
        [Arguments(3)]
        public void ReadVectorTile(int i)
        {
            _tileReader?.ReadVectorTile(_tiles[i], _data[i])
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();
        }

        [Benchmark]
        public void ReadVectorTiles()
        {
            for (var i = 0; i < _tiles.Count(); i++)
                _tileReader?.ReadVectorTile(_tiles[i], _data[i])
                    .ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult();
        }
    }
}
