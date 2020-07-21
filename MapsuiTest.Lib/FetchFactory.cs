using System;
using System.Net.Http;
using Polly;
using Mapsui.Logging;

namespace MapsuiTest
{
    public class FetchFactory
    {
        public static Func<Uri, byte[]> CreateRetryFetcher(HttpClient httpClient)
            => (Uri uri) => FetchTile(uri, httpClient);

        private static byte[] FetchTile(Uri uri, HttpClient httpClient) =>
            Policy<byte[]>
                .Handle<HttpRequestException>()
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: attempt =>
                    {
                        Logger.Log(LogLevel.Warning, $"Retrying to fetch {uri} for {attempt} time");
                        return TimeSpan.FromMilliseconds(250);
                    }
                )
                .ExecuteAsync(async () =>
                {
                    Logger.Log(LogLevel.Trace, $"Fetching {uri}");
                    return await httpClient.GetByteArrayAsync(uri).ConfigureAwait(false);
                }).ConfigureAwait(false).GetAwaiter().GetResult();
    }
}