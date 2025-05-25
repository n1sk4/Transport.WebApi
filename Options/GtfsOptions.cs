namespace Transport.WebApi.Options;

public class GtfsOptions
{
  public string BaseUrl { get; set; } = string.Empty;
  public string RealtimeDataEndpoint { get; set; } = string.Empty;
  public string StaticDataEndpoint { get; set; } = string.Empty;
}
