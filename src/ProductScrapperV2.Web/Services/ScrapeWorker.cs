using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ProductScrapperV2.Web.Domains;
using ProductScrapperV2.Web.ViewModels;
using System.Threading;
using ZennerDownlink.Data;
using System.Net;

namespace ProductScrapperV2.Web.Services;

public sealed class ScrapeWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ScrapeWorker> _logger;
    private readonly ScrapingOptions _options;
    private readonly IScrapeCache cache;

    public ScrapeWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<ScrapeWorker> logger,
        IOptions<ScrapingOptions> options,
        IScrapeCache cache)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options.Value;
        this.cache = cache;
    }


    private static string HashSnapshot(PageSnapshotDto snapshot)
    {
        var raw = snapshot.Url + string.Join("|", snapshot.VisibleTexts);
        return Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(raw)));
    }


    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ScrapeWorker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
               await ProcessNextJobAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in ScrapeWorker loop");
            }

            await Task.Delay(
                TimeSpan.FromSeconds(_options.DelayBetweenJobsSeconds),
                stoppingToken);
        }
    }

    // ------------------------------------------------------------

    private async Task ProcessNextJobAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var scrapingService = scope.ServiceProvider.GetRequiredService<IScrapingService>();
        var chatGpt = scope.ServiceProvider.GetRequiredService<IChatGptAnalysisService>();

        var job = await dbContext.ScrapeJobs
            .Include(j => j.Product)
            .OrderBy(j => j.CreatedAt)
            .FirstOrDefaultAsync(j => j.Status == JobStatus.Pending, ct);

        if (job is null || job.Product is null)
            return;

        job.Status = JobStatus.InProgress;
        await dbContext.SaveChangesAsync(ct);

        try
        {
            _logger.LogInformation("Processing scrape job {JobId} for product {Product}",
                job.Id, job.Product.Name);

            var searchResults = await scrapingService.SearchGoogleAsync(job.Product.Name, ct);

            var candidateLinks = searchResults
                .Select(r => r.Split('|', StringSplitOptions.RemoveEmptyEntries).LastOrDefault()?.Trim())
                .Where(u => !string.IsNullOrWhiteSpace(u))
                .Distinct()
                .Take(10)
                .ToList();

            var extractedList = await FindAllMatchesAgentDrivenAsync(
    job.Product.Name,
    candidateLinks,
    scrapingService,
    chatGpt,
    ct);

            if (extractedList.Count == 0)
                throw new InvalidOperationException("Agent could not extract any valid product from candidates.");


            foreach (var extracted in extractedList)
            {
                var competitor = await ResolveCompetitorAsync(
                    dbContext,
                    chatGpt,
                    job.Product.Name,
                    searchResults,
                    extracted.ProductUrl,
                    ct);

                dbContext.PriceRecords.Add(new PriceRecord
                {
                    Id = Guid.NewGuid(),
                    ProductId = job.ProductId,
                    CompetitorId = competitor.Id,
                    ProductTitle = extracted.ProductTitle,
                    ProductUrl = extracted.ProductUrl,
                    Price = extracted.Price,
                    MatchPercentage = extracted.MatchPercentage,
                    ConfidenceScore = extracted.ConfidenceScore,
                    Currency = "IRR",
                    CapturedAt = DateTimeOffset.UtcNow
                });
            }


            job.Status = JobStatus.Completed;
            job.CompletedAt = DateTimeOffset.UtcNow;

            var prices = await dbContext.PriceRecords
                .Where(p =>
                    p.ProductId == job.ProductId &&
                    p.Price > 0) // فقط قیمت‌های معتبر
                .Select(p => p.Price)
                .ToListAsync(ct);

            if (prices.Any())
            {
                job.Product.AverageMarketPrice = prices.Average();
            }

            _logger.LogInformation("Scrape job {JobId} completed successfully", job.Id);
        }
        catch (Exception ex)
        { 
            job.Status = JobStatus.Failed;
            job.AttemptCount++;
            job.ErrorMessage = ex.Message;

            _logger.LogError(ex, "Scrape job {JobId} failed", job.Id);
        }

        await dbContext.SaveChangesAsync(ct);
    }

    private static string GetHostFromUrl(string url)
    {
        var uri = new Uri(url);
        var host = uri.Host.ToLowerInvariant();
        if (host.StartsWith("www.")) host = host[4..];
        return host;
    }


    // ------------------------------------------------------------
    // AGENT-BASED MATCHING
    // ------------------------------------------------------------


    private async Task<IReadOnlyList<ScrapeResultDto>> FindAllMatchesAgentDrivenAsync(
        string productName,
        IReadOnlyCollection<string> candidateLinks,
        IScrapingService scrapingService,
        IChatGptAnalysisService chatGpt,
        CancellationToken ct)
    {
        const int maxAttempts = 4;
        const int baseDelayMs = 1000;

        var results = new List<ScrapeResultDto>();

        if (candidateLinks is null || candidateLinks.Count == 0)
            return results;

        foreach (var candidate in candidateLinks)
        {
            ct.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(candidate))
                continue;

            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    _logger.LogDebug("Agent attempt {Attempt}/{MaxAttempts} for {Product} on {Url}",
                        attempt, maxAttempts, productName, candidate);

                    var extracted = await RunAgentAsync(
                        productName,
                        candidate,
                        scrapingService,
                        chatGpt,
                        cache,
                        ct);

                    // اگر چیزی استخراج شد:
                    if (extracted is not null)
                    {
                        // اگر قیمت ناموجود(-1) یا معتبر(>0) بود، نگه دار.
                        // قیمت 0 یعنی "نتونست تشخیص بده" => نریز تو خروجی
                        if (extracted.Price == -1 || extracted.Price > 0)
                            results.Add(extracted);

                        break; // برو لینک بعدی
                    }

                    // چیزی درنیومد => لینک بعدی
                    break;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex) when (IsTooManyRequests(ex))
                {
                    if (attempt >= maxAttempts)
                    {
                        _logger.LogWarning("429 retry exceeded for {Product} on {Url}", productName, candidate);
                        break; // برو لینک بعدی
                    }

                    var delayMs = baseDelayMs * (int)Math.Pow(2, attempt - 1);
                    _logger.LogWarning(ex, "429 on attempt {Attempt}/{MaxAttempts} for {Product} on {Url}. Delay={Delay}ms",
                        attempt, maxAttempts, productName, candidate, delayMs);

                    await Task.Delay(delayMs, ct);
                    continue;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Agent error for {Product} on {Url}. Skipping this candidate.", productName, candidate);
                    break;
                }
            }
        }

        return results;

        static bool IsTooManyRequests(Exception ex)
        {
            if (ex is HttpRequestException hre && hre.StatusCode == HttpStatusCode.TooManyRequests)
                return true;

            var msg = ex.Message ?? string.Empty;
            if (msg.Contains("429", StringComparison.OrdinalIgnoreCase) ||
                msg.Contains("too many requests", StringComparison.OrdinalIgnoreCase))
                return true;

            Thread.Sleep(3000);

            return ex.InnerException is not null && IsTooManyRequests(ex.InnerException);
        }
    }
    // ------------------------------------------------------------
    // AGENT LOOP
    // ------------------------------------------------------------

    private async Task<ScrapeResultDto?> RunAgentAsync(
        string productName,
        string startUrl,
        IScrapingService scraping,
        IChatGptAnalysisService ai,
        IScrapeCache cache,
        CancellationToken ct)
    {


        if (cache.TryGet(startUrl, out var cached))
            return cached;


        await using var session = await scraping.CreateSessionAsync(ct);

        await session.NavigateAsync(startUrl, ct);
        var visitedSnapshots = new HashSet<string>();

        for (var step = 1; step <= _options.MaxAgentStepsPerSite; step++)
        {
            var snapshot = await session.CaptureSnapshotAsync(ct);
            var hash = HashSnapshot(snapshot);

            if (!visitedSnapshots.Add(hash))
            {
                _logger.LogWarning("Agent loop detected, stopping");
                return null;
            }

            var action = await ai.DecideNextActionAsync(productName, snapshot, step, ct);

            switch (action.Type)
            {
                case AgentActionType.Navigate:
                    if (!string.IsNullOrWhiteSpace(action.Url))
                        await session.NavigateAsync(action.Url, ct);
                    break;

                case AgentActionType.Click:
                    if (!string.IsNullOrWhiteSpace(action.CssSelector))
                        await session.ClickAsync(action.CssSelector, ct);
                    break;

                case AgentActionType.ExtractProduct:
                    {
                        var result = await ai.ExtractProductAsync(productName, snapshot, ct);

                        if (result != null)
                            cache.Set(startUrl, result);

                        return result;
                    }


                case AgentActionType.Stop:
                    return null;
            }
        }

        return null;
    }

    // ------------------------------------------------------------
    // COMPETITOR RESOLUTION
    // ------------------------------------------------------------
    private static async Task<Competitor> ResolveCompetitorAsync(
    AppDbContext dbContext,
    IChatGptAnalysisService chatGpt,
    string productName,
    IReadOnlyCollection<string> searchResults,
    string productUrl,
    CancellationToken ct)
    {
        var host = GetHostFromUrl(productUrl);

        var existing = dbContext.Competitors
            .Where(c => c.WebsiteUrl != null)
            .AsEnumerable() // ✅ از اینجا به بعد روی RAM
            .FirstOrDefault(c => GetHostFromUrl(c.WebsiteUrl!) == host);

        if (existing is not null)
            return existing;

        // 2) از AI کمک بگیر ولی باز match رو با host انجام بده
        var discovered = await chatGpt.AnalyzeCompetitorsAsync(productName, searchResults, ct);

        var match = discovered.FirstOrDefault(d =>
        {
            try { return GetHostFromUrl(d.WebsiteUrl) == host; }
            catch { return false; }
        });

        var competitor = new Competitor
        {
            Id = Guid.NewGuid(),
            Name = match?.CompetitorName ?? GuessCompetitorNameFromHost(host),
            WebsiteUrl = match?.WebsiteUrl ?? $"https://{host}",
            WebsiteHost = host, // ✅ مهم
            IsAutoDiscovered = true
        };

        dbContext.Competitors.Add(competitor);
        return competitor;
    }



    private static string GetHost(string url)
    {
        var uri = new Uri(url);
        var host = uri.Host.ToLowerInvariant();
        if (host.StartsWith("www.")) host = host[4..];
        return host;
    }

private static string GuessCompetitorNameFromHost(string host)
{
    return host switch
    {
        "digikala.com" => "دیجی کالا",
        "torob.com" => "توروب",
        "technolife.com" => "تکنولایف",
        "snappshop.ir" => "اسنپ شاپ",
        "divar.ir" => "دیوار",
        _ => host
    };
}




}
