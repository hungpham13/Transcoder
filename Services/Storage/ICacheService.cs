using ErrorOr;

namespace Transcoder.Services.Storage;

public interface ICacheService
{
    ErrorOr<T> GetData<T>(string key);
    bool SetData<T>(string key, T value, DateTimeOffset expirationTime);
    object RemoveData(string key);
}