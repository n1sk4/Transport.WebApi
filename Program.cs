using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Serilog;
using System.Text.Json.Serialization;
using Transport.WebApi.Options;
using Transport.WebApi.Services;
using Transport.WebApi.Services.Caching;

internal class Program
{
  private static void Main(string[] args)
  {
    var builder = WebApplication.CreateBuilder(args);

    ConfigureLogging(builder);
    ConfigureServices(builder);
    ConfigureSwagger(builder);

    var app = builder.Build();

    ConfigureMiddleware(app);

    app.Run();
  }

  #region Logging Configuration
  private static void ConfigureLogging(WebApplicationBuilder builder)
  {
    Log.Logger = new LoggerConfiguration()
        .ReadFrom.Configuration(builder.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
        .CreateLogger();
    builder.Host.UseSerilog();
  }
  #endregion

  #region Service Configuration
  private static void ConfigureServices(WebApplicationBuilder builder)
  {
    // Configuration
    builder.Services.Configure<GtfsOptions>(builder.Configuration.GetSection("Gtfs"));

    // Memory Cache
    builder.Services.Configure<CacheOptions>(builder.Configuration.GetSection("Cache"));
    builder.Services.AddMemoryCache();
    builder.Services.AddSingleton<IConfigureOptions<MemoryCacheOptions>, ConfigureMemoryCacheOptions>();

    // Cache Services
    builder.Services.AddSingleton<ICacheService, MemoryCacheService>();

    // HTTP Client for GTFS data
    builder.Services.AddHttpClient<GtfsDataService>(client =>
    {
      client.Timeout = TimeSpan.FromSeconds(30);
      client.DefaultRequestHeaders.Add("User-Agent", "Transport.WebApi/1.0");
    });

    // Core Services (base implementations)
    builder.Services.AddScoped<GtfsDataService>();
    builder.Services.AddScoped<GtfsService>();

    // Cached Service Decorators
    builder.Services.AddScoped<IGtfsDataService>(provider =>
    {
      var baseService = provider.GetRequiredService<GtfsDataService>();
      var cacheService = provider.GetRequiredService<ICacheService>();
      var cacheOptions = provider.GetRequiredService<IOptions<CacheOptions>>().Value;
      var logger = provider.GetRequiredService<ILogger<CachedGtfsDataService>>();
      return new CachedGtfsDataService(baseService, cacheService, cacheOptions, logger);
    });

    builder.Services.AddScoped<IGtfsService>(provider =>
    {
      var baseService = provider.GetRequiredService<GtfsService>();
      var cacheService = provider.GetRequiredService<ICacheService>();
      var cacheOptions = provider.GetRequiredService<IOptions<CacheOptions>>().Value;
      var logger = provider.GetRequiredService<ILogger<CachedGtfsService>>();
      return new CachedGtfsService(baseService, cacheService, cacheOptions, logger);
    });

    // CORS Configuration - ADD THIS HERE (before builder.Build())
    builder.Services.AddCors(options =>
    {
      options.AddPolicy("AllowLocalDevelopment", policy =>
      {
        policy.WithOrigins(
            "http://localhost:8000",
            "http://localhost:3000",
            "http://127.0.0.1:8000"
        )
        .AllowAnyMethod()
        .AllowAnyHeader();
      });
    });

    // Controllers and API configuration
    builder.Services.AddControllers().AddJsonOptions(options =>
    {
      options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

    builder.Services.AddEndpointsApiExplorer();
  }
  #endregion

  #region Swagger Configuration
  private static void ConfigureSwagger(WebApplicationBuilder builder)
  {
    builder.Services.AddSwaggerGen(c =>
    {
      c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
      {
        Title = "Transport API",
        Version = "v1",
        Description = "GTFS Realtime and Static Data API"
      });
      c.UseInlineDefinitionsForEnums();
    });
  }
  #endregion

  #region Middleware Configuration
  private static void ConfigureMiddleware(WebApplication app)
  {
    if (app.Environment.IsDevelopment())
    {
      app.UseSwagger();
      app.UseSwaggerUI(c =>
      {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Transport API v1");
        c.RoutePrefix = "swagger";
      });
      app.UseCors("AllowLocalDevelopment");
    }

    app.UseHttpsRedirection();

    var clientPath = Path.Combine(Directory.GetCurrentDirectory(), "WebClients", "simple-client");
    if (Directory.Exists(clientPath))
    {
      app.UseDefaultFiles(new DefaultFilesOptions
      {
        FileProvider = new PhysicalFileProvider(clientPath),
        RequestPath = ""
      });
      app.UseStaticFiles(new StaticFileOptions
      {
        FileProvider = new PhysicalFileProvider(clientPath),
        RequestPath = ""
      });
    }

    app.UseAuthorization();
    app.MapControllers();
  }
  #endregion
}
