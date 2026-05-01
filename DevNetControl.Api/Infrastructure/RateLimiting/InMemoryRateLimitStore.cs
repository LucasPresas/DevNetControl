namespace DevNetControl.Api.Infrastructure.RateLimiting;

/// <summary>
/// Implementación de IRateLimitStore usando almacenamiento en memoria.
/// Thread-safe y escalable para uso de desarrollo/testing.
/// Para producción con alto volumen, considerar Redis.
/// </summary>
public class InMemoryRateLimitStore : IRateLimitStore
{
    private readonly Dictionary<string, (int Count, DateTime ExpirationTime)> _store = new();
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly ILogger<InMemoryRateLimitStore> _logger;

    public InMemoryRateLimitStore(ILogger<InMemoryRateLimitStore> logger)
    {
        _logger = logger;
    }

    public Task<int> IncrementAsync(string key, int windowSeconds)
    {
        _lock.EnterWriteLock();
        try
        {
            var now = DateTime.UtcNow;

            // Limpiar entrada expirada
            if (_store.TryGetValue(key, out var existing) && existing.ExpirationTime < now)
            {
                _store.Remove(key);
            }

            if (_store.TryGetValue(key, out var entry))
            {
                entry.Count++;
                _store[key] = entry;
                return Task.FromResult(entry.Count);
            }
            else
            {
                var expirationTime = now.AddSeconds(windowSeconds);
                _store[key] = (1, expirationTime);
                return Task.FromResult(1);
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public Task<int> GetCountAsync(string key)
    {
        _lock.EnterReadLock();
        try
        {
            if (_store.TryGetValue(key, out var entry) && entry.ExpirationTime > DateTime.UtcNow)
            {
                return Task.FromResult(entry.Count);
            }
            return Task.FromResult(0);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public Task<int?> GetTtlAsync(string key)
    {
        _lock.EnterReadLock();
        try
        {
            if (_store.TryGetValue(key, out var entry))
            {
                var remainingSeconds = (int)(entry.ExpirationTime - DateTime.UtcNow).TotalSeconds;
                if (remainingSeconds > 0)
                    return Task.FromResult((int?)remainingSeconds);
            }
            return Task.FromResult<int?>(null);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public Task<int> CleanupAsync()
    {
        _lock.EnterWriteLock();
        try
        {
            var now = DateTime.UtcNow;
            var keysToRemove = _store
                .Where(kvp => kvp.Value.ExpirationTime < now)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in keysToRemove)
            {
                _store.Remove(key);
            }

            if (keysToRemove.Count > 0)
                _logger.LogDebug("Rate limiting: Cleaned up {Count} expired entries", keysToRemove.Count);

            return Task.FromResult(keysToRemove.Count);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Obtiene estadísticas del almacén (para monitoreo)
    /// </summary>
    public Dictionary<string, (int Count, int TtlSeconds)> GetStats()
    {
        _lock.EnterReadLock();
        try
        {
            var now = DateTime.UtcNow;
            return _store
                .Where(kvp => kvp.Value.ExpirationTime > now)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => (kvp.Value.Count, (int)(kvp.Value.ExpirationTime - now).TotalSeconds)
                );
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }
}
