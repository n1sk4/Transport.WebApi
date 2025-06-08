using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using Transport.WebApi.Options;

namespace Transport.WebApi.Services.Caching;

public class MemoryCacheService : ICacheService
{
  private readonly IMemoryCache _memoryCache;
  private readonly ILogger<MemoryCacheService> _logger;
  private readonly CacheOptions _cacheOptions;

  private readonly ConcurrentDictionary<string, CacheEntryInfo> _entryMetadata = new();
  private int _hitCount = 0;
  private int _missCount = 0;
  private DateTime _lastCompaction = DateTime.UtcNow;

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
        Interlocked.Increment(ref _hitCount);
        if (_cacheOptions.LogCacheOperations)
        {
          _logger.LogDebug("Cache HIT for key: {CacheKey}, Type: {Type}", key, typeof(T).Name);
        }
        return (T?)cached;
      }

      Interlocked.Increment(ref _missCount);

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
      long estimatedSize = EstimateObjectSize(value);

      var cacheOptions = new MemoryCacheEntryOptions
      {
        AbsoluteExpirationRelativeToNow = expiration,
        SlidingExpiration = null, // No sliding expiration for GTFS data
        Priority = CacheItemPriority.Normal,
        Size = Math.Max(1, (int)(estimatedSize / 1000)) // Convert bytes to cache units
      };

      var entryInfo = new CacheEntryInfo
      {
        Key = key,
        SetAt = DateTime.UtcNow,
        ExpiresAt = DateTime.UtcNow.Add(expiration),
        EstimatedSize = estimatedSize,
        DataType = typeof(T).Name
      };

      cacheOptions.RegisterPostEvictionCallback((evictedKey, evictedValue, reason, state) =>
      {
        _entryMetadata.TryRemove(evictedKey.ToString() ?? string.Empty, out _);

        if (reason == EvictionReason.Capacity)
        {
          _lastCompaction = DateTime.UtcNow;
        }

        if (_cacheOptions.LogCacheOperations)
        {
          _logger.LogDebug("Cache item evicted - Key: {Key}, Reason: {Reason}, Type: {Type}",
            evictedKey, reason, typeof(T).Name);
        }
      });

      _memoryCache.Set(key, value, cacheOptions);

      _entryMetadata[key] = entryInfo;

      if (_cacheOptions.LogCacheOperations)
      {
        _logger.LogDebug("Cache SET - Key: {CacheKey}, Expires in: {Expiration}, Size: {Size}KB, Type: {Type}",
          key, expiration, estimatedSize / 1024, typeof(T).Name);
      }
      else
      {
        _logger.LogInformation("Cached item with key: {CacheKey}, expires in: {Expiration}", key, expiration);
      }
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error setting cache for key: {CacheKey}", key);
      throw;
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

  public async Task<T?> GetOrSetNullableAsync<T>(string key, Func<Task<T?>> factory, TimeSpan expiration) where T : class
  {
    var wrapper = await GetAsync<NullableWrapper<T>>(key);
    if (wrapper != null)
    {
      return wrapper.Value;
    }

    var value = await factory();
    var wrapperToCache = new NullableWrapper<T> { Value = value };
    await SetAsync(key, wrapperToCache, expiration);
    return value;
  }

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

  #region Helper Methods
  public CacheDiagnostics GetDiagnostics()
  {
    // Clean up expired entries
    var expiredKeys = _entryMetadata
        .Where(kvp => kvp.Value.IsExpired)
        .Select(kvp => kvp.Key)
        .ToList();

    foreach (var expiredKey in expiredKeys)
    {
      _entryMetadata.TryRemove(expiredKey, out _);
    }

    var activeEntries = _entryMetadata.Values.Where(e => !e.IsExpired).ToList();

    return new CacheDiagnostics
    {
      TotalEntries = activeEntries.Count,
      EstimatedMemoryUsage = activeEntries.Sum(e => e.EstimatedSize),
      HitCount = _hitCount,
      MissCount = _missCount,
      RecentEntries = activeEntries.OrderByDescending(e => e.SetAt).Take(20).ToList(),
      LastCompaction = _lastCompaction
    };
  }

  public bool ContainsKey(string key) => _memoryCache.TryGetValue(key, out _);

  public void ClearCache()
  {
    _entryMetadata.Clear();
    _logger.LogInformation("Cache metadata cleared");
  }

  private static long EstimateObjectSize<T>(T obj)
  {
    return obj switch
    {
      ICollection<object> collection => Math.Max(1000, collection.Count * 100),
      string str => Math.Max(100, str.Length * 2),
      byte[] bytes => bytes.Length,
      _ => 1000
    };
  }
  #endregion
}

internal class NullableWrapper<T> where T : class
{
  public T? Value { get; set; }
}
