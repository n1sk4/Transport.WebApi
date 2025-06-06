namespace Transport.WebApi.Models;

public class VehicleCurrentPosition : Dictionary<string, object>
{
  public VehicleCurrentPosition() : base(StringComparer.OrdinalIgnoreCase) { }
  public VehicleCurrentPosition(IDictionary<string, object> dictionary) : base(dictionary, StringComparer.OrdinalIgnoreCase) { }
}

public class EnhancedVehiclePosition
{
  public string RouteId { get; set; } = string.Empty;
  public string RouteShortName { get; set; } = string.Empty;
  public string RouteLongName { get; set; } = string.Empty;
  public int RouteType { get; set; } = 3; // Default to bus
  public List<VehiclePositionData> Vehicles { get; set; } = new();
}

public class VehiclePositionData
{
  public double Latitude { get; set; }
  public double Longitude { get; set; }
  public string VehicleId { get; set; } = string.Empty;
  public DateTime? LastUpdate { get; set; }
  public double? Speed { get; set; }
  public double? Bearing { get; set; }
}
