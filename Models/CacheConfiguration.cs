namespace Transport.WebApi.Models;

public class CacheConfiguration
{
  public TimeSpan RealtimeCacheSeconds { get; set; }
  public TimeSpan StaticCacheHours { get; set; }
  public int CacheSizeLimit { get; set; }
  public double CompactionPercentage { get; set; }
  public bool EnableCacheHealthCheck { get; set; }
  public bool LogCacheOperations { get; set; }
  public DateTime Timestamp { get; set; }

  public CacheConfiguration() { }

  public CacheConfiguration(
      TimeSpan realtimeCacheSeconds,
      TimeSpan staticCacheHours,
      int cacheSizeLimit,
      double compactionPercentage,
      bool enableCacheHealthCheck,
      bool logCacheOperations)
  {
    RealtimeCacheSeconds = realtimeCacheSeconds;
    StaticCacheHours = staticCacheHours;
    CacheSizeLimit = cacheSizeLimit;
    CompactionPercentage = compactionPercentage;
    EnableCacheHealthCheck = enableCacheHealthCheck;
    LogCacheOperations = logCacheOperations;
    Timestamp = DateTime.UtcNow;
  }
}
