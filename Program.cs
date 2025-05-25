using Serilog;
using System.Text.Json.Serialization;
using Transport.WebApi.Options;

internal class Program
{
  private static void Main(string[] args)
  {
    var builder = WebApplication.CreateBuilder(args);

    Log.Logger = new LoggerConfiguration()
        .ReadFrom.Configuration(builder.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
        .CreateLogger();
    builder.Host.UseSerilog();

    builder.Services.Configure<GtfsOptions>(builder.Configuration.GetSection("Gtfs"));

    builder.Services.AddHttpClient<Transport.WebApi.Services.GtfsDataService>();

    builder.Services.AddScoped<Transport.WebApi.Services.GtfsService>();

    builder.Services.AddControllers().AddJsonOptions(options =>
    {
      options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
      c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "Transport API", Version = "v1" });
      c.CustomSchemaIds(type => type.FullName);
      c.UseInlineDefinitionsForEnums();
    });

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
      app.UseSwagger();
      app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();

    app.UseAuthorization();

    app.MapControllers();

    app.Run();
  }
}