using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ProductScrapperV2.Application.Interfaces;
using ProductScrapperV2.Domain.Entities;
using ProductScrapperV2.Infrastructure.Data;
using ProductScrapperV2.Infrastructure.Options;

namespace ProductScrapperV2.Infrastructure.Services;

public class ScrapeWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ScrapeWorker> _logger;
    private readonly ScrapingOptions _options;

    public ScrapeWorker(IServiceScopeFactory scopeFactory, ILogger<ScrapeWorker> logger, IOptions<ScrapingOptions> options)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await ProcessJobsAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromSeconds(_options.DelayBetweenJobsSeconds), stoppingToken);
        }
    }

    private async Task ProcessJobsAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var scrapingService = scope.ServiceProvider.GetRequiredService<IScrapingService>();
        var chatGpt = scope.ServiceProvider.GetRequiredService<IChatGptAnalysisService>();

        var job = await dbContext.ScrapeJobs
            .Include(j => j.Product)
            .OrderBy(j => j.CreatedAt)
            .FirstOrDefaultAsync(j => j.Status == JobStatus.Pending, cancellationToken);

        if (job is null || job.Product is null)
        {
            return;
        }

        job.Status = JobStatus.InProgress;
        await dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            var searchResults = await scrapingService.SearchGoogleAsync(job.Product.Name, cancellationToken);
            var candidateLinks = searchResults
                .Select(result => result.Split('|', StringSplitOptions.RemoveEmptyEntries).LastOrDefault()?.Trim())
                .Where(link => !string.IsNullOrWhiteSpace(link))
                .Distinct()
                .ToList();

            var targetLink = candidateLinks.FirstOrDefault() ?? job.Product.Name;
            var rawElements = await scrapingService.ExtractRawElementsAsync(targetLink, cancellationToken);
            var analysis = await chatGpt.AnalyzeProductAsync(job.Product.Name, targetLink, rawElements, cancellationToken);

            if (analysis is null)
            {
                throw new InvalidOperationException("AI analysis returned no result.");
            }

            var competitorCandidates = await chatGpt.AnalyzeCompetitorsAsync(job.Product.Name, searchResults, cancellationToken);
            var competitorInfo = competitorCandidates.OrderBy(c => c.SuggestedRank).FirstOrDefault();
            var competitor = competitorInfo is null
                ? await dbContext.Competitors.FirstOrDefaultAsync(cancellationToken: cancellationToken)
                : await dbContext.Competitors.FirstOrDefaultAsync(c => c.WebsiteUrl == competitorInfo.WebsiteUrl, cancellationToken);
            if (competitor is null)
            {
                competitor = new Competitor
                {
                    Id = Guid.NewGuid(),
                    Name = competitorInfo?.CompetitorName ?? "Unknown",
                    WebsiteUrl = competitorInfo?.WebsiteUrl ?? analysis.ProductUrl,
                    IsAutoDiscovered = true
                };
                dbContext.Competitors.Add(competitor);
            }

            dbContext.PriceRecords.Add(new PriceRecord
            {
                Id = Guid.NewGuid(),
                ProductId = job.ProductId,
                CompetitorId = competitor.Id,
                ProductTitle = analysis.ProductTitle,
                ProductUrl = analysis.ProductUrl,
                Price = analysis.Price,
                MatchPercentage = analysis.MatchPercentage,
                ConfidenceScore = analysis.ConfidenceScore,
                Currency = "IRR",
                CapturedAt = DateTimeOffset.UtcNow
            });

            job.Status = JobStatus.Completed;
            job.CompletedAt = DateTimeOffset.UtcNow;
        }
        catch (Exception ex)
        {
            job.Status = JobStatus.Failed;
            job.AttemptCount++;
            job.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Failed to process scrape job {JobId}", job.Id);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
