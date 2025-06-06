using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Transport.WebApi.Services.Caching;
using Transport.WebApi.Options;
using Microsoft.Extensions.Logging;
using System.Collections;

public class MemoryCacheService : ICacheService
{
  private readonly IMemoryCache _memoryCache;
  private readonly ILogger<MemoryCacheService> _logger;
  private readonly CacheOptions _cacheOptions;

  public MemoryCacheService(
    IMemoryCache memoryCache,
    ILogger<MemoryCacheService> logger,
    IOptions<CacheOptions> cacheOptions)
  {
    _memoryCache = memoryCache;
    _logger = logger;
    _cacheOptions = cacheOptions.Value;
  }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
  public async Task<T?> GetAsync<T>(string key) where T : class
  {
    try
    {
      if (_memoryCache.TryGetValue(key, out var cached))
      {
        if (_cacheOptions.LogCacheOperations)
        {
          _logger.LogDebug("Cache HIT for key: {CacheKey}, Type: {Type}", key, typeof(T).Name);
        }
        return (T?)cached;
      }

      if (_cacheOptions.LogCacheOperations)
      {
        _logger.LogDebug("Cache MISS for key: {CacheKey}, Type: {Type}", key, typeof(T).Name);
      }
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
      int size = value switch
      {
        ICollection<object> collection => Math.Max(1, collection.Count / 10),
        string str => Math.Max(1, str.Length / 1000),
        byte[] bytes => Math.Max(1, bytes.Length / 10000),
        _ => 1
      };

      var cacheOptions = new MemoryCacheEntryOptions
      {
        AbsoluteExpirationRelativeToNow = expiration,
        SlidingExpiration = null, // No sliding expiration for GTFS data
        Priority = CacheItemPriority.Normal,
        Size = size
      };

      if (_cacheOptions.LogCacheOperations)
      {
        cacheOptions.RegisterPostEvictionCallback((evictedKey, evictedValue, reason, state) =>
        {
          _logger.LogDebug("Cache item evicted - Key: {Key}, Reason: {Reason}, Type: {Type}",
            evictedKey, reason, typeof(T).Name);
        });
      }

      _memoryCache.Set(key, value, cacheOptions);

      if (_cacheOptions.LogCacheOperations)
      {
        _logger.LogDebug("Cache SET - Key: {CacheKey}, Expires in: {Expiration}, Size: {Size}, Type: {Type}",
          key, expiration, size, typeof(T).Name);
      }
      else
      {
        _logger.LogInformation("Cached item with key: {CacheKey}, expires in: {Expiration}", key, expiration);
      }
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
      if (_cacheOptions.LogCacheOperations)
      {
        _logger.LogDebug("Cache REMOVE - Key: {CacheKey}", key);
      }
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
