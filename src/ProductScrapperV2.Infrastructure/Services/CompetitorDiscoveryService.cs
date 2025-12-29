using Microsoft.EntityFrameworkCore;
using ProductScrapperV2.Application.DTOs;
using ProductScrapperV2.Application.Interfaces;
using ProductScrapperV2.Domain.Entities;
using ProductScrapperV2.Infrastructure.Data;

namespace ProductScrapperV2.Infrastructure.Services;

public class CompetitorDiscoveryService : ICompetitorDiscoveryService
{
    private readonly AppDbContext _dbContext;
    private readonly IScrapingService _scrapingService;
    private readonly IChatGptAnalysisService _chatGpt;

    public CompetitorDiscoveryService(
        AppDbContext dbContext,
        IScrapingService scrapingService,
        IChatGptAnalysisService chatGpt)
    {
        _dbContext = dbContext;
        _scrapingService = scrapingService;
        _chatGpt = chatGpt;
    }

    public async Task<IReadOnlyCollection<CompetitorDiscoveryResult>> DiscoverCompetitorsAsync(
        string productName,
        CancellationToken cancellationToken)
    {
        var searchResults = await _scrapingService.SearchGoogleAsync(productName, cancellationToken);
        var discovered = await _chatGpt.AnalyzeCompetitorsAsync(productName, searchResults, cancellationToken);

        foreach (var candidate in discovered)
        {
            var existing = await _dbContext.Competitors
                .FirstOrDefaultAsync(c => c.WebsiteUrl == candidate.WebsiteUrl, cancellationToken);

            if (existing is null)
            {
                _dbContext.Competitors.Add(new Competitor
                {
                    Id = Guid.NewGuid(),
                    Name = candidate.CompetitorName,
                    WebsiteUrl = candidate.WebsiteUrl,
                    IsAutoDiscovered = true
                });
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return discovered;
    }
}
