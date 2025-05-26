using System.ComponentModel.DataAnnotations;

namespace Transport.WebApi.Options;

public class CacheOptions
{
  [Range(1, 3600, ErrorMessage = "Realtime cache duration must be between 1 and 3600 seconds")]
  public TimeSpan RealtimeCacheSeconds { get; set; } = TimeSpan.FromSeconds(30);

  [Range(1, 168, ErrorMessage = "Static cache duration must be between 1 and 168 hours")]
  public TimeSpan StaticCacheHours { get; set; } = TimeSpan.FromHours(24);

  [Range(10, 1000, ErrorMessage = "Cache size limit must be between 10 and 1000")]
  public int CacheSizeLimit { get; set; } = 100;

  [Range(0.1, 0.5, ErrorMessage = "Compaction percentage must be between 0.1 and 0.5")]
  public double CompactionPercentage { get; set; } = 0.25;

  public bool EnableCacheHealthCheck { get; set; } = true;
  public bool LogCacheOperations { get; set; } = false;
}
