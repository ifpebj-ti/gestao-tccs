namespace gestaotcc.Application.Gateways;

public interface IMemoryCacheGateway
{
    void Set<T>(string key, T value, TimeSpan expiration);
    bool TryGetValue<T>(string key, out T? value);
    void Remove(string key);
}