using Microsoft.Extensions.Caching.Memory;

namespace Transport.WebApi.Services.Caching;

public class CacheCleanupService : BackgroundService
{
  private readonly IServiceProvider _serviceProvider;
  private readonly ILogger<CacheCleanupService> _logger;

  public CacheCleanupService(IServiceProvider serviceProvider, ILogger<CacheCleanupService> logger)
  {
    _serviceProvider = serviceProvider;
    _logger = logger;
  }

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    _logger.LogInformation("Cache cleanup service started");

    while (!stoppingToken.IsCancellationRequested)
    {
      try
      {
        using var scope = _serviceProvider.CreateScope();
        var memoryCache = scope.ServiceProvider.GetRequiredService<IMemoryCache>();

        // Force garbage collection periodically to clean up expired cache entries
        if (memoryCache is MemoryCache mc)
        {
          var beforeCompaction = GC.GetTotalMemory(false);
          mc.Compact(0.25); // Remove 25% of items if needed
          var afterCompaction = GC.GetTotalMemory(true);

          _logger.LogDebug("Cache cleanup completed. Memory before: {Before}MB, after: {After}MB",
            beforeCompaction / 1024 / 1024,
            afterCompaction / 1024 / 1024);
        }
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error during cache cleanup");
      }

      // Run cleanup every hour
      await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
    }
  }

  public override async Task StopAsync(CancellationToken stoppingToken)
  {
    _logger.LogInformation("Cache cleanup service is stopping");
    await base.StopAsync(stoppingToken);
  }
}
