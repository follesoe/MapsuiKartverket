using System;
using System.IO;
using System.Net.Http;
using BruTile;
using BruTile.Web;
using BruTile.Cache;
using Mapsui.Projection;
using Mapsui.Fetcher;
using static System.Environment;

namespace MapsuiTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Mapsui.Logging.Logger.LogDelegate += (level, message, ex) =>
            {
                Console.WriteLine($"[{level}]: {message}");
            };

            using var httpClient = new HttpClient();

            var fileCache = new FileCache(Path.Combine(Path.GetTempPath(), "tilecache"), "png");

            HttpTileSource tileSource = KartverketSources.Create(
                KartverketTileSource.SjøkartRaster,
                FetchFactory.CreateRetryFetcher(httpClient),
                fileCache);

            var southWest = SphericalMercator.FromLonLat(22.7962, 70.0910);
            var northEast = SphericalMercator.FromLonLat(22.8618, 70.1205);
            var extent = new Extent(southWest.X, southWest.Y, northEast.X, northEast.Y);

            Console.WriteLine($"Getting tiles for extent {extent}");

            var fetchStrategy = new DataFetchStrategy();
            var tileInfos = fetchStrategy.Get(tileSource.Schema, extent, 15);
            //var tileInfos = tileSource.Schema.GetTileInfos(extent, 7);

            Console.WriteLine($"Show tile info ({tileInfos.Count} tiles)");
            int tileCounter = 0;
            foreach (var tileInfo in tileInfos)
            {
                tileCounter++;
                double progress = Math.Round(tileCounter / (float)tileInfos.Count * 100.0f, 2);

                var tile = tileSource.GetTile(tileInfo);
                Console.WriteLine(
                    $"{progress}% ({tileCounter}/{tileInfos.Count}) - " +
                    $"tile col: {tileInfo.Index.Col}, " +
                    $"tile row: {tileInfo.Index.Row}, " +
                    $"tile level: {tileInfo.Index.Level}, " +
                    $"tile size {tile.Length}");
            }
        }
    }
}
