using Avalonia.Controls;
using BruTile.MbTiles;
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
        var source = new MvtVectorTileSource(connectionString, whitelist: ["water"]);
        var tileLayer = new TileLayer(source);
        TheMap.Map.Layers.Add(tileLayer);
    }
}