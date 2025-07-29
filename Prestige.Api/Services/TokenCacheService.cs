using Microsoft.Extensions.Caching.Memory;

namespace Prestige.Api.Services
{
    public class TokenCacheService
    {
        private readonly IMemoryCache _cache;
        private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(55); // Just under 1 hour token expiry

        public TokenCacheService(IMemoryCache cache)
        {
            _cache = cache;
        }

        public bool IsTokenValid(string tokenHash)
        {
            return _cache.TryGetValue($"token_{tokenHash}", out _);
        }

        public void CacheValidToken(string tokenHash)
        {
            _cache.Set($"token_{tokenHash}", true, _cacheExpiry);
        }

        public void InvalidateToken(string tokenHash)
        {
            _cache.Remove($"token_{tokenHash}");
        }
    }
}