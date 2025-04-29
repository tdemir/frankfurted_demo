using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Frankfurt.Services.Interfaces;

public interface ICustomCacheService
{
    Task<T> Get<T>(string key);
    Task<T> GetOrCreate<T>(string key, TimeSpan expiration, Func<Task<T>> createItem);
    Task Set<T>(string key, T value, TimeSpan expiration);
    Task Remove(string key);
}