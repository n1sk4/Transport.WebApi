using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Serilog;
using System.Text.Json.Serialization;
using Transport.WebApi.HealthChecks;
using Transport.WebApi.Options;
using Transport.WebApi.Services;
using Transport.WebApi.Services.Caching;
using Transport.WebApi.Services.Gtfs;

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
    builder.Services.AddResponseCompression(options =>
    {
      options.Providers.Add<BrotliCompressionProvider>();
      options.Providers.Add<GzipCompressionProvider>();
      options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(["application/json"]);
      options.EnableForHttps = true;
    });

    // Configuration with validation
    builder.Services.Configure<GtfsOptions>(builder.Configuration.GetSection("Gtfs"));
    builder.Services.Configure<CacheOptions>(builder.Configuration.GetSection("Cache"));

    // Add options validation
    builder.Services.AddOptions<GtfsOptions>()
        .Bind(builder.Configuration.GetSection("Gtfs"))
        .ValidateDataAnnotations()
        .ValidateOnStart();

    builder.Services.AddOptions<CacheOptions>()
        .Bind(builder.Configuration.GetSection("Cache"))
        .ValidateDataAnnotations()
        .ValidateOnStart();

    // Memory Cache configuration
    builder.Services.AddMemoryCache();
    builder.Services.AddSingleton<IConfigureOptions<MemoryCacheOptions>, ConfigureMemoryCacheOptions>();

    // Cache Services
    builder.Services.AddSingleton<ICacheService, MemoryCacheService>();

    // HTTP Client for GTFS data with better configuration
    builder.Services.AddHttpClient<GtfsDataService>(client =>
    {
      client.Timeout = TimeSpan.FromSeconds(30);
      client.DefaultRequestHeaders.Add("User-Agent", "Transport.WebApi/1.0");
    });

    // Register services with proper interface implementations
    builder.Services.AddScoped<GtfsDataService>();
    builder.Services.AddScoped<GtfsService>();

    // Register the global exception filter
    builder.Services.AddScoped<GlobalExceptionFilter>();

    // Register interfaces with decorator pattern
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

    builder.Services.AddCors(options =>
    {
      var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

      options.AddPolicy("ApiCorsPolicy", policy =>
      {
        if (allowedOrigins.Length > 0)
        {
          policy.WithOrigins(allowedOrigins)
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
        }
        else
        {
          // Fallback for development if not configured
          policy.AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
        }
      });

      // Add a development-specific policy
      options.AddPolicy("DevelopmentCorsPolicy", policy =>
      {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
      });
    });

    // Controllers and API configuration
    builder.Services.AddControllers(options =>
    {
      // Add global exception handling
      options.Filters.Add<GlobalExceptionFilter>();
    })
    .AddJsonOptions(options =>
    {
      options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
      options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

    // Add health checks
    builder.Services.AddHealthChecks()
        .AddCheck<GtfsHealthCheck>("gtfs-api")
        .AddCheck("memory-cache", () =>
        {
          // Simple memory check
          var memoryUsed = GC.GetTotalMemory(false);
          return memoryUsed < 500_000_000 ? // 500MB threshold
              Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy($"Memory usage: {memoryUsed / 1024 / 1024}MB") :
              Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Degraded($"High memory usage: {memoryUsed / 1024 / 1024}MB");
        });

    builder.Services.AddEndpointsApiExplorer();

    if (builder.Environment.IsProduction())
    {
      builder.Services.AddHostedService<CacheCleanupService>();
    }
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
        Description = "GTFS Realtime and Static Data API for Zagreb Public Transport",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
          Name = "Transport API Support",
          Url = new Uri("https://github.com/n1sk4/Transport.WebApi")
        }
      });

      c.UseInlineDefinitionsForEnums();

      // Add XML comments if available
      var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
      var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
      if (File.Exists(xmlPath))
      {
        c.IncludeXmlComments(xmlPath);
      }
    });
  }
  #endregion

  #region Middleware Configuration
  private static void ConfigureMiddleware(WebApplication app)
  {
    // Security headers
    app.Use(async (context, next) =>
    {
      context.Response.Headers["X-Content-Type-Options"] = "nosniff";
      context.Response.Headers["X-Frame-Options"] = "DENY";
      context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
      context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
      await next();
    });

    // Exception handling
    if (app.Environment.IsDevelopment())
    {
      app.UseDeveloperExceptionPage();
    }
    else
    {
      app.UseExceptionHandler("/error");
      app.UseHsts();
    }

    // Swagger configuration for both environments
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
      c.SwaggerEndpoint("/swagger/v1/swagger.json", "Transport API v1");
      c.RoutePrefix = "swagger";
      c.DisplayRequestDuration();
      c.EnableDeepLinking();
      c.ShowExtensions();
    });

    // HTTPS redirection
    app.UseHttpsRedirection();

    // Static files configuration
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
        RequestPath = "",
        OnPrepareResponse = ctx =>
        {
          // Add cache headers for static files
          if (ctx.File.Name.EndsWith(".js") || ctx.File.Name.EndsWith(".css"))
          {
            ctx.Context.Response.Headers["Cache-Control"] = "public,max-age=3600";
          }
        }
      });
    }

    if (app.Environment.IsDevelopment())
    {
      app.UseCors("DevelopmentCorsPolicy");
    }
    else
    {
      app.UseCors("ApiCorsPolicy");
    }

    // Health checks
    app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
      ResponseWriter = async (context, report) =>
      {
        context.Response.ContentType = "application/json";
        var response = new
        {
          status = report.Status.ToString(),
          checks = report.Entries.Select(x => new
          {
            name = x.Key,
            status = x.Value.Status.ToString(),
            exception = x.Value.Exception?.Message,
            duration = x.Value.Duration.ToString()
          }),
          duration = report.TotalDuration.ToString()
        };
        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
      }
    });

    // Request logging in development
    if (app.Environment.IsDevelopment())
    {
      app.UseSerilogRequestLogging(options =>
      {
        options.MessageTemplate = "Handled {RequestPath} ({RequestMethod}) in {Elapsed:0.0000} ms";
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
          diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
          diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
        };
      });
    }

    app.UseRouting();
    app.UseResponseCompression();
    app.UseAuthorization();
    app.MapControllers();

    // Fallback route for SPA
    app.MapFallbackToFile("index.html", new StaticFileOptions
    {
      FileProvider = new PhysicalFileProvider(clientPath)
    });
  }
  #endregion
}

// Global exception filter
public class GlobalExceptionFilter : Microsoft.AspNetCore.Mvc.Filters.IExceptionFilter
{
  private readonly ILogger<GlobalExceptionFilter> _logger;

  public GlobalExceptionFilter(ILogger<GlobalExceptionFilter> logger)
  {
    _logger = logger;
  }

  public void OnException(Microsoft.AspNetCore.Mvc.Filters.ExceptionContext context)
  {
    _logger.LogError(context.Exception, "Unhandled exception occurred");

    if (!context.HttpContext.RequestServices.GetService<IWebHostEnvironment>()?.IsDevelopment() ?? false)
    {
      context.Result = new Microsoft.AspNetCore.Mvc.ObjectResult(new { error = "An internal server error occurred" })
      {
        StatusCode = 500
      };
      context.ExceptionHandled = true;
    }
  }
}
