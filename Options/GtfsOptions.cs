using System.ComponentModel.DataAnnotations;

namespace Transport.WebApi.Options;

public class GtfsOptions
{
  [Required]
  [Url]
  public string BaseUrl { get; set; } = string.Empty;
  [Required]
  public string RealtimeDataEndpoint { get; set; } = string.Empty;
  [Required]
  public string StaticDataEndpoint { get; set; } = string.Empty;
}
