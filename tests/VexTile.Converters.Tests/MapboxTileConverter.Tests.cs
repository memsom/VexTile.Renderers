using VexTile.Converter.Mapbox;
using VexTile.DataSource.MBTilesSQLite;
using Xunit;

namespace VexTile.Readers.Tests
{
    public class MapboxConverterTests
    {
        readonly string _path = "..\\..\\..\\..\\..\\tiles\\zurich.mbtiles";

        [Fact]
        public async void VectorTileConverterTest()
        {
            var dataSource = new MBTilesSQLiteDataSource(_path, determineZoomLevelsFromTilesTable: true, determineTileRangeFromTilesTable: true);

            Assert.True(dataSource.Version == "3.6.1");

            var tileConverter = new MapboxTileConverter(dataSource);

            var vectorTile = await tileConverter.ConvertToVectorTile(new NetTopologySuite.IO.VectorTiles.Tiles.Tile(8580, 10645, 14));

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
