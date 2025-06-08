namespace Transport.WebApi.Services.Caching;

public class CacheDiagnostics
{
  public int TotalEntries { get; set; }
  public long EstimatedMemoryUsage { get; set; }
  public int HitCount { get; set; }
  public int MissCount { get; set; }
  public double HitRatio => TotalRequests > 0 ? (double)HitCount / TotalRequests : 0;
  public int TotalRequests => HitCount + MissCount;
  public List<CacheEntryInfo> RecentEntries { get; set; } = new();
  public DateTime LastCompaction { get; set; }
}

public class CacheEntryInfo
{
  public string Key { get; set; } = string.Empty;
  public DateTime SetAt { get; set; }
  public DateTime? ExpiresAt { get; set; }
  public long EstimatedSize { get; set; }
  public string DataType { get; set; } = string.Empty;
  public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value < DateTime.UtcNow;
}
