using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Transport.WebApi.Models;
using Transport.WebApi.Options;

namespace Transport.WebApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class StatisticsController : ControllerBase
{
  private readonly ILogger<StatisticsController> _logger;
  private readonly CacheOptions _cacheOptions;

  public StatisticsController(ILogger<StatisticsController> logger, IOptions<CacheOptions> cacheOptions)
  {
    _logger = logger;
    _cacheOptions = cacheOptions.Value;
  }

  #region Health Check
  /// <summary>
  /// Gets the health status of the API
  /// </summary>
  [HttpGet("health")]
  [ProducesResponseType(200)]
  [ProducesResponseType(503)]
  public IActionResult GetHealthStatus()
  {
    try
    {
      _logger.LogInformation("Health check requested");
      // TODO: Perform any necessary health checks here
      return Ok(new { Status = "OK", Timestamp = DateTime.UtcNow });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Health check failed");
      return StatusCode(503, new { Status = "Error", Error = ex.Message });
    }
  }
  #endregion

  #region Cache Management (monitoring)

  /// <summary>
  /// Gets cache statistics
  /// </summary>
  [HttpGet("cache/stats")]
  [ProducesResponseType(200, Type = typeof(CacheConfiguration))]
  public ActionResult<CacheConfiguration> GetCacheStats()
  {
    if (!HttpContext.RequestServices.GetService<IWebHostEnvironment>()?.IsDevelopment() ?? false)
    {
      return NotFound();
    }

    var stats = new
    {
      CacheConfiguration = new CacheConfiguration
      {
        RealtimeCacheSeconds = _cacheOptions.RealtimeCacheDuration,
        StaticCacheHours = _cacheOptions.StaticCacheDuration,
        CacheSizeLimit = _cacheOptions.CacheSizeLimit,
        CompactionPercentage = _cacheOptions.CompactionPercentage,
        EnableCacheHealthCheck = _cacheOptions.EnableCacheHealthCheck,
        LogCacheOperations = _cacheOptions.LogCacheOperations
      },
      Timestamp = DateTime.UtcNow
    };

    return Ok(stats);
  }

  #endregion
}
