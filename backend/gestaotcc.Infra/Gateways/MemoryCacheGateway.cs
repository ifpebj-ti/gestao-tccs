using gestaotcc.Application.Gateways;
using Microsoft.Extensions.Caching.Memory;

namespace gestaotcc.Infra.Gateways;

public class MemoryCacheGateway(IMemoryCache cache) : IMemoryCacheGateway
{
    public void Set<T>(string key, T value, TimeSpan expiration)
    {
        var cacheEntryOptions = new MemoryCacheEntryOptions
        {
            // Define uma expiração absoluta a partir de agora
            AbsoluteExpirationRelativeToNow = expiration
        };

        cache.Set(key, value, cacheEntryOptions);
    }

    public bool TryGetValue<T>(string key, out T? value)
    {
        return cache.TryGetValue(key, out value);
    }

    public void Remove(string key)
    {
        cache.Remove(key);
    }
}