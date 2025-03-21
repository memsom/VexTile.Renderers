namespace VexTile.Renderer.Mvt.AliFlux;

public interface IVectorCache
{
    string CachePath { get; set; }
    int Count { get; }
    int MaxFiles { get; set; }

    void Refresh();
}