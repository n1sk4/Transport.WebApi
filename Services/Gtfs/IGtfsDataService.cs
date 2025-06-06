using Transport.WebApi.Options;

namespace Transport.WebApi.Services.Gtfs;

public interface IGtfsDataService
{
  #region Realtime Data Retrieval
  Task<byte[]> GetRealtimeDataAsync();
  #endregion
  #region Static Data Retrieval
  Task<List<string>> GetStaticFileDataAsync(GtfsStaticDataFile fileName);
  #endregion
}
