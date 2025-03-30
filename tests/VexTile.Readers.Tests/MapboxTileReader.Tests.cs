using SQLite;
using VexTile.DataSources.MBTiles;
using VexTile.Readers.Mapbox;
using Xunit;

namespace VexTile.Readers.Tests
{
    public class MapboxReaderTests
    {
        readonly string _path = "..\\..\\..\\..\\..\\tiles\\zurich.mbtiles";

        [Fact]
        public async void CheckVectorTileReaderTest()
        {
            var dataSource = new MBTilesDataSource(_path, determineZoomLevelsFromTilesTable: true, determineTileRangeFromTilesTable: true);

            Assert.True(dataSource.Version == "3.6.1");

            var tileReader = new MapboxTileReader(dataSource);

            var vectorTile = await tileReader.ReadVectorTile(new NetTopologySuite.IO.VectorTiles.Tiles.Tile(8580, 10645, 14));

            Assert.NotNull(vectorTile);
            Assert.True(vectorTile.TileId == 263894745);
            Assert.True(vectorTile.IsEmpty == false);
            Assert.True(vectorTile.Layers.Count == 12);
            Assert.True(vectorTile.Layers[0].Name == "water");
            Assert.True(vectorTile.Layers[0].Features.Count == 9);
            Assert.True(vectorTile.Layers[0].Features[0].Attributes.Count == 1);
            Assert.True(vectorTile.Layers[0].Features[0].Attributes.GetNames()[0] == "class");
            Assert.True(vectorTile.Layers[0].Features[0].Attributes.GetValues()[0].ToString() == "river");
        }
    }
}
