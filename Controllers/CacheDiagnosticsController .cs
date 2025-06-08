using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Transport.WebApi.Options;
using Transport.WebApi.Services.Caching;
using System.Reflection;
using System.Collections;

namespace Transport.WebApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CacheDiagnosticsController : ControllerBase
{
  private readonly ICacheService _cacheService;
  private readonly IMemoryCache _memoryCache;
  private readonly CacheOptions _cacheOptions;
  private readonly ILogger<CacheDiagnosticsController> _logger;

  public CacheDiagnosticsController(
    ICacheService cacheService,
    IMemoryCache memoryCache,
    IOptions<CacheOptions> cacheOptions,
    ILogger<CacheDiagnosticsController> logger)
  {
    _cacheService = cacheService;
    _memoryCache = memoryCache;
    _cacheOptions = cacheOptions.Value;
    _logger = logger;
  }

  /// <summary>
  /// Test cache expiration with a simple key
  /// </summary>
  [HttpPost("test-expiration/{seconds}")]
  public async Task<IActionResult> TestCacheExpiration(int seconds)
  {
    if (!HttpContext.RequestServices.GetService<IWebHostEnvironment>()?.IsDevelopment() ?? false)
    {
      return NotFound("Development only");
    }

    var testKey = $"test-expiration-{DateTime.UtcNow:HHmmss}";
    var testValue = new { Message = "Test cache value", CreatedAt = DateTime.UtcNow };
    var expiration = TimeSpan.FromSeconds(seconds);

    // Set cache with explicit expiration
    await _cacheService.SetAsync(testKey, testValue, expiration);

    _logger.LogInformation("Set cache key {Key} with {Seconds}s expiration", testKey, seconds);

    return Ok(new
    {
      TestKey = testKey,
      ExpirationSeconds = seconds,
      ExpirationTimeSpan = expiration.ToString(),
      SetAt = DateTime.UtcNow,
      Message = $"Cache set with {seconds}s expiration. Check /api/CacheDiagnostics/check-key/{testKey} to verify."
    });
  }

  /// <summary>
  /// Check if a specific cache key exists
  /// </summary>
  [HttpGet("check-key/{key}")]
  public IActionResult CheckCacheKey(string key)
  {
    if (!IsDevelopmentEnvironment())
    {
      return NotFound();
    }

    var exists = _cacheService.ContainsKey(key);

    return Ok(new
    {
      Key = key,
      Exists = exists,
      CheckedAt = DateTime.UtcNow,
      Message = exists ? "Key found in cache" : "Key not found or expired"
    });
  }

  /// <summary>
  /// Get current cache configuration values
  /// </summary>
  [HttpGet("config")]
  public IActionResult GetCacheConfig()
  {
    if (!HttpContext.RequestServices.GetService<IWebHostEnvironment>()?.IsDevelopment() ?? false)
    {
      return NotFound("Development only");
    }

    return Ok(new
    {
      Configuration = new
      {
        RealtimeCacheSeconds = _cacheOptions.RealtimeCacheSeconds,
        RealtimeCacheSecondsTotal = _cacheOptions.RealtimeCacheDuration,
        StaticCacheHours = _cacheOptions.StaticCacheHours,
        StaticCacheHoursTotal = _cacheOptions.StaticCacheDuration,
        CacheSizeLimit = _cacheOptions.CacheSizeLimit,
        CompactionPercentage = _cacheOptions.CompactionPercentage,
        EnableCacheHealthCheck = _cacheOptions.EnableCacheHealthCheck,
        LogCacheOperations = _cacheOptions.LogCacheOperations
      },
      RawConfigValues = new
      {
        // Show raw config values from IConfiguration
        RealtimeCacheSecondsRaw = HttpContext.RequestServices.GetService<IConfiguration>()?.GetValue<double>("Cache:RealtimeCacheSeconds"),
        StaticCacheHoursRaw = HttpContext.RequestServices.GetService<IConfiguration>()?.GetValue<double>("Cache:StaticCacheHours")
      }
    });
  }

  /// <summary>
  /// Get detailed memory cache statistics
  /// </summary>
  [HttpGet("memory-stats")]
  public IActionResult GetCacheStats()
  {
    if (!IsDevelopmentEnvironment())
    {
      return NotFound();
    }

    try
    {
      var diagnostics = _cacheService.GetDiagnostics();

      return Ok(new
      {
        Statistics = diagnostics,
        Configuration = new
        {
          RealtimeCacheSeconds = _cacheOptions.RealtimeCacheSeconds,
          StaticCacheHours = _cacheOptions.StaticCacheHours,
          CacheSizeLimit = _cacheOptions.CacheSizeLimit,
          CompactionPercentage = _cacheOptions.CompactionPercentage
        },
        CheckedAt = DateTime.UtcNow
      });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error retrieving cache diagnostics");
      return StatusCode(500, new { Error = "Failed to retrieve cache diagnostics" });
    }
  }

  /// <summary>
  /// Test the actual GTFS cache behavior
  /// </summary>
  [HttpPost("test-gtfs-cache")]
  public async Task<IActionResult> TestGtfsCacheBehavior()
  {
    if (!HttpContext.RequestServices.GetService<IWebHostEnvironment>()?.IsDevelopment() ?? false)
    {
      return NotFound("Development only");
    }

    var results = new List<object>();
    var testKey = CacheKeyGenerator.GetAllVehiclesCurrentPositionsKey();

    // First call - should be cache miss
    var start1 = DateTime.UtcNow;
    var exists1 = _memoryCache.TryGetValue(testKey, out var value1);
    results.Add(new
    {
      Call = 1,
      Time = start1,
      CacheHit = exists1,
      Key = testKey
    });

    // Set a test value with the actual configuration
    var testData = new { TestVehicles = "Sample data", SetAt = DateTime.UtcNow };
    await _cacheService.SetAsync(testKey, testData, _cacheOptions.RealtimeCacheDuration);

    // Second call - should be cache hit
    var start2 = DateTime.UtcNow;
    var exists2 = _memoryCache.TryGetValue(testKey, out var value2);
    results.Add(new
    {
      Call = 2,
      Time = start2,
      CacheHit = exists2,
      Key = testKey,
      Value = value2
    });

    return Ok(new
    {
      TestResults = results,
      CacheKeyUsed = testKey,
      ConfiguredExpiration = _cacheOptions.RealtimeCacheSeconds,
      ConfiguredExpirationSeconds = _cacheOptions.RealtimeCacheDuration,
      Message = $"Test completed. Cache should expire in {_cacheOptions.RealtimeCacheDuration} seconds. Check again with GET /api/CacheDiagnostics/check-key/{Uri.EscapeDataString(testKey)}"
    });
  }

  private bool IsDevelopmentEnvironment()
  {
    return HttpContext.RequestServices
        .GetService<IWebHostEnvironment>()?
        .IsDevelopment() ?? false;
  }
}
