using Microsoft.Extensions.Caching.Memory;
using Registry.Data;
using Registry.Models;
using Registry.Services;
using System.Collections.Concurrent;

namespace Registry.Services
{
    public class TokenService(ApplicationDbContext dbcontext, IMemoryCache cache) : ITokenService
    {
        public ApplicationDbContext Context = dbcontext;
        private readonly IMemoryCache _cache = cache;

        public void InsertInto(ApiKey key)
        {
            Context.Tokens.Add(key);
            Context.SaveChanges();
        }

        public void StorePending(ApiKey key)
        {
            var normalizedUrl = NormalizeUrl(key.ServerName);
            Console.WriteLine(normalizedUrl);
            _cache.Set(normalizedUrl, key, TimeSpan.FromMinutes(5));
        }

        public ApiKey? ConfirmPending(string url)
        {
            var normalizedUrl = NormalizeUrl(url);
            return _cache.TryGetValue(normalizedUrl, out ApiKey key) ? key : null;

        }

        private static string NormalizeUrl(string url)
        {
            return url.Trim().TrimEnd('/').ToLowerInvariant();
        }

    }
}
