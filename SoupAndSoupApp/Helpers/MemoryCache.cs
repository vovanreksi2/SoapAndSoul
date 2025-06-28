using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SoupAndSoupApp.Helpers;

public abstract class MemoryCache<T>
{
    private readonly Dictionary<string, T> _cache = new();
    private readonly TimeSpan _ttl = TimeSpan.FromMinutes(10);
    private readonly Dictionary<string, DateTime> _expiry = new();

    public async Task<T> GetOrAddAsync(string key, Func<Task<T>> fetchFromDbAsync)
    {
        if (_cache.TryGetValue(key, out var value) && _expiry[key] > DateTime.Now)
            return value;

        value = await  fetchFromDbAsync();
        _cache[key] = value;
        _expiry[key] = DateTime.Now + _ttl;
        return value;
    }
}