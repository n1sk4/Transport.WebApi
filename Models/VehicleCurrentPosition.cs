namespace Transport.WebApi.Models;

public class VehicleCurrentPosition : Dictionary<string, object>
{
  public VehicleCurrentPosition() : base(StringComparer.OrdinalIgnoreCase){ }
  public VehicleCurrentPosition(IDictionary<string, object> dictionary) : base(dictionary, StringComparer.OrdinalIgnoreCase) { }
}
