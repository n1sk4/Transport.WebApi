namespace Transport.WebApi.Services.Caching;

public interface ICacheService
{
  Task<T?> GetAsync<T>(string key) where T : class;
  Task SetAsync<T>(string key, T value, TimeSpan expiration) where T : class;
  Task RemoveAsync(string key);
  Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan expiration) where T : class;
  Task<T?> GetOrSetNullableAsync<T>(string key, Func<Task<T?>> factory, TimeSpan expiration) where T : class;

  CacheDiagnostics GetDiagnostics();
  bool ContainsKey(string key);
  void ClearCache();
}
