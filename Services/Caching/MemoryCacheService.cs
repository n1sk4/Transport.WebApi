using Microsoft.Extensions.Caching.Memory;
using Transport.WebApi.Services.Caching;

public class MemoryCacheService : ICacheService
{
  private readonly IMemoryCache _memoryCache;
  private readonly ILogger<MemoryCacheService> _logger;

  public MemoryCacheService(IMemoryCache memoryCache, ILogger<MemoryCacheService> logger)
  {
    _memoryCache = memoryCache;
    _logger = logger;
  }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
  public async Task<T?> GetAsync<T>(string key) where T : class
  {
    try
    {
      if (_memoryCache.TryGetValue(key, out var cached))
      {
        _logger.LogDebug("Cache hit for key: {CacheKey}", key);
        return (T?)cached;
      }

      _logger.LogDebug("Cache miss for key: {CacheKey}", key);
      return null;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error retrieving from cache for key: {CacheKey}", key);
      return null;
    }
  }

  public async Task SetAsync<T>(string key, T value, TimeSpan expiration) where T : class
  {
    try
    {
      var cacheOptions = new MemoryCacheEntryOptions
      {
        AbsoluteExpirationRelativeToNow = expiration,
        SlidingExpiration = null, // No sliding expiration for GTFS data
        Priority = CacheItemPriority.Normal
      };

      _memoryCache.Set(key, value, cacheOptions);
      _logger.LogDebug("Cached item with key: {CacheKey}, expires in: {Expiration}", key, expiration);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error setting cache for key: {CacheKey}", key);
    }
  }

  public async Task RemoveAsync(string key)
  {
    try
    {
      _memoryCache.Remove(key);
      _logger.LogDebug("Removed cache entry for key: {CacheKey}", key);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error removing cache for key: {CacheKey}", key);
    }
  }

  public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan expiration) where T : class
  {
    var cached = await GetAsync<T>(key);
    if (cached != null)
    {
      return cached;
    }

    var value = await factory();
    await SetAsync(key, value, expiration);
    return value;
  }
}

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
