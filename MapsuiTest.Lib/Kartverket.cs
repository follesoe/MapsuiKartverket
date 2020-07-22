using System;
using System.Collections.Generic;
using BruTile;
using BruTile.Web;
using BruTile.Predefined;
using BruTile.Cache;
using System.Linq;

namespace MapsuiTest
{
    public enum KartverketTileSource
    {
        Grunnkart,
        GrunnkartGråtone,
        Topografisk,
        Terrengmodell,
        EnkeltKart,
        SjøkartRaster,
        NorgeskartBakgrunn
    }

    public static class KartverketSources
    {
        private static readonly Attribution attribution = new Attribution("© Kartverket", "https://www.kartverket.no");

        public static HttpTileSource Create(
            KartverketTileSource source = KartverketTileSource.Topografisk,
            Func<Uri, byte[]> tileFetcher = null,
            IPersistentCache<byte[]> persistentCache = null) =>
            new HttpTileSource(
                new GlobalSphericalMercator(zoomLevels: TileSourceToZoomLevels(source)),
                "https://{s}.statkart.no/gatekeeper/gk/gk.open_gmaps?layers=" + TileSourceToLayer(source) + "&zoom={z}&x={x}&y={y}",
                serverNodes: new[] { "opencache", "opencache2", "opencache3" },
                persistentCache: persistentCache,
                name: source.ToString(),
                attribution: attribution,
                tileFetcher: tileFetcher);

        private static string TileSourceToLayer(KartverketTileSource source) =>
            source switch
            {
                KartverketTileSource.Grunnkart => "norges_grunnkart",
                KartverketTileSource.GrunnkartGråtone => "norges_grunnkart_graatone",
                KartverketTileSource.Topografisk => "topo4",
                KartverketTileSource.Terrengmodell => "terreng_norgeskart",
                KartverketTileSource.EnkeltKart => "egk",
                KartverketTileSource.SjøkartRaster => "sjokartraster",
                KartverketTileSource.NorgeskartBakgrunn => "norgeskart_bakgrunn",
                _ => throw new ArgumentException($"Unknown source: {source}", nameof(source)),
            };

        private static IEnumerable<int> TileSourceToZoomLevels(KartverketTileSource source) =>
            source switch
            {
                KartverketTileSource.NorgeskartBakgrunn => Enumerable.Range(1, 18),
                _ => Enumerable.Range(1, 15)
            };
    }
}
