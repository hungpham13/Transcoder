using StackExchange.Redis;
using ErrorOr;
using Newtonsoft.Json;

namespace Transcoder.Services.Storage;

public class CacheService : ICacheService
{
    private IDatabase _cacheDb;
    public CacheService()
    {
        var redis = ConnectionMultiplexer.Connect("172.16.1.117:6379,defaultDatabase=1");
        _cacheDb = redis.GetDatabase();
    } 
    
    public ErrorOr<T> GetData<T>(string key)
    {
        var value = _cacheDb.StringGet(key);
        if (!string.IsNullOrEmpty(value))
            return JsonConvert.DeserializeObject<T>(value);
        return default;
    }

    public bool SetData<T>(string key, T value, DateTimeOffset expirationTime)
    {
        var expiryTime = expirationTime.DateTime.Subtract(DateTime.Now);
        var isSet = _cacheDb.StringSet(key, JsonConvert.SerializeObject(value), expiryTime);
        return isSet;
    }

    public object RemoveData(string key)
    {
        var _exist = _cacheDb.KeyExists(key);
        if (_exist)
            return _cacheDb.KeyDelete(key);
        return false;
    }
}