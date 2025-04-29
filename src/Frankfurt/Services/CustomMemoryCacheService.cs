using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Frankfurt.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace Frankfurt.Services;

public class CustomMemoryCacheService : ICustomCacheService
{
    private readonly IMemoryCache _cache;
    public CustomMemoryCacheService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public Task<T> Get<T>(string key)
    {
        if (_cache.TryGetValue(key, out T value))
        {
            return Task.FromResult(value);
        }

        return Task.FromResult(default(T));
    }

    public async Task<T> GetOrCreate<T>(string key, TimeSpan expiration, Func<Task<T>> createItem)
    {
        if (_cache.TryGetValue(key, out T value))
        {
            return value;
        }

        var cacheEntry = _cache.CreateEntry(key);
        //value = createItem(cacheEntry);
        value = await createItem();
        cacheEntry.SetAbsoluteExpiration(expiration);
        cacheEntry.SetValue(value);
        cacheEntry.Dispose();

        return value;
    }


    public Task Set<T>(string key, T value, TimeSpan expiration)
    {
        _cache.Set(key, value, expiration);
        return Task.CompletedTask;
    }
    public Task Remove(string key)
    {
        _cache.Remove(key);
        return Task.CompletedTask;
    }

    // public void Clear()
    // {
    //     // This method is not implemented in the original code.
    //     // You can implement it if needed, but it's not a standard feature of IMemoryCache.
    //     throw new NotImplementedException("Clearing the entire cache is not supported.");
    // }


}