using Microsoft.Extensions.Options;
using Microsoft.Extensions.Caching.Memory;
using Transport.WebApi.Options;

public class ConfigureMemoryCacheOptions : IConfigureOptions<MemoryCacheOptions>
{
  private readonly CacheOptions _cacheOptions;

  public ConfigureMemoryCacheOptions(IOptions<CacheOptions> cacheOptions)
  {
    _cacheOptions = cacheOptions.Value;
  }

  public void Configure(MemoryCacheOptions options)
  {
    options.SizeLimit = _cacheOptions.CacheSizeLimit;
    options.CompactionPercentage = _cacheOptions.CompactionPercentage;
  }
}
