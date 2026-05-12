using Microsoft.Extensions.Caching.Memory;

namespace FitTrack.Copilot.Service;

internal sealed class MemoryDistributedCacheAdapter : Microsoft.Extensions.Caching.Distributed.IDistributedCache
{
    private readonly IMemoryCache _memoryCache;

    public MemoryDistributedCacheAdapter(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
    }

    public byte[]? Get(string key) => _memoryCache.Get<byte[]>(key);

    public Task<byte[]?> GetAsync(string key, CancellationToken token = default)
        => Task.FromResult(_memoryCache.Get<byte[]>(key));

    public void Set(string key, byte[] value, Microsoft.Extensions.Caching.Distributed.DistributedCacheEntryOptions options)
    {
        var memoryCacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = options.AbsoluteExpirationRelativeToNow,
            SlidingExpiration = options.SlidingExpiration
        };

        _memoryCache.Set(key, value, memoryCacheOptions);
    }

    public Task SetAsync(string key, byte[] value, Microsoft.Extensions.Caching.Distributed.DistributedCacheEntryOptions options, CancellationToken token = default)
    {
        Set(key, value, options);
        return Task.CompletedTask;
    }

    public void Refresh(string key)
    {
    }

    public Task RefreshAsync(string key, CancellationToken token = default) => Task.CompletedTask;

    public void Remove(string key) => _memoryCache.Remove(key);

    public Task RemoveAsync(string key, CancellationToken token = default)
    {
        _memoryCache.Remove(key);
        return Task.CompletedTask;
    }
}
