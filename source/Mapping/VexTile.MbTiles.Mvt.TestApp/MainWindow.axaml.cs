using Avalonia.Controls;
using Mapsui.Tiling.Layers;
using SQLite;
using VexTile.TileSource.Mvt;

namespace VexTile.MbTiles.Mvt.TestApp;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        var connectionString = new SQLiteConnectionString("zurich.mbtiles", SQLiteOpenFlags.ReadOnly, false);
        var source = new MvtVectorTileSource(connectionString);
        var tileLayer = new TileLayer(source);
        TheMap.Map.Layers.Add(tileLayer);
    }
}