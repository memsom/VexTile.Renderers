using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using VexTile.Common.Sources;
using VexTile.Mapbox.VectorTile.Geometry;
using VexTile.Renderer.Mvt.AliFlux.Drawing;
using MbVectorTileFeature = VexTile.Mapbox.VectorTile.VectorTileFeature;
using MbVectorTile = VexTile.Mapbox.VectorTile.VectorTileData;
using MbVectorTileLayer = VexTile.Mapbox.VectorTile.VectorTileLayer;


namespace VexTile.Renderer.Mvt.AliFlux.Sources;

public class PbfTileSource : IVectorTileSource
{
    public string Path { get; set; } = "";
    public byte[] Bytes { get; set; } = null;

    public PbfTileSource(string path)
    {
        this.Path = path;
    }

    public PbfTileSource(byte[] data)
    {
        this.Bytes = data;
    }

    public async Task<byte[]> GetTileAsync(int x, int y, int zoom)
    {
        return await Task.Run(() =>
        {
            string qualifiedPath = Path
                .Replace("{x}", x.ToString())
                .Replace("{y}", y.ToString())
                .Replace("{z}", zoom.ToString());
            return File.ReadAllBytes(qualifiedPath);
        });
    }

    public async Task<VectorTile> GetVectorTileAsync(int x, int y, int zoom)
    {
        if (Path != "")
        {
            byte[] stream = await GetTileAsync(x, y, zoom);

            return await UnzipStreamAsync(stream);
        }
        else if (Bytes != null)
        {
            return await UnzipStreamAsync(Bytes);
        }

        return null;
    }

    private async Task<VectorTile> UnzipStreamAsync(byte[] data)
    {
        if (IsGZipped(data))
        {
            using (var stream = new MemoryStream(data))
            using (var zipStream = new GZipStream(stream, CompressionMode.Decompress))
            using (var resultStream = new MemoryStream())
            {
                await zipStream.CopyToAsync(resultStream);
                resultStream.Seek(0, SeekOrigin.Begin);
                return await LoadDataAsync(ReadTillEnd(resultStream));
            }
        }
        else
        {
            return await LoadDataAsync(data);
        }
    }

    private async Task<VectorTile> LoadDataAsync(byte[] stream)
    {
        var mbLayers = new MbVectorTile(stream);

        return await BaseTileToVectorAsync(mbLayers);
    }

    private static string ConvertGeometryType(GeometryType type)
    {
        if (type == GeometryType.Linestring)
        {
            return "LineString";
        }
        else if (type == GeometryType.Point)
        {
            return "Point";
        }
        else if (type == GeometryType.Polygon)
        {
            return "Polygon";
        }
        else
        {
            return "Unknown";
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S125:Sections of code should not be commented out", Justification = "<Pending>")]
    private static async Task<VectorTile> BaseTileToVectorAsync(object baseTile)
    {
        return await Task.Run(() =>
        {
            var tile = baseTile as MbVectorTile;
            var result = new VectorTile();

            foreach (string lyrName in tile.LayerNames())
            {
                MbVectorTileLayer lyr = tile.GetLayer(lyrName);

                var vectorLayer = new VectorTileLayer
                {
                    Name = lyrName
                };

                for (int i = 0; i < lyr.FeatureCount(); i++)
                {
                    MbVectorTileFeature feat = lyr.GetFeature(i);

                    var vectorFeature = new VectorTileFeature
                    {
                        Extent = 1,
                        GeometryType = ConvertGeometryType(feat.GeometryType),
                        Attributes = feat.GetProperties()
                    };

                    var vectorGeometry = new List<List<Point>>();

                    foreach (var points in feat.Geometry<int>())
                    {
                        var vectorPoints = new List<Point>();

                        foreach (var coordinate in points)
                        {
                            double dX = coordinate.X / (double)lyr.Extent;
                            double dY = coordinate.Y / (double)lyr.Extent;

                            vectorPoints.Add(new Point(dX, dY));

                            //var newX = Utils.ConvertRange(dX, extent.Left, extent.Right, 0, vectorFeature.Extent);
                            //var newY = Utils.ConvertRange(dY, extent.Top, extent.Bottom, 0, vectorFeature.Extent);

                            //vectorPoints.Add(new Point(newX, newY));
                        }

                        vectorGeometry.Add(vectorPoints);
                    }

                    vectorFeature.Geometry = vectorGeometry;
                    vectorLayer.Features.Add(vectorFeature);
                }

                result.Layers.Add(vectorLayer);
            }

            return result;
        });
    }

    private byte[] ReadTillEnd(Stream input)
    {
        byte[] buffer = new byte[16 * 1024];
        using MemoryStream ms = new();

        int read;
        while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
        {
            ms.Write(buffer, 0, read);
        }
        return ms.ToArray();
    }

    private bool IsGZipped(byte[] data)
    {
        return IsZipped(data, 3, "1F-8B-08");
    }

    private bool IsZipped(byte[] data, int signatureSize = 4, string expectedSignature = "50-4B-03-04")
    {
        if (data.Length < signatureSize)
            return false;
        byte[] signature = new byte[signatureSize];
        Buffer.BlockCopy(data, 0, signature, 0, signatureSize);
        string actualSignature = BitConverter.ToString(signature);
        return (actualSignature == expectedSignature);
    }

    Task<byte[]> IBasicTileSource.GetTileAsync(int x, int y, int zoom)
    {
        throw new TileSourceException("Not implemented yet");
    }
}
