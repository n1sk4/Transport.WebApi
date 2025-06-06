using System.ComponentModel.DataAnnotations;

namespace Transport.WebApi.Options;

public class CacheOptions
{
  [Range(1, 3600, ErrorMessage = "Realtime cache duration must be between 1 and 3600 seconds")]
  public int RealtimeCacheSeconds { get; set; } = 30;

  [Range(1, 168, ErrorMessage = "Static cache duration must be between 1 and 168 hours")]
  public int StaticCacheHours { get; set; } = 24;

  [Range(10, 1000, ErrorMessage = "Cache size limit must be between 10 and 1000")]
  public int CacheSizeLimit { get; set; } = 100;

  [Range(0.1, 0.5, ErrorMessage = "Compaction percentage must be between 0.1 and 0.5")]
  public double CompactionPercentage { get; set; } = 0.25;

  public bool EnableCacheHealthCheck { get; set; } = true;
  public bool LogCacheOperations { get; set; } = false;

  // Helper properties to convert to TimeSpan
  public TimeSpan RealtimeCacheDuration => TimeSpan.FromSeconds(RealtimeCacheSeconds);
  public TimeSpan StaticCacheDuration => TimeSpan.FromHours(StaticCacheHours);
}
