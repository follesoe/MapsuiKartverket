using BruTile;
using BruTile.Cache;

namespace MapsuiTest
{
    public class ReadOnlyFileCache : IPersistentCache<byte[]>, ITileCache<byte[]>
    {
        private readonly FileCache fileCache;
        private readonly MemoryCache<byte[]> memoryCache;

        public bool CacheToDisk { get; set; }

        public ReadOnlyFileCache(string directory, string format)
        {
            fileCache = new FileCache(directory, format);
            memoryCache = new MemoryCache<byte[]>();
        }

        public void Add(TileIndex index, byte[] image)
        {
            memoryCache.Add(index, image);
            if (CacheToDisk)
            {
                fileCache.Add(index, image);
            }
        }

        public byte[] Find(TileIndex index)
        {
            byte[] data = memoryCache.Find(index);
            return data ?? fileCache.Find(index);
        }

        public void Remove(TileIndex index) =>
            memoryCache.Remove(index);

        public bool Exists(TileIndex index) => fileCache.Exists(index);
    }
}
