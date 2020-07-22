using System;
using System.IO;
using System.Net.Http;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Collections.Generic;
using Mapsui;
using Mapsui.Fetcher;
using Mapsui.Layers;
using Mapsui.Projection;
using Mapsui.Widgets;
using Mapsui.Widgets.ScaleBar;
using Mapsui.Geometries;
using Mapsui.UI.Forms.Extensions;
using BruTile.Web;
using BruTile.Predefined;
using Xamarin.Forms;
using Acr.UserDialogs;
using Plugin.Segmented.Event;

namespace MapsuiTest
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class MainPage : ContentPage
    {
        private readonly HttpClient httpClient;
        private readonly List<HttpTileSource> sources;
        private readonly List<TileLayer> layers;
        private HttpTileSource selectedSource;

        public MainPage()
        {
            InitializeComponent();

            httpClient = new HttpClient();
            sources = new List<HttpTileSource>();
            layers = new List<TileLayer>();

            downloadMapButton.Clicked += OnDownloadMapButtonClicked;
            layerSegmentControl.OnSegmentSelected += OnLayerSegmentSelected;

            Mapsui.Logging.Logger.LogDelegate += (l, msg, ex) => Console.WriteLine($"[{l}]: {msg}");

            var map = new Map
            {
                CRS = "EPSG:3857",
                Transformation = new MinimalTransformation()
            };

            map.Widgets.Add(new ScaleBarWidget(map)
            {
                TextAlignment = Alignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Bottom
            });

            map.DataChanged += OnMapDataChanged;

            var latLon = SphericalMercator.FromLonLat(22.88967, 70.114923);
            mapView.Map = map;
            mapView.UseDoubleTap = true;
            mapView.IsNorthingButtonVisible = false;
            mapView.IsZoomButtonVisible = false;
            mapView.IsMyLocationButtonVisible = false;
            mapView.MyLocationEnabled = false;

            AddSources();

            mapView.Navigator.NavigateTo(latLon, ZoomLevelExtensions.ToMapsuiResolution(10));
            mapView.Refresh();
        }

        private void AddSources()
        {
            var norgeskart = KartverketSources.Create(
                KartverketTileSource.NorgeskartBakgrunn,
                FetchFactory.CreateRetryFetcher(httpClient));
            norgeskart.PersistentCache = new ReadOnlyFileCache(GetCacheFolder(norgeskart.Name), "png");
            var norgeskartLayer = new TileLayer(norgeskart) { Name = norgeskart.Name, Enabled = true };
            mapView.Map.Layers.Add(norgeskartLayer);

            var sjøkart = KartverketSources.Create(
                KartverketTileSource.SjøkartRaster,
                FetchFactory.CreateRetryFetcher(httpClient));
            sjøkart.PersistentCache = new ReadOnlyFileCache(GetCacheFolder(sjøkart.Name), "png");
            var sjøkartLayer = new TileLayer(sjøkart) { Name = sjøkart.Name, Enabled = false };
            mapView.Map.Layers.Add(sjøkartLayer);

            var osm = KnownTileSources.Create(
                KnownTileSource.OpenStreetMap,
                tileFetcher: FetchFactory.CreateRetryFetcher(httpClient));
            osm.PersistentCache = new ReadOnlyFileCache(GetCacheFolder(osm.Name), "png");
            var osmLayer = new TileLayer(osm) { Name = osm.Name, Enabled = false };
            mapView.Map.Layers.Add(osmLayer);

            selectedSource = norgeskart;
            sources.Add(norgeskart);
            sources.Add(sjøkart);
            sources.Add(osm);

            layers.Add(norgeskartLayer);
            layers.Add(sjøkartLayer);
            layers.Add(osmLayer);
        }

        private void OnLayerSegmentSelected(object sender, SegmentSelectEventArgs e)
        {
            foreach (var layer in layers)
                layer.Enabled = false;

            layers[e.NewValue].Enabled = true;
            selectedSource = sources[e.NewValue];

            mapView.Refresh();
        }

        private void OnDownloadMapButtonClicked(object sender, EventArgs e)
        {
            Task.Run(DownloadMap);
        }

        private void OnMapDataChanged(object sender, DataChangedEventArgs e)
        {
            if (e.Error != null)
            {
                Console.WriteLine($"Data fetch error: {e.Error.Message}");
            }
        }

        private string GetCacheFolder(string sourceName)
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "tilecache", sourceName);
            Console.WriteLine($"Path: {path}");
            return path;
        }

        private void DownloadMap()
        {
            if (selectedSource.PersistentCache == null || !(selectedSource.PersistentCache is ReadOnlyFileCache))
            {
                return;
            }

            var cache = (ReadOnlyFileCache)selectedSource.PersistentCache;
            var mapExtent = mapView.Viewport.Extent.ToExtent();
            var fetchStrategy = new CustomFetchStrategy();
            var tileInfos = fetchStrategy.Get(
                selectedSource.Schema,
                mapExtent,
                selectedSource.Schema.Resolutions.Count - 1);
            cache.CacheToDisk = true;

            using (var loading = UserDialogs.Instance.Loading())
            {
                int tileCounter = 0;
                foreach (var tileInfo in tileInfos)
                {
                    tileCounter++;
                    int progress = Convert.ToInt32(tileCounter / (float)tileInfos.Count * 100.0);

                    var tileSize = 0;
                    if (!cache.Exists(tileInfo.Index))
                    {
                        var tile = selectedSource.GetTile(tileInfo);
                        tileSize = tile.Length;
                    }

                    var progressString = $"{progress}% ({tileCounter}/{tileInfos.Count})";
                    var progressMessage = $"Downloading\n{progressString}";
                    Console.WriteLine(
                        $"{progressString} - " +
                        $"tile col: {tileInfo.Index.Col}, " +
                        $"tile row: {tileInfo.Index.Row}, " +
                        $"tile level: {tileInfo.Index.Level}, " +
                        $"tile size {tileSize}");

                    loading.Title = progressMessage;
                }
            }

            cache.CacheToDisk = false;
        }
    }
}
