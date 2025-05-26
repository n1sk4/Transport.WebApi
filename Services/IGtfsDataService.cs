using Transport.WebApi.Options;

namespace Transport.WebApi.Services;

public interface IGtfsDataService
{
  Task<List<string>> GetStaticFileDataAsync(GtfsStaticDataFile fileName);
  Task<byte[]> GetRealtimeDataAsync();
}
