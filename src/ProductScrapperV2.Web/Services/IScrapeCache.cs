using ProductScrapperV2.Web.ViewModels;

namespace ProductScrapperV2.Web.Services;

public interface IScrapeCache
{
    bool TryGet(string url, out ScrapeResultDto result);
    void Set(string url, ScrapeResultDto result);
}

public sealed class InMemoryScrapeCache : IScrapeCache
{
    private readonly Dictionary<string, ScrapeResultDto> _cache = new();
    private readonly object _lock = new();

    public bool TryGet(string url, out ScrapeResultDto result)
    {
        lock (_lock)
        {
            return _cache.TryGetValue(Normalize(url), out result!);
        }
    }

    public void Set(string url, ScrapeResultDto result)
    {
        lock (_lock)
        {
            _cache[Normalize(url)] = result;
        }
    }

    private static string Normalize(string url)
    {
        return url.Trim().ToLowerInvariant();
    }
}
